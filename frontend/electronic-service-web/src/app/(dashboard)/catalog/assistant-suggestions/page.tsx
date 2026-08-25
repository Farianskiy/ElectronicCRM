"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { approveDictionarySuggestion } from "@/features/dictionarySuggestions/api/approveDictionarySuggestion";
import { getDictionarySuggestions } from "@/features/dictionarySuggestions/api/getDictionarySuggestions";
import { rejectDictionarySuggestion } from "@/features/dictionarySuggestions/api/rejectDictionarySuggestion";
import { DictionarySuggestionApprovalForm } from "@/features/dictionarySuggestions/ui/DictionarySuggestionApprovalForm";
import type {
  ApproveDictionarySuggestionRequest,
  AssistantDictionarySuggestion,
  DictionarySuggestionStatusFilter,
} from "@/features/dictionarySuggestions/model/types";
import { RequireTechnicalUser } from "@/features/auth/ui/RequireTechnicalUser";
import { formatDate, formatPercent } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";

const statusFilters: Array<{
  value: DictionarySuggestionStatusFilter;
  label: string;
}> = [
  { value: "Pending", label: "Ожидают проверки" },
  { value: "Approved", label: "Одобрены" },
  { value: "Rejected", label: "Отклонены" },
  { value: "All", label: "Все" },
];

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data;

    if (typeof responseData === "string") {
      return responseData;
    }

    if (typeof responseData?.detail === "string") {
      return responseData.detail;
    }

    if (typeof responseData?.message === "string") {
      return responseData.message;
    }
  }

  return "Произошла неизвестная ошибка.";
}

function getStatusBadgeClass(status: string): string {
  if (status === "Approved") {
    return "bg-green-500/15 text-green-300 border-green-500/30";
  }

  if (status === "Rejected") {
    return "bg-red-500/15 text-red-300 border-red-500/30";
  }

  return "bg-amber-500/15 text-amber-300 border-amber-500/30";
}

function getStatusLabel(status: string): string {
  if (status === "Approved") {
    return "Одобрено";
  }

  if (status === "Rejected") {
    return "Отклонено";
  }

  return "Ожидает проверки";
}

function getFeedbackTypeLabel(feedbackType: string): string {
  if (feedbackType === "Accepted") {
    return "Принято без изменения";
  }

  if (feedbackType === "Corrected") {
    return "Исправлено пользователем";
  }

  if (feedbackType === "Rejected") {
    return "Отклонено пользователем";
  }

  if (feedbackType === "AddedManually") {
    return "Добавлено вручную";
  }

  if (feedbackType === "ConflictResolved") {
    return "Разрешён конфликт";
  }

  return feedbackType;
}

function getSourcePresentation(source: string): {
  label: string;
  description: string;
  className: string;
} {
  if (source === "RecognitionLearning") {
    return {
      label: "Самообучение",
      description:
        "Предложение создано автоматически после повторяющихся пользовательских исправлений.",
      className: "border-violet-500/30 bg-violet-500/15 text-violet-200",
    };
  }

  if (source === "Assistant") {
    return {
      label: "Ассистент",
      description: "Неизвестная фраза была обнаружена при работе ассистента.",
      className: "border-blue-500/30 bg-blue-500/15 text-blue-200",
    };
  }

  if (source === "ImportRecognition") {
    return {
      label: "Распознавание импорта",
      description: "Закономерность обнаружена во время анализа Excel-импорта.",
      className: "border-cyan-500/30 bg-cyan-500/15 text-cyan-200",
    };
  }

  if (source === "UserCorrection") {
    return {
      label: "Исправление пользователя",
      description: "Предложение основано на пользовательском исправлении.",
      className: "border-amber-500/30 bg-amber-500/15 text-amber-200",
    };
  }

  if (source === "MlRecognition") {
    return {
      label: "ML-распознавание",
      description: "Предложение создано на основе результата ML-модели.",
      className: "border-fuchsia-500/30 bg-fuchsia-500/15 text-fuchsia-200",
    };
  }

  return {
    label: source,
    description: "Источник предложения не имеет отдельного представления.",
    className: "border-white/10 bg-white/[0.06] text-slate-300",
  };
}

