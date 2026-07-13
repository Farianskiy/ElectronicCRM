"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { approveDictionarySuggestion } from "@/features/assistantSuggestions/api/approveDictionarySuggestion";
import { getDictionarySuggestions } from "@/features/assistantSuggestions/api/getDictionarySuggestions";
import { rejectDictionarySuggestion } from "@/features/assistantSuggestions/api/rejectDictionarySuggestion";
import type {
  AssistantDictionarySuggestion,
  DictionarySuggestionStatusFilter,
} from "@/features/assistantSuggestions/model/types";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { RequireTechnicalUser } from "@/features/auth/ui/RequireTechnicalUser";
import { AppShell } from "@/widgets/appShell/AppShell";

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

function formatDate(value?: string | null): string {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatConfidence(value: number): string {
  return `${Math.round(value * 100)}%`;
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

export default function CatalogAssistantSuggestionsPage() {
  return (
    <RequireAuth>
      <AppShell
        title="Предложения словаря"
        description="Модерация неизвестных слов, найденных assistant-ом."
      >
        <RequireTechnicalUser>
          <DictionarySuggestionsContent />
        </RequireTechnicalUser>
      </AppShell>
    </RequireAuth>
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
    queryKey: ["assistant-dictionary-suggestions", status, page, pageSize],
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
        queryKey: ["assistant-dictionary-suggestions"],
      });
    },
  });

  const rejectMutation = useMutation({
    mutationFn: rejectDictionarySuggestion,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["assistant-dictionary-suggestions"],
      });
    },
  });

  const suggestions = suggestionsQuery.data?.items ?? [];
  const totalCount = suggestionsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  function updateComment(suggestionId: string, value: string) {
    setReviewComments((current) => ({
      ...current,
      [suggestionId]: value,
    }));
  }

  function approveSuggestion(suggestion: AssistantDictionarySuggestion) {
    approveMutation.mutate({
      suggestionId: suggestion.id,
      request: {
        reviewComment: reviewComments[suggestion.id] || null,
      },
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

  function handleStatusChange(nextStatus: DictionarySuggestionStatusFilter) {
    setStatus(nextStatus);
    setPage(1);
  }

  const isReviewPending =
    approveMutation.isPending || rejectMutation.isPending;

  return (
    <div className="grid gap-6">
      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-center">
          <div>
            <h2 className="text-xl font-semibold text-white">
              Очередь модерации
            </h2>
            <p className="mt-1 text-sm text-slate-400">
              Здесь технический пользователь подтверждает или отклоняет
              неизвестные слова.
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
          <h2 className="text-xl font-semibold text-white">
            Предложений нет
          </h2>
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
              onApprove={() => approveSuggestion(suggestion)}
              onReject={() => rejectSuggestion(suggestion)}
              isReviewPending={isReviewPending}
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
  onApprove: () => void;
  onReject: () => void;
  isReviewPending: boolean;
}) {
  const canReview = suggestion.status === "Pending";

  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h3 className="text-xl font-semibold text-white">
              {suggestion.unknownPhrase}
            </h3>

            <span
              className={`rounded-full border px-3 py-1 text-xs font-medium ${getStatusBadgeClass(
                suggestion.status,
              )}`}
            >
              {suggestion.status}
            </span>
          </div>

          <p className="mt-2 text-sm text-slate-400">
            Создано: {formatDate(suggestion.createdAtUtc)}
          </p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 px-4 py-3 text-right">
          <p className="text-xs text-slate-400">Уверенность</p>
          <p className="mt-1 text-lg font-semibold text-teal-300">
            {formatConfidence(suggestion.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-5 grid gap-4 lg:grid-cols-2">
        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="text-sm font-medium text-slate-400">
            Исходный запрос пользователя
          </p>
          <p className="mt-2 text-slate-100">{suggestion.originalMessage}</p>
        </div>

        <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="text-sm font-medium text-slate-400">
            Предлагаемое сопоставление
          </p>

          <div className="mt-3 grid gap-2 text-sm">
            <InfoRow label="Тип" value={suggestion.suggestedKind} />
            <InfoRow
              label="Код характеристики"
              value={suggestion.suggestedTargetCode ?? "—"}
            />
            <InfoRow label="Значение" value={suggestion.suggestedTargetValue} />
          </div>
        </div>
      </div>

      {suggestion.reviewedAtUtc && (
        <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="text-sm font-medium text-slate-400">
            Результат проверки
          </p>
          <p className="mt-2 text-sm text-slate-300">
            Проверено: {formatDate(suggestion.reviewedAtUtc)}
          </p>
          {suggestion.reviewComment && (
            <p className="mt-2 text-slate-100">{suggestion.reviewComment}</p>
          )}
        </div>
      )}

      {canReview && (
        <div className="mt-5 grid gap-4">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Комментарий проверки
            </span>
            <textarea
              value={reviewComment}
              onChange={(event) => onReviewCommentChange(event.target.value)}
              placeholder="Например: Подтверждено, ЧЕНТ — это CHINT."
              className="min-h-24 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
            />
          </label>

          <div className="flex flex-wrap gap-3">
            <button
              type="button"
              disabled={isReviewPending}
              onClick={onApprove}
              className="rounded-xl bg-green-500 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              Одобрить
            </button>

            <button
              type="button"
              disabled={isReviewPending}
              onClick={onReject}
              className="rounded-xl bg-red-500 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              Отклонить
            </button>
          </div>
        </div>
      )}
    </article>
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