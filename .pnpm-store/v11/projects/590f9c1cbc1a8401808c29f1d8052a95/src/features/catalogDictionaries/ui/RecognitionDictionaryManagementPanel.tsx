"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import {
  catalogDictionaryTermsQueryKey,
  getCatalogDictionaryTerms,
} from "@/features/catalogDictionaries/api/getCatalogDictionaryTerms";
import { setCatalogDictionaryTermActive } from "@/features/catalogDictionaries/api/setCatalogDictionaryTermActive";
import type {
  CatalogDictionaryTerm,
  CatalogDictionaryTermStatus,
} from "@/features/catalogDictionaries/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";

type DictionaryTermFilter = "Active" | "Disabled" | "All";

interface DictionaryTermAction {
  termId: string;
  isActive: boolean;
}

interface RecognitionDictionaryManagementPanelProps {
  currentProductTypeId: string | null;
  isRecognitionRefreshing: boolean;
  onRecognitionRefresh: () => Promise<void>;
}

export function RecognitionDictionaryManagementPanel({
  currentProductTypeId,
  isRecognitionRefreshing,
  onRecognitionRefresh,
}: RecognitionDictionaryManagementPanelProps) {
  const queryClient = useQueryClient();

  const [filter, setFilter] = useState<DictionaryTermFilter>("Active");
  const [searchText, setSearchText] = useState("");
  const [action, setAction] = useState<DictionaryTermAction | null>(null);
  const [disableReason, setDisableReason] = useState("");
  const [actionConfirmed, setActionConfirmed] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [refreshError, setRefreshError] = useState<string | null>(null);

  const termsQuery = useQuery({
    queryKey: catalogDictionaryTermsQueryKey,
    queryFn: getCatalogDictionaryTerms,
  });

  const stateMutation = useMutation({
    mutationFn: setCatalogDictionaryTermActive,

    onSuccess: async (_, variables) => {
      await queryClient.invalidateQueries({
        queryKey: catalogDictionaryTermsQueryKey,
      });

      setAction(null);
      setDisableReason("");
      setActionConfirmed(false);
      setValidationError(null);

      setSuccessMessage(
        variables.isActive
          ? "Словарный термин возвращён в работу."
          : "Словарный термин отключён. Recognition Engine больше не будет использовать его.",
      );

      setRefreshError(null);

      try {
        await onRecognitionRefresh();
      } catch (error) {
        setRefreshError(
          getApiErrorMessage(
            error,
            "Состояние термина сохранено, но повторный Recognition Preview завершился ошибкой.",
          ),
        );
      }
    },
  });

  const terms = termsQuery.data ?? [];
  const normalizedSearchText = searchText.trim().toLocaleUpperCase("ru-RU");

  const filteredTerms = terms.filter((term) => {
    const matchesFilter =
      filter === "All" ||
      (filter === "Active" && term.status === "Approved") ||
      (filter === "Disabled" && term.status === "Disabled");

    if (!matchesFilter) {
      return false;
    }

    if (normalizedSearchText.length === 0) {
      return true;
    }

    const searchableText = [
      term.phrase,
      term.normalizedPhrase,
      term.kind,
      term.targetCode ?? "",
      term.targetValue,
      term.source,
      term.productTypeId ?? "",
      term.disableReason ?? "",
    ]
      .join(" ")
      .toLocaleUpperCase("ru-RU");

    return searchableText.includes(normalizedSearchText);
  });

  const activeCount = terms.filter((term) => term.status === "Approved").length;

  const disabledCount = terms.filter(
    (term) => term.status === "Disabled",
  ).length;

  const otherCount = terms.length - activeCount - disabledCount;

  const requestIsPending = stateMutation.isPending || isRecognitionRefreshing;

  function openAction(termId: string, isActive: boolean): void {
    setAction({
      termId,
      isActive,
    });

    setDisableReason("");
    setActionConfirmed(false);
    setValidationError(null);
    setSuccessMessage(null);
    setRefreshError(null);
    stateMutation.reset();
  }

  function closeAction(): void {
    setAction(null);
    setDisableReason("");
    setActionConfirmed(false);
    setValidationError(null);
    stateMutation.reset();
  }

  function handleActionSubmit(
    event: FormEvent<HTMLFormElement>,
    term: CatalogDictionaryTerm,
  ): void {
    event.preventDefault();

    if (!action || action.termId !== term.id) {
      return;
    }

    const normalizedReason = disableReason.trim();

    if (!action.isActive && normalizedReason.length === 0) {
      setValidationError("Укажите причину отключения словарного термина.");

      return;
    }

    if (!action.isActive && normalizedReason.length > 500) {
      setValidationError(
        "Причина отключения не должна превышать 500 символов.",
      );

      return;
    }

    if (!actionConfirmed) {
      setValidationError("Подтвердите изменение состояния словарного термина.");

      return;
    }

    setValidationError(null);
    setSuccessMessage(null);
    setRefreshError(null);

    stateMutation.mutate({
      termId: term.id,
      isActive: action.isActive,
      reason: action.isActive ? null : normalizedReason,
    });
  }

  return (
    <section className="rounded-3xl border border-cyan-500/25 bg-cyan-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Словарь распознавания
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          Здесь показаны постоянные CatalogDictionaryTerm, которые использует
          Recognition Engine. Отключение не удаляет термин: правило сохраняется
          вместе с причиной решения и может быть возвращено в работу.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-amber-500/25 bg-amber-500/[0.06] p-4">
        <p className="text-sm font-medium text-amber-100">
          Изменения применяются ко всей CRM
        </p>

        <p className="mt-2 text-sm leading-6 text-amber-200/80">
          Отключённый термин перестаёт участвовать в Recognition Engine, импорте
          Excel и ассистенте. После изменения страница автоматически повторит
          текущий Recognition Preview.
        </p>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <SummaryCard
          label="Всего терминов"
          value={terms.length}
          className="text-slate-100"
        />

        <SummaryCard
          label="Активных"
          value={activeCount}
          className="text-emerald-300"
        />

        <SummaryCard
          label="Отключённых"
          value={disabledCount}
          className="text-amber-300"
        />

        <SummaryCard
          label="Другие статусы"
          value={otherCount}
          className="text-violet-300"
        />
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-[280px_1fr]">
        <div className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">Статус</span>

          <AppSelect
            ariaLabel="Фильтр словарных терминов по статусу"
            value={filter}
            options={[
              {
                value: "Active",
                label: `Активные — ${activeCount}`,
              },
              {
                value: "Disabled",
                label: `Отключённые — ${disabledCount}`,
              },
              {
                value: "All",
                label: `Все — ${terms.length}`,
              },
            ]}
            disabled={termsQuery.isLoading}
            onChange={(value) => {
              setFilter(value as DictionaryTermFilter);
              closeAction();
            }}
          />
        </div>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Поиск по словарю
          </span>

          <input
            value={searchText}
            disabled={termsQuery.isLoading}
            onChange={(event) => {
              setSearchText(event.target.value);
              closeAction();
            }}
            placeholder="Phrase, TargetCode, TargetValue, Source или ProductTypeId"
            className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-cyan-400 focus:ring-2 focus:ring-cyan-400/20"
          />
        </label>
      </div>

      {termsQuery.isLoading && (
        <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-400">
          Загружаем словарные термины...
        </div>
      )}

      {termsQuery.isError && (
        <div className="mt-6 rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          <p className="font-medium">Не удалось загрузить словарь</p>

          <p className="mt-2">
            {getApiErrorMessage(
              termsQuery.error,
              "Не удалось получить словарные термины.",
            )}
          </p>

          <button
            type="button"
            onClick={() => void termsQuery.refetch()}
            className="mt-4 rounded-xl border border-red-400/30 bg-red-500/10 px-4 py-2 text-sm font-medium text-red-100 transition hover:bg-red-500/20"
          >
            Повторить загрузку
          </button>
        </div>
      )}

      {successMessage && (
        <div className="mt-6 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-4 text-sm text-emerald-200">
          {successMessage}
        </div>
      )}

      {refreshError && (
        <div className="mt-6 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {refreshError}
        </div>
      )}

      {!termsQuery.isLoading &&
        !termsQuery.isError &&
        filteredTerms.length === 0 && (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5">
            <p className="font-medium text-white">Термины не найдены</p>

            <p className="mt-2 text-sm text-slate-400">
              Измените фильтр или поисковую строку.
            </p>
          </div>
        )}

      {filteredTerms.length > 0 && (
        <div className="mt-6 grid gap-4">
          {filteredTerms.map((term) => {
            const statusPresentation = getStatusPresentation(term.status);
            const actionIsOpen = action?.termId === term.id;
            const termIsActive = term.status === "Approved";
            const termIsDisabled = term.status === "Disabled";

            return (
              <article
                key={term.id}
                className={`rounded-2xl border p-5 ${statusPresentation.cardClassName}`}
              >
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-lg font-semibold text-white">
                        {term.phrase}
                      </h3>

                      <span
                        className={`rounded-full border px-3 py-1 text-xs font-medium ${statusPresentation.badgeClassName}`}
                      >
                        {statusPresentation.label}
                      </span>

                      <span className="rounded-full border border-blue-500/25 bg-blue-500/10 px-3 py-1 text-xs text-blue-200">
                        {term.source}
                      </span>
                    </div>

                    <p className="mt-2 font-mono text-sm text-cyan-300">
                      {term.targetCode ?? term.kind} → {term.targetValue}
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    {termIsActive && (
                      <button
                        type="button"
                        disabled={requestIsPending}
                        onClick={() => openAction(term.id, false)}
                        className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-medium text-red-200 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Отключить
                      </button>
                    )}

                    {termIsDisabled && (
                      <button
                        type="button"
                        disabled={requestIsPending}
                        onClick={() => openAction(term.id, true)}
                        className="rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-emerald-500/20 disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Вернуть в работу
                      </button>
                    )}
                  </div>
                </div>

                <dl className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  <TermValue
                    label="Нормализованная фраза"
                    value={term.normalizedPhrase}
                    monospace
                  />

                  <TermValue label="Тип знания" value={term.kind} />

                  <TermValue
                    label="Область действия"
                    value={getScopeDescription(
                      term.productTypeId,
                      currentProductTypeId,
                    )}
                  />

                  <TermValue
                    label="Priority"
                    value={term.priority.toString()}
                    monospace
                  />

                  <TermValue
                    label="Создан"
                    value={formatDateTime(term.createdAtUtc)}
                  />

                  <TermValue
                    label="Одобрен"
                    value={formatDateTime(term.approvedAtUtc)}
                  />

                  <TermValue label="TermId" value={term.id} monospace />

                  <TermValue
                    label="ProductTypeId"
                    value={term.productTypeId ?? "null — глобальный"}
                    monospace
                  />
                </dl>

                {(term.disabledAtUtc ||
                  term.disableReason ||
                  term.reactivatedAtUtc) && (
                  <div className="mt-5 rounded-2xl border border-amber-500/20 bg-black/20 p-4">
                    <h4 className="font-medium text-amber-100">
                      Последнее изменение lifecycle
                    </h4>

                    <dl className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                      <TermValue
                        label="Отключён"
                        value={formatDateTime(term.disabledAtUtc)}
                      />

                      <TermValue
                        label="Отключил"
                        value={getUserDescription(
                          term.disabledByUserDisplayName,
                          term.disabledByUserId,
                        )}
                      />

                      <TermValue
                        label="Причина отключения"
                        value={term.disableReason ?? "Не указана"}
                      />

                      <TermValue
                        label="Возвращён в работу"
                        value={formatDateTime(term.reactivatedAtUtc)}
                      />

                      <TermValue
                        label="Вернул в работу"
                        value={getUserDescription(
                          term.reactivatedByUserDisplayName,
                          term.reactivatedByUserId,
                        )}
                      />
                    </dl>
                  </div>
                )}

                {!termIsActive && !termIsDisabled && (
                  <div className="mt-5 rounded-2xl border border-violet-500/20 bg-violet-500/[0.06] p-4 text-sm text-violet-200">
                    Термин находится в статусе {term.status}. Операции
                    отключения и повторного включения для этого статуса
                    недоступны.
                  </div>
                )}

                {actionIsOpen && (
                  <form
                    onSubmit={(event) => handleActionSubmit(event, term)}
                    className={
                      action.isActive
                        ? "mt-5 rounded-2xl border border-emerald-500/25 bg-emerald-500/[0.06] p-5"
                        : "mt-5 rounded-2xl border border-red-500/25 bg-red-500/[0.06] p-5"
                    }
                  >
                    <h4 className="font-semibold text-white">
                      {action.isActive
                        ? "Вернуть термин в работу"
                        : "Отключить словарный термин"}
                    </h4>

                    <p className="mt-2 text-sm leading-6 text-slate-300">
                      {action.isActive
                        ? "После подтверждения Recognition Engine снова начнёт использовать этот термин."
                        : "Термин останется в PostgreSQL и истории, но перестанет участвовать в распознавании."}
                    </p>

                    {!action.isActive && (
                      <label className="mt-4 grid gap-2">
                        <span className="text-sm font-medium text-red-100">
                          Причина отключения
                        </span>

                        <textarea
                          value={disableReason}
                          rows={3}
                          maxLength={500}
                          disabled={requestIsPending}
                          onChange={(event) => {
                            setDisableReason(event.target.value);
                            setValidationError(null);
                            stateMutation.reset();
                          }}
                          placeholder="Например: термин создаёт ложное распознавание серии товара."
                          className="resize-y rounded-2xl border border-red-500/20 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-red-400"
                        />

                        <span className="text-xs text-slate-500">
                          {disableReason.length}/500. Причина сохранится в
                          PostgreSQL.
                        </span>
                      </label>
                    )}

                    <label className="mt-4 flex items-start gap-3 rounded-xl border border-white/10 bg-black/20 p-4">
                      <input
                        type="checkbox"
                        checked={actionConfirmed}
                        disabled={requestIsPending}
                        onChange={(event) => {
                          setActionConfirmed(event.target.checked);
                          setValidationError(null);
                          stateMutation.reset();
                        }}
                        className="mt-1 h-4 w-4 accent-cyan-500"
                      />

                      <span className="text-sm leading-6 text-slate-300">
                        Я понимаю, что это постоянное изменение словаря и оно
                        повлияет на импорт, ассистента и Recognition Engine.
                      </span>
                    </label>

                    {validationError && (
                      <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
                        {validationError}
                      </div>
                    )}

                    {stateMutation.isError && (
                      <div className="mt-4 rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
                        {getApiErrorMessage(
                          stateMutation.error,
                          "Не удалось изменить состояние словарного термина.",
                        )}
                      </div>
                    )}

                    <div className="mt-4 flex flex-wrap gap-3">
                      <button
                        type="submit"
                        disabled={requestIsPending}
                        className={
                          action.isActive
                            ? "rounded-xl bg-emerald-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
                            : "rounded-xl bg-red-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-400 disabled:cursor-not-allowed disabled:opacity-50"
                        }
                      >
                        {stateMutation.isPending
                          ? "Сохраняем..."
                          : action.isActive
                            ? "Подтвердить включение"
                            : "Подтвердить отключение"}
                      </button>

                      <button
                        type="button"
                        disabled={requestIsPending}
                        onClick={closeAction}
                        className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        Отмена
                      </button>
                    </div>
                  </form>
                )}
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}

function SummaryCard({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className: string;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p className={`mt-2 text-3xl font-semibold ${className}`}>{value}</p>
    </div>
  );
}

function TermValue({
  label,
  value,
  monospace = false,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-3">
      <dt className="text-xs text-slate-500">{label}</dt>

      <dd
        className={`mt-2 break-words text-sm text-slate-200 ${
          monospace ? "font-mono" : ""
        }`}
      >
        {value}
      </dd>
    </div>
  );
}

function getStatusPresentation(status: CatalogDictionaryTermStatus): {
  label: string;
  cardClassName: string;
  badgeClassName: string;
} {
  switch (status) {
    case "Approved":
      return {
        label: "Активен",
        cardClassName: "border-emerald-500/20 bg-emerald-500/[0.035]",
        badgeClassName:
          "border-emerald-500/30 bg-emerald-500/10 text-emerald-200",
      };

    case "Disabled":
      return {
        label: "Отключён",
        cardClassName: "border-amber-500/25 bg-amber-500/[0.04]",
        badgeClassName: "border-amber-500/30 bg-amber-500/10 text-amber-200",
      };

    case "Pending":
      return {
        label: "Ожидает одобрения",
        cardClassName: "border-violet-500/20 bg-violet-500/[0.035]",
        badgeClassName: "border-violet-500/30 bg-violet-500/10 text-violet-200",
      };

    case "Rejected":
      return {
        label: "Отклонён",
        cardClassName: "border-red-500/20 bg-red-500/[0.035]",
        badgeClassName: "border-red-500/30 bg-red-500/10 text-red-200",
      };
  }
}

function getScopeDescription(
  productTypeId: string | null,
  currentProductTypeId: string | null,
): string {
  if (!productTypeId) {
    return "Глобальный термин";
  }

  if (productTypeId === currentProductTypeId) {
    return "Текущий выбранный тип товара";
  }

  return "Другой тип товара";
}

function getUserDescription(
  displayName: string | null,
  userId: string | null,
): string {
  if (displayName && userId) {
    return `${displayName} — ${userId}`;
  }

  if (displayName) {
    return displayName;
  }

  if (userId) {
    return userId;
  }

  return "Не указан";
}

function formatDateTime(value: string | null): string {
  if (!value) {
    return "Не указано";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