export default function CatalogAssistantSuggestionsPage() {
  return (
    <RequireTechnicalUser>
      <DictionarySuggestionsContent />
    </RequireTechnicalUser>
  );
}

function DictionarySuggestionsContent() {
  const queryClient = useQueryClient();

  const [status, setStatus] =
    useState<DictionarySuggestionStatusFilter>("Pending");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [reviewComments, setReviewComments] = useState<Record<string, string>>(
    {},
  );

  const suggestionsQuery = useQuery({
    queryKey: ["dictionary-suggestions", status, page, pageSize],
    queryFn: () =>
      getDictionarySuggestions({
        status,
        page,
        pageSize,
      }),
  });

  const approveMutation = useMutation({
    mutationFn: approveDictionarySuggestion,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["dictionary-suggestions"],
      });
    },
  });

  const rejectMutation = useMutation({
    mutationFn: rejectDictionarySuggestion,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["dictionary-suggestions"],
      });
    },
  });

  const suggestions = suggestionsQuery.data?.items ?? [];
  const totalCount = suggestionsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const reviewIsPending = approveMutation.isPending || rejectMutation.isPending;

  function handleStatusChange(nextStatus: DictionarySuggestionStatusFilter) {
    setStatus(nextStatus);
    setPage(1);
  }

  function updateComment(suggestionId: string, value: string) {
    setReviewComments((current) => ({
      ...current,
      [suggestionId]: value,
    }));
  }

  function approveSuggestion(
    suggestion: AssistantDictionarySuggestion,
    request: ApproveDictionarySuggestionRequest,
  ) {
    approveMutation.mutate({
      suggestionId: suggestion.id,
      request,
    });
  }

  function rejectSuggestion(suggestion: AssistantDictionarySuggestion) {
    rejectMutation.mutate({
      suggestionId: suggestion.id,
      request: {
        reviewComment: reviewComments[suggestion.id] || null,
      },
    });
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Предложения словаря"
        description="Проверка новых знаний, найденных ассистентом, импортом и механизмом самообучения."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div>
            <h2 className="text-xl font-semibold text-white">
              Очередь модерации
            </h2>
            <p className="mt-1 text-sm text-slate-400">
              Одобрение создаёт постоянный термин словаря. Отклонение сохраняет
              решение и не позволяет системе автоматически принять ошибочную
              закономерность.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            {statusFilters.map((filter) => (
              <button
                key={filter.value}
                type="button"
                onClick={() => handleStatusChange(filter.value)}
                className={
                  status === filter.value
                    ? "rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-white"
                    : "rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/[0.1]"
                }
              >
                {filter.label}
              </button>
            ))}
          </div>
        </div>
      </section>

      {suggestionsQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {getErrorMessage(suggestionsQuery.error)}
        </section>
      )}

      {(approveMutation.isError || rejectMutation.isError) && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {approveMutation.isError
            ? getErrorMessage(approveMutation.error)
            : getErrorMessage(rejectMutation.error)}
        </section>
      )}

      {suggestionsQuery.isLoading ? (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем предложения...
        </section>
      ) : suggestions.length === 0 ? (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
          <h2 className="text-xl font-semibold text-white">Предложений нет</h2>
          <p className="mt-2 text-sm text-slate-400">
            Для выбранного фильтра ничего не найдено.
          </p>
        </section>
      ) : (
        <section className="grid gap-4">
          {suggestions.map((suggestion) => (
            <SuggestionCard
              key={suggestion.id}
              suggestion={suggestion}
              reviewComment={reviewComments[suggestion.id] ?? ""}
              onReviewCommentChange={(value) =>
                updateComment(suggestion.id, value)
              }
              onApprove={(request) => approveSuggestion(suggestion, request)}
              onReject={() => rejectSuggestion(suggestion)}
              isReviewPending={reviewIsPending}
            />
          ))}
        </section>
      )}

      <section className="flex items-center justify-between rounded-3xl border border-white/10 bg-white/[0.04] p-4">
        <button
          type="button"
          disabled={page <= 1}
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 disabled:opacity-40"
        >
          Назад
        </button>

        <p className="text-sm text-slate-400">
          Страница {page} из {totalPages}. Всего: {totalCount}
        </p>

        <button
          type="button"
          disabled={page >= totalPages}
          onClick={() =>
            setPage((current) => Math.min(totalPages, current + 1))
          }
          className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 disabled:opacity-40"
        >
          Вперёд
        </button>
      </section>
    </div>
  );
}

