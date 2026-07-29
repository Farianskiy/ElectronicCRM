"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { applyCatalogImportBatch } from "../api/applyCatalogImportBatch";
import { rejectCatalogImportBatch } from "../api/rejectCatalogImportBatch";
import { requestCatalogImportChanges } from "../api/requestCatalogImportChanges";
import { catalogImportQueryKeys } from "../model/queryKeys";

interface CatalogImportReviewDecisionPanelProps {
  batchId: string;
  originalFileName: string;
  rowsCount: number;
  validRowsCount: number;
  errorRowsCount: number;
  canRequestChanges: boolean;
  canReject: boolean;
  canApply: boolean;
}

type ReviewDecisionAction = "request-changes" | "reject" | "apply";

interface ReviewDecisionPayload {
  action: ReviewDecisionAction;
  text?: string;
}

const textareaClassName = [
  "min-h-36 w-full resize-y rounded-2xl",
  "border border-white/10 bg-black/30",
  "px-4 py-3 text-sm leading-6 text-slate-100",
  "outline-none transition",
  "placeholder:text-slate-600",
  "hover:border-white/20",
  "focus:border-teal-400",
  "focus:ring-2 focus:ring-teal-400/20",
  "disabled:cursor-not-allowed disabled:opacity-50",
].join(" ");

