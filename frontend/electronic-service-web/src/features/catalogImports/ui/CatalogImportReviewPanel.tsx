"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate } from "@/shared/lib/formatters";
import { startCatalogImportReview } from "../api/startCatalogImportReview";
import type { CatalogImportBatchStatus } from "../model/types";

interface CatalogImportReviewPanelProps {
  batchId: string;
  originalFileName: string;
  status: CatalogImportBatchStatus;
  reviewedByUserId?: string | null;
  reviewedAtUtc?: string | null;
}

export function CatalogImportReviewPanel({
  batchId,
  originalFileName,
  status,
  reviewedByUserId,
  reviewedAtUtc,
}: CatalogImportReviewPanelProps) {
  const session = useAuthSession();
  const queryClient = useQueryClient();

  const startMutation = useMutation({
    mutationFn: () => startCatalogImportReview(batchId),

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "details", batchId],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "review-queue"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "history", batchId],
        }),
      ]);
    },

    onError: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["catalog-import-batches", "details", batchId],
      });
    },
  });

  const isTechnical = session?.userType === "Technical";

  const assignedToCurrentUser =
    Boolean(session?.userId) && reviewedByUserId === session?.userId;

  const assignedToAnotherUser =
    status === "UnderReview" &&
    Boolean(reviewedByUserId) &&
    !assignedToCurrentUser;

  if (!isTechnical || (status !== "Submitted" && status !== "UnderReview")) {
    return null;
  }

  function handleStartReview(): void {
    const confirmed = window.confirm(
      [
        `Начать проверку пакета «${originalFileName}»?`,
        "",
        "Пакет будет закреплён за вашим пользователем Technical.",
      ].join("\n"),
    );

    if (confirmed) {
      startMutation.mutate();
    }
  }

  if (status === "Submitted") {
    return (
      <section className="rounded-3xl border border-teal-500/30 bg-teal-500/[0.07] p-6">
        <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-center">
          <div>
            <h2 className="text-xl font-semibold text-teal-100">
              Пакет ожидает проверки
            </h2>

            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-300">
              Изучите строки и исходный файл. После начала проверки пакет будет
              закреплён за вами и перейдёт в статус UnderReview.
            </p>
          </div>

          <button
            type="button"
            disabled={startMutation.isPending}
            onClick={handleStartReview}
            className="shrink-0 rounded-2xl bg-teal-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {startMutation.isPending
              ? "Начинаем проверку..."
              : "Начать проверку"}
          </button>
        </div>

        {startMutation.isError && (
          <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {getApiErrorMessage(
              startMutation.error,
              "Не удалось начать проверку. Возможно, пакет уже забрал другой специалист.",
            )}
          </div>
        )}
      </section>
    );
  }

  if (assignedToCurrentUser) {
    return (
      <section className="rounded-3xl border border-blue-500/30 bg-blue-500/[0.07] p-6">
        <h2 className="text-xl font-semibold text-blue-100">
          Вы проверяете этот пакет
        </h2>

        <p className="mt-2 text-sm leading-6 text-slate-300">
          Пакет закреплён за вашим пользователем. После изучения данных можно
          будет вернуть его на исправление, отклонить или применить.
        </p>

        <p className="mt-3 text-sm text-blue-200">
          Проверка начата: {formatDate(reviewedAtUtc)}
        </p>
      </section>
    );
  }

  if (assignedToAnotherUser) {
    return (
      <section className="rounded-3xl border border-amber-500/30 bg-amber-500/[0.07] p-6">
        <h2 className="text-xl font-semibold text-amber-100">
          Пакет проверяет другой специалист
        </h2>

        <p className="mt-2 text-sm leading-6 text-slate-300">
          Вы можете просмотреть данные, но решения по пакету доступны только
          назначенному Technical.
        </p>

        <p className="mt-3 text-sm text-amber-200">
          Проверка начата: {formatDate(reviewedAtUtc)}
        </p>
      </section>
    );
  }

  return null;
}