function SuggestionCard({
  suggestion,
  reviewComment,
  onReviewCommentChange,
  onApprove,
  onReject,
  isReviewPending,
}: {
  suggestion: AssistantDictionarySuggestion;
  reviewComment: string;
  onReviewCommentChange: (value: string) => void;
  onApprove: (request: ApproveDictionarySuggestionRequest) => void;
  onReject: () => void;
  isReviewPending: boolean;
}) {
  const canReview = suggestion.status === "Pending";
  const isRecognitionLearning = suggestion.source === "RecognitionLearning";
  const sourcePresentation = getSourcePresentation(suggestion.source);

  const productTypeValue = suggestion.productTypeName
    ? `${suggestion.productTypeName} — ${suggestion.productTypeCode}`
    : (suggestion.productTypeCode ?? "Глобальная область");

  const characteristicValue = suggestion.characteristicName
    ? `${suggestion.characteristicName} — ${suggestion.characteristicCode}`
    : (suggestion.characteristicCode ??
      suggestion.suggestedTargetCode ??
      "Не указана");

  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h3 className="text-xl font-semibold text-white">
              {suggestion.unknownPhrase}
              <span className="mx-3 text-slate-500">→</span>
              <span className="text-teal-300">
                {suggestion.suggestedTargetValue}
              </span>
            </h3>

            <span
              className={`rounded-full border px-3 py-1 text-xs font-medium ${getStatusBadgeClass(
                suggestion.status,
              )}`}
            >
              {getStatusLabel(suggestion.status)}
            </span>

            <span
              className={`rounded-full border px-3 py-1 text-xs font-medium ${sourcePresentation.className}`}
            >
              {sourcePresentation.label}
            </span>

            {suggestion.generatedAutomatically && (
              <span className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 text-xs font-medium text-violet-200">
                Создано автоматически
              </span>
            )}
          </div>

          <p className="mt-2 text-sm text-slate-400">
            {sourcePresentation.description}
          </p>

          <p className="mt-1 text-sm text-slate-500">
            Создано: {formatDate(suggestion.createdAtUtc)}
          </p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-right">
          <p className="text-xs text-slate-400">Уверенность</p>
          <p className="mt-1 text-lg font-semibold text-teal-300">
            {formatPercent(suggestion.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-5 grid gap-4 lg:grid-cols-2">
        <section className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <h4 className="font-semibold text-white">Предлагаемое правило</h4>

          <div className="mt-3 grid gap-2 text-sm">
            <InfoRow
              label="Фраза в наименовании"
              value={suggestion.unknownPhrase}
            />
            <InfoRow label="Характеристика" value={characteristicValue} />
            <InfoRow
              label="Итоговое значение"
              value={suggestion.suggestedTargetValue}
            />
            <InfoRow label="Тип знания" value={suggestion.suggestedKind} />
          </div>
        </section>

        <section className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <h4 className="font-semibold text-white">Область действия</h4>

          <div className="mt-3 grid gap-2 text-sm">
            <InfoRow label="Тип товара" value={productTypeValue} />
            <InfoRow
              label="Область"
              value={
                suggestion.productTypeId
                  ? "Только выбранный тип товара"
                  : "Все типы товаров"
              }
            />
          </div>

          <p className="mt-3 text-sm text-slate-400">
            {suggestion.productTypeId
              ? "После одобрения термин будет применяться только внутри указанного типа товара."
              : "После одобрения термин станет глобальным и сможет использоваться для разных типов товара."}
          </p>
        </section>
      </div>

      {(suggestion.generatedAutomatically ||
        suggestion.occurrenceCount > 1) && (
        <section className="mt-5 rounded-2xl border border-violet-500/20 bg-violet-500/[0.06] p-4">
          <h4 className="font-semibold text-white">
            Доказательства закономерности
          </h4>

          <p className="mt-1 text-sm text-slate-400">
            Эти значения показывают, почему система решила вынести правило на
            проверку Technical-пользователю.
          </p>

          <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <EvidenceCard
              label="Всего наблюдений"
              value={suggestion.occurrenceCount}
              className="text-slate-100"
            />
            <EvidenceCard
              label="Принято без изменения"
              value={suggestion.acceptedEvidenceCount}
              className="text-green-300"
            />
            <EvidenceCard
              label="Исправлено на это значение"
              value={suggestion.correctedEvidenceCount}
              className="text-cyan-300"
            />
            <EvidenceCard
              label="Отклонено"
              value={suggestion.rejectedEvidenceCount}
              className="text-red-300"
            />
          </div>

          {suggestion.evidenceExamples.length > 0 && (
            <div className="mt-5 border-t border-violet-500/20 pt-5">
              <h5 className="font-semibold text-white">
                Реальные примеры товаров
              </h5>

              <p className="mt-1 text-sm leading-6 text-slate-400">
                Это конкретные товары, решения по которым были объединены в одно
                предложение. Показано не больше пяти примеров.
              </p>

              <div className="mt-4 grid gap-3">
                {suggestion.evidenceExamples.map((evidence) => {
                  const spanValue =
                    evidence.spanStart !== null && evidence.spanLength !== null
                      ? `${evidence.spanStart}..${
                          evidence.spanStart + evidence.spanLength
                        }`
                      : "Не сохранён";

                  return (
                    <article
                      key={evidence.feedbackId}
                      className="rounded-2xl border border-white/10 bg-black/20 p-4"
                    >
                      <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
                        <div>
                          <p className="font-medium text-white">
                            {evidence.productName}
                          </p>

                          <div className="mt-2 flex flex-wrap gap-2">
                            <span className="rounded-full border border-cyan-500/30 bg-cyan-500/10 px-3 py-1 text-xs text-cyan-200">
                              {getFeedbackTypeLabel(evidence.feedbackType)}
                            </span>

                            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-300">
                              Качество метки: {evidence.labelQuality}
                            </span>

                            {evidence.suggestedSource && (
                              <span className="rounded-full border border-blue-500/30 bg-blue-500/10 px-3 py-1 text-xs text-blue-200">
                                Источник: {evidence.suggestedSource}
                              </span>
                            )}
                          </div>
                        </div>

                        <div className="text-left lg:text-right">
                          <p className="text-xs text-slate-500">Confidence</p>

                          <p className="mt-1 font-mono text-sm text-teal-300">
                            {evidence.suggestedConfidence !== null
                              ? formatPercent(evidence.suggestedConfidence)
                              : "—"}
                          </p>
                        </div>
                      </div>

                      <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                        <div className="rounded-xl bg-white/[0.03] p-3">
                          <p className="text-xs text-slate-500">
                            Фрагмент Recognition
                          </p>

                          <p className="mt-2 font-mono text-sm text-slate-100">
                            {evidence.suggestedRawValue ?? "—"}
                          </p>
                        </div>

                        <div className="rounded-xl bg-white/[0.03] p-3">
                          <p className="text-xs text-slate-500">
                            Распознанное значение
                          </p>

                          <p className="mt-2 font-mono text-sm text-amber-200">
                            {evidence.suggestedNormalizedValue ?? "—"}
                          </p>
                        </div>

                        <div className="rounded-xl bg-white/[0.03] p-3">
                          <p className="text-xs text-slate-500">
                            Итог пользователя
                          </p>

                          <p className="mt-2 font-mono text-sm text-green-200">
                            {evidence.finalNormalizedValue ??
                              "Значение удалено"}
                          </p>
                        </div>

                        <div className="rounded-xl bg-white/[0.03] p-3">
                          <p className="text-xs text-slate-500">
                            Span в наименовании
                          </p>

                          <p className="mt-2 font-mono text-sm text-slate-100">
                            {spanValue}
                          </p>
                        </div>
                      </div>

                      {evidence.finalizedAtUtc && (
                        <p className="mt-3 text-xs text-slate-500">
                          Feedback финализирован:{" "}
                          {formatDate(evidence.finalizedAtUtc)}
                        </p>
                      )}
                    </article>
                  );
                })}
              </div>
            </div>
          )}
        </section>
      )}

      {isRecognitionLearning ? (
        <details className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
          <summary className="cursor-pointer font-medium text-slate-300">
            Показать технические сведения
          </summary>

          <div className="mt-4 grid gap-2 text-sm">
            <InfoRow
              label="Нормализованная фраза"
              value={suggestion.normalizedUnknownPhrase}
            />
            <InfoRow label="Источник" value={suggestion.source} />
            <InfoRow
              label="ProductTypeId"
              value={suggestion.productTypeId ?? "—"}
            />
            <InfoRow
              label="CharacteristicDefinitionId"
              value={suggestion.characteristicDefinitionId ?? "—"}
            />
            <InfoRow
              label="Техническое описание"
              value={suggestion.originalMessage}
            />
          </div>
        </details>
      ) : (
        <section className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
          <h4 className="font-semibold text-white">
            Исходный запрос пользователя
          </h4>
          <p className="mt-2 text-slate-100">{suggestion.originalMessage}</p>
        </section>
      )}

      {suggestion.reviewedAtUtc && (
        <section className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
          <h4 className="font-semibold text-white">Результат проверки</h4>

          <p className="mt-2 text-sm text-slate-300">
            Проверено: {formatDate(suggestion.reviewedAtUtc)}
          </p>

          {suggestion.status === "Approved" &&
            suggestion.approvedPhrase &&
            suggestion.approvedTargetValue && (
              <div className="mt-4 grid gap-3">
                <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-4">
                  <p className="text-sm font-medium text-green-100">
                    Созданное правило
                  </p>

                  <p className="mt-2 break-words font-mono text-green-200">
                    {suggestion.approvedPhrase} →{" "}
                    {suggestion.approvedKind === "Characteristic"
                      ? `${suggestion.approvedTargetCode ?? "характеристика"} = `
                      : ""}
                    {suggestion.approvedTargetValue}
                  </p>
                </div>

                <div className="grid gap-2 text-sm lg:grid-cols-2">
                  <InfoRow
                    label="Тип знания"
                    value={suggestion.approvedKind ?? "—"}
                  />
                  <InfoRow
                    label="Область типа товара"
                    value={
                      suggestion.approvedProductTypeName
                        ? `${suggestion.approvedProductTypeName} — ${suggestion.approvedProductTypeCode}`
                        : "Глобальная"
                    }
                  />
                  <InfoRow
                    label="Priority"
                    value={suggestion.approvedPriority?.toString() ?? "—"}
                  />
                  <InfoRow
                    label="Созданный DictionaryTermId"
                    value={suggestion.createdDictionaryTermId ?? "—"}
                  />
                </div>
              </div>
            )}

          {suggestion.reviewComment && (
            <div className="mt-4 rounded-xl bg-white/[0.03] p-3">
              <p className="text-xs text-slate-400">Комментарий Technical</p>
              <p className="mt-2 text-slate-100">{suggestion.reviewComment}</p>
            </div>
          )}
        </section>
      )}

      {canReview && (
        <>
          <DictionarySuggestionApprovalForm
            suggestion={suggestion}
            reviewComment={reviewComment}
            disabled={isReviewPending}
            onReviewCommentChange={onReviewCommentChange}
            onApprove={onApprove}
          />

          <section className="mt-4 rounded-2xl border border-red-500/20 bg-red-500/[0.05] p-4">
            <h4 className="font-semibold text-white">Отклонить предложение</h4>

            <p className="mt-2 text-sm leading-6 text-slate-400">
              Отклонение не создаёт словарный термин. Комментарий из формы выше
              будет сохранён как причина решения.
            </p>

            <button
              type="button"
              disabled={isReviewPending}
              onClick={onReject}
              className="mt-4 rounded-xl bg-red-500 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              {isRecognitionLearning
                ? "Отклонить закономерность"
                : "Отклонить предложение"}
            </button>
          </section>
        </>
      )}
    </article>
  );
}

function EvidenceCard({
  label,
  value,
  className,
}: {
  label: string;
  value: number;
  className: string;
}) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-3">
      <p className="text-xs text-slate-400">{label}</p>
      <p className={`mt-2 text-2xl font-semibold ${className}`}>{value}</p>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 rounded-xl bg-white/[0.03] px-3 py-2">
      <span className="text-slate-400">{label}</span>
      <span className="text-right font-medium text-slate-100">{value}</span>
    </div>
  );
}