export function CatalogImportReviewDecisionPanel({
  batchId,
  originalFileName,
  rowsCount,
  validRowsCount,
  errorRowsCount,
  canRequestChanges,
  canReject,
  canApply,
}: CatalogImportReviewDecisionPanelProps) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const [selectedAction, setSelectedAction] =
    useState<ReviewDecisionAction | null>(null);

  const [comment, setComment] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  const decisionMutation = useMutation({
    mutationFn: async (payload: ReviewDecisionPayload): Promise<void> => {
      switch (payload.action) {
        case "request-changes":
          await requestCatalogImportChanges(batchId, {
            comment: payload.text ?? "",
          });

          return;

        case "reject":
          await rejectCatalogImportBatch(batchId, {
            reason: payload.text ?? "",
          });

          return;

        case "apply":
          await applyCatalogImportBatch(batchId);

          return;
      }
    },

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.reviewQueueRoot,
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.history(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.appliedProductsRoot(batchId),
        }),
      ]);

      router.replace("/catalog/import-reviews");
    },

    onError: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.reviewQueueRoot,
        }),
      ]);
    },
  });

  const hasAvailableActions = canRequestChanges || canReject || canApply;

  if (!hasAvailableActions) {
    return null;
  }

  const isBusy = decisionMutation.isPending;

  function selectAction(action: ReviewDecisionAction): void {
    if (isBusy) {
      return;
    }

    setSelectedAction(action);
    setComment("");
    setValidationError(null);
    decisionMutation.reset();
  }

  function closeForm(): void {
    if (isBusy) {
      return;
    }

    setSelectedAction(null);
    setComment("");
    setValidationError(null);
    decisionMutation.reset();
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!selectedAction) {
      return;
    }

    const normalizedComment = comment.trim();

    if (
      selectedAction === "request-changes" &&
      normalizedComment.length === 0
    ) {
      setValidationError("Укажите, что именно пользователь должен исправить.");

      return;
    }

    if (selectedAction === "reject" && normalizedComment.length === 0) {
      setValidationError("Укажите причину окончательного отклонения.");

      return;
    }

    if (selectedAction === "reject") {
      const confirmed = window.confirm(
        [
          `Окончательно отклонить пакет «${originalFileName}»?`,
          "",
          "Пользователь больше не сможет исправить и повторно отправить этот пакет.",
          "Для повторной попытки потребуется создать новый импорт.",
        ].join("\n"),
      );

      if (!confirmed) {
        return;
      }
    }

    if (selectedAction === "apply") {
      const confirmed = window.confirm(
        [
          `Применить пакет «${originalFileName}» к каталогу?`,
          "",
          `Будет создано товаров: ${validRowsCount}`,
          "",
          "Операция изменит рабочий каталог и не может быть отменена через интерфейс импорта.",
        ].join("\n"),
      );

      if (!confirmed) {
        return;
      }
    }

    setValidationError(null);

    decisionMutation.mutate({
      action: selectedAction,
      text: selectedAction === "apply" ? undefined : normalizedComment,
    });
  }

  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">Решение по пакету</h2>

        <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
          Выберите результат технической проверки. Решение немедленно изменит
          статус пакета и будет видно его автору.
        </p>
      </div>

      <div className="mt-5 grid gap-4 lg:grid-cols-3">
        {canRequestChanges && (
          <DecisionCard
            title="Вернуть на исправление"
            description="Пользователь сможет исправить mapping или отдельные строки и повторно отправить пакет."
            selected={selectedAction === "request-changes"}
            disabled={isBusy}
            buttonLabel="Запросить изменения"
            buttonClassName="border-orange-500/30 bg-orange-500/10 text-orange-200 hover:bg-orange-500/20"
            onClick={() => selectAction("request-changes")}
          />
        )}

        {canReject && (
          <DecisionCard
            title="Отклонить окончательно"
            description="Пакет будет закрыт без возможности редактирования и повторной отправки."
            selected={selectedAction === "reject"}
            disabled={isBusy}
            buttonLabel="Отклонить пакет"
            buttonClassName="border-red-500/30 bg-red-500/10 text-red-200 hover:bg-red-500/20"
            onClick={() => selectAction("reject")}
          />
        )}

        {canApply && (
          <DecisionCard
            title="Применить в каталог"
            description="Все валидные строки будут превращены в новые товары рабочего каталога."
            selected={selectedAction === "apply"}
            disabled={isBusy}
            buttonLabel="Выбрать применение"
            buttonClassName="border-green-500/30 bg-green-500/10 text-green-200 hover:bg-green-500/20"
            onClick={() => selectAction("apply")}
          />
        )}
      </div>

      {selectedAction && (
        <form
          onSubmit={handleSubmit}
          className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5"
        >
          {selectedAction === "request-changes" && (
            <div className="grid gap-3">
              <div>
                <h3 className="font-semibold text-orange-100">
                  Комментарий для исправления
                </h3>

                <p className="mt-1 text-sm leading-6 text-slate-400">
                  Опишите конкретные проблемы: неверное сопоставление,
                  производитель, характеристика или строка Excel.
                </p>
              </div>

              <textarea
                value={comment}
                disabled={isBusy}
                onChange={(event) => {
                  setComment(event.target.value);
                  setValidationError(null);
                  decisionMutation.reset();
                }}
                className={textareaClassName}
                placeholder="Например: в строках 14–20 неверно определён производитель. Также колонку «Номинальный ток» нужно сопоставить с характеристикой..."
              />
            </div>
          )}

          {selectedAction === "reject" && (
            <div className="grid gap-3">
              <div>
                <h3 className="font-semibold text-red-100">
                  Причина окончательного отклонения
                </h3>

                <p className="mt-1 text-sm leading-6 text-slate-400">
                  Это решение закрывает пакет. Для исправляемых проблем
                  используйте возврат на доработку.
                </p>
              </div>

              <textarea
                value={comment}
                disabled={isBusy}
                onChange={(event) => {
                  setComment(event.target.value);
                  setValidationError(null);
                  decisionMutation.reset();
                }}
                className={textareaClassName}
                placeholder="Укажите причину окончательного отклонения пакета..."
              />
            </div>
          )}

          {selectedAction === "apply" && (
            <div className="grid gap-5">
              <div>
                <h3 className="font-semibold text-green-100">
                  Применение пакета
                </h3>

                <p className="mt-1 text-sm leading-6 text-slate-400">
                  Backend создаст товары, значения характеристик и записи аудита
                  в одной транзакции.
                </p>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <SummaryCard label="Всего строк" value={rowsCount} />

                <SummaryCard
                  label="Корректных"
                  value={validRowsCount}
                  valueClassName="text-green-300"
                />

                <SummaryCard
                  label="С ошибками"
                  value={errorRowsCount}
                  valueClassName={
                    errorRowsCount > 0 ? "text-red-300" : "text-white"
                  }
                />
              </div>

              {errorRowsCount > 0 && (
                <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
                  В пакете остаются ошибочные строки. Применение должно быть
                  недоступно. Обновите страницу, если backend всё ещё возвращает
                  canApply = true.
                </div>
              )}

              <div className="rounded-2xl border border-amber-500/30 bg-amber-500/[0.07] p-4 text-sm leading-6 text-amber-100">
                Проверьте артикулы особенно внимательно. Существующий в каталоге
                артикул остановит всё применение, а не только одну строку.
              </div>
            </div>
          )}

          {validationError && (
            <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
              {validationError}
            </div>
          )}

          {decisionMutation.isError && (
            <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
              {getApiErrorMessage(
                decisionMutation.error,
                getDecisionErrorMessage(selectedAction),
              )}
            </div>
          )}

          <div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <button
              type="button"
              disabled={isBusy}
              onClick={closeForm}
              className="rounded-2xl border border-white/10 bg-white/[0.05] px-5 py-3 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
            >
              Отмена
            </button>

            <button
              type="submit"
              disabled={
                isBusy || (selectedAction === "apply" && errorRowsCount > 0)
              }
              className={getSubmitButtonClassName(selectedAction)}
            >
              {getSubmitButtonLabel(selectedAction, isBusy)}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function DecisionCard({
  title,
  description,
  selected,
  disabled,
  buttonLabel,
  buttonClassName,
  onClick,
}: {
  title: string;
  description: string;
  selected: boolean;
  disabled: boolean;
  buttonLabel: string;
  buttonClassName: string;
  onClick: () => void;
}) {
  return (
    <div
      className={[
        "flex flex-col rounded-2xl border p-5 transition",
        selected
          ? "border-white/30 bg-white/[0.08]"
          : "border-white/10 bg-black/20",
      ].join(" ")}
    >
      <h3 className="font-semibold text-white">{title}</h3>

      <p className="mt-2 flex-1 text-sm leading-6 text-slate-400">
        {description}
      </p>

      <button
        type="button"
        disabled={disabled}
        onClick={onClick}
        className={[
          "mt-5 rounded-xl border px-4 py-2.5",
          "text-sm font-semibold transition",
          "disabled:cursor-not-allowed disabled:opacity-50",
          buttonClassName,
        ].join(" ")}
      >
        {selected ? "Выбрано" : buttonLabel}
      </button>
    </div>
  );
}

function SummaryCard({
  label,
  value,
  valueClassName = "text-white",
}: {
  label: string;
  value: number;
  valueClassName?: string;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p className={["mt-2 text-2xl font-semibold", valueClassName].join(" ")}>
        {value}
      </p>
    </div>
  );
}

function getDecisionErrorMessage(action: ReviewDecisionAction): string {
  switch (action) {
    case "request-changes":
      return "Не удалось вернуть пакет на исправление.";

    case "reject":
      return "Не удалось окончательно отклонить пакет.";

    case "apply":
      return "Не удалось применить пакет к каталогу.";
  }
}

function getSubmitButtonLabel(
  action: ReviewDecisionAction,
  isPending: boolean,
): string {
  if (isPending) {
    switch (action) {
      case "request-changes":
        return "Возвращаем...";

      case "reject":
        return "Отклоняем...";

      case "apply":
        return "Создаём товары...";
    }
  }

  switch (action) {
    case "request-changes":
      return "Вернуть на исправление";

    case "reject":
      return "Отклонить окончательно";

    case "apply":
      return "Применить в каталог";
  }
}

function getSubmitButtonClassName(action: ReviewDecisionAction): string {
  const baseClassName = [
    "rounded-2xl px-6 py-3",
    "text-sm font-semibold text-white",
    "transition",
    "disabled:cursor-not-allowed disabled:opacity-50",
  ].join(" ");

  switch (action) {
    case "request-changes":
      return `${baseClassName} bg-orange-500 hover:bg-orange-400`;

    case "reject":
      return `${baseClassName} bg-red-500 hover:bg-red-400`;

    case "apply":
      return `${baseClassName} bg-green-600 hover:bg-green-500`;
  }
}
