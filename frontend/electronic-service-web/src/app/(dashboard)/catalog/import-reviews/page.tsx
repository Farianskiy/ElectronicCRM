"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { getCatalogImportReviewQueue } from "@/features/catalogImports/api/getCatalogImportReviewQueue";
import { startCatalogImportReview } from "@/features/catalogImports/api/startCatalogImportReview";
import { getCatalogImportStatusLabel } from "@/features/catalogImports/model/catalogImportStatus";
import {
  catalogImportReviewQueueStatuses,
  type CatalogImportReviewQueueItem,
  type CatalogImportReviewQueueStatus,
} from "@/features/catalogImports/model/types";
import {
  catalogImportQueryKeys,
  CatalogImportStatusBadge,
} from "@/features/catalogImports";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate } from "@/shared/lib/formatters";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";

const pageSize = 25;

export default function CatalogImportReviewsPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const session = useAuthSession();

  const [status, setStatus] = useState<CatalogImportReviewQueueStatus | null>(
    null,
  );

  const [page, setPage] = useState(1);

  const queueQuery = useQuery({
    queryKey: catalogImportQueryKeys.reviewQueue(status, page, pageSize),
    queryFn: () =>
      getCatalogImportReviewQueue({
        status,
        page,
        pageSize,
      }),
    placeholderData: (previousData) => previousData,
  });

  const startReviewMutation = useMutation({
    mutationFn: startCatalogImportReview,

    onSuccess: async (result) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.reviewQueueRoot,
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(result.batchId),
        }),
      ]);

      router.push(`/catalog/imports/${result.batchId}?from=review-queue`);
    },

    onError: async () => {
      /*
       * Другой Technical мог забрать пакет между
       * загрузкой страницы и нажатием кнопки.
       */
      await queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.reviewQueueRoot,
      });
    },
  });

  const items = queueQuery.data?.items ?? [];
  const totalCount = queueQuery.data?.totalCount ?? 0;
  const backendTotalPages = queueQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  function handleStatusChange(value: string): void {
    setStatus(
      value.length === 0 ? null : (value as CatalogImportReviewQueueStatus),
    );

    setPage(1);
    startReviewMutation.reset();
  }

  function handleStartReview(item: CatalogImportReviewQueueItem): void {
    const confirmed = window.confirm(
      [
        `Начать проверку пакета «${item.originalFileName}»?`,
        "",
        `Автор: ${item.createdByDisplayName}`,
        `Строк: ${item.rowsCount}`,
        "",
        "Пакет будет закреплён за вами.",
      ].join("\n"),
    );

    if (!confirmed) {
      return;
    }

    startReviewMutation.mutate(item.batchId);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Проверка импортов"
        description="Очередь пакетов каталога, отправленных пользователями на техническую проверку."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="grid gap-4 md:grid-cols-[minmax(0,320px)_1fr] md:items-end">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Состояние проверки
            </span>

            <AppSelect
              ariaLabel="Состояние очереди проверки"
              value={status ?? ""}
              onChange={handleStatusChange}
              options={[
                {
                  value: "",
                  label: "Вся очередь",
                },
                ...catalogImportReviewQueueStatuses.map((statusItem) => ({
                  value: statusItem,
                  label: getCatalogImportStatusLabel(statusItem),
                })),
              ]}
            />
          </label>

          <div className="flex items-center justify-start md:justify-end">
            <p className="text-sm text-slate-400">
              Пакетов в выборке:{" "}
              <span className="font-semibold text-white">{totalCount}</span>
            </p>
          </div>
        </div>
      </section>

      {queueQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {getApiErrorMessage(
            queueQuery.error,
            "Не удалось загрузить очередь проверки импортов.",
          )}
        </section>
      )}

      {startReviewMutation.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5">
          <h2 className="font-semibold text-red-100">
            Не удалось начать проверку
          </h2>

          <p className="mt-2 text-sm text-red-200">
            {getApiErrorMessage(
              startReviewMutation.error,
              "Возможно, пакет уже взял на проверку другой технический специалист.",
            )}
          </p>
        </section>
      )}

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="flex flex-col justify-between gap-3 md:flex-row md:items-center">
          <div>
            <h2 className="text-xl font-semibold text-white">Очередь</h2>

            <p className="mt-1 text-sm text-slate-400">
              Страница {page} из {totalPages}
            </p>
          </div>

          <button
            type="button"
            disabled={queueQuery.isFetching}
            onClick={() => {
              startReviewMutation.reset();
              void queueQuery.refetch();
            }}
            className="rounded-xl border border-white/10 bg-white/[0.05] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {queueQuery.isFetching ? "Обновляем..." : "Обновить очередь"}
          </button>
        </div>

        {queueQuery.isLoading ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5 text-slate-300">
            Загружаем очередь проверки...
          </div>
        ) : items.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-6">
            <h3 className="text-lg font-semibold text-white">Очередь пуста</h3>

            <p className="mt-2 text-sm text-slate-400">
              Для выбранного фильтра нет пакетов. Редкий и подозрительно
              приятный момент.
            </p>
          </div>
        ) : (
          <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10">
            <table className="w-full min-w-[1320px] border-collapse text-left text-sm">
              <thead className="bg-black/30 text-slate-400">
                <tr>
                  <th className="px-4 py-3 font-medium">Файл</th>

                  <th className="px-4 py-3 font-medium">Автор</th>

                  <th className="px-4 py-3 font-medium">Статус</th>

                  <th className="px-4 py-3 font-medium">Строки</th>

                  <th className="px-4 py-3 font-medium">Отправлен</th>

                  <th className="px-4 py-3 font-medium">Проверяющий</th>

                  <th className="px-4 py-3 font-medium">Действия</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-white/10">
                {items.map((item) => {
                  const assignedToCurrentUser =
                    Boolean(session?.userId) &&
                    item.reviewedByUserId === session?.userId;

                  const assignedToAnotherUser =
                    item.status === "UnderReview" &&
                    Boolean(item.reviewedByUserId) &&
                    !assignedToCurrentUser;

                  const isStarting =
                    startReviewMutation.isPending &&
                    startReviewMutation.variables === item.batchId;

                  return (
                    <tr
                      key={item.batchId}
                      className="bg-white/[0.01] align-top transition hover:bg-white/[0.04]"
                    >
                      <td className="max-w-80 px-4 py-4">
                        <p className="break-words font-medium text-white">
                          {item.originalFileName}
                        </p>

                        <p className="mt-1 break-all text-xs text-slate-600">
                          {item.batchId}
                        </p>
                      </td>

                      <td className="px-4 py-4">
                        <p className="font-medium text-slate-200">
                          {item.createdByDisplayName}
                        </p>

                        <p className="mt-1 text-xs text-slate-500">
                          {item.createdByEmail || "Email не указан"}
                        </p>

                        <p className="mt-1 text-xs text-slate-600">
                          {item.createdByUserType}
                        </p>
                      </td>

                      <td className="px-4 py-4">
                        <CatalogImportStatusBadge status={item.status} />
                      </td>

                      <td className="px-4 py-4">
                        <p className="text-slate-200">
                          Всего: {item.rowsCount}
                        </p>

                        <p className="mt-1 text-xs text-green-300">
                          Корректных: {item.validRowsCount}
                        </p>

                        <p className="mt-1 text-xs text-red-300">
                          С ошибками: {item.errorRowsCount}
                        </p>
                      </td>

                      <td className="whitespace-nowrap px-4 py-4 text-slate-300">
                        {formatDate(item.submittedAtUtc)}
                      </td>

                      <td className="px-4 py-4">
                        {item.status === "Submitted" ? (
                          <span className="text-slate-500">Не назначен</span>
                        ) : assignedToCurrentUser ? (
                          <div>
                            <span className="rounded-full border border-teal-500/30 bg-teal-500/10 px-3 py-1 text-xs font-medium text-teal-200">
                              Моя проверка
                            </span>

                            <p className="mt-2 text-xs text-slate-500">
                              {formatDate(item.reviewedAtUtc)}
                            </p>
                          </div>
                        ) : assignedToAnotherUser ? (
                          <div>
                            <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 text-xs font-medium text-amber-200">
                              Другой Technical
                            </span>

                            <p className="mt-2 text-xs text-slate-500">
                              {formatDate(item.reviewedAtUtc)}
                            </p>
                          </div>
                        ) : (
                          <span className="text-slate-500">Не определён</span>
                        )}
                      </td>

                      <td className="px-4 py-4">
                        <div className="flex flex-wrap gap-2">
                          {item.status === "Submitted" ? (
                            <>
                              <Link
                                href={`/catalog/imports/${item.batchId}?from=review-queue`}
                                className="rounded-xl bg-white/[0.06] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-white/[0.1]"
                              >
                                Открыть
                              </Link>

                              <button
                                type="button"
                                disabled={startReviewMutation.isPending}
                                onClick={() => handleStartReview(item)}
                                className="rounded-xl bg-teal-500 px-3 py-2 text-xs font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
                              >
                                {isStarting
                                  ? "Назначаем..."
                                  : "Начать проверку"}
                              </button>
                            </>
                          ) : (
                            <Link
                              href={`/catalog/imports/${item.batchId}?from=review-queue`}
                              className={[
                                "rounded-xl px-3 py-2",
                                "text-xs font-medium transition",
                                assignedToCurrentUser
                                  ? "bg-teal-500 text-white hover:bg-teal-400"
                                  : "bg-white/[0.06] text-slate-200 hover:bg-white/[0.1]",
                              ].join(" ")}
                            >
                              {assignedToCurrentUser
                                ? "Продолжить проверку"
                                : "Просмотреть"}
                            </Link>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        <div className="mt-5 flex items-center justify-between">
          <button
            type="button"
            disabled={page <= 1 || queueQuery.isFetching}
            onClick={() => {
              startReviewMutation.reset();

              setPage((currentPage) => Math.max(1, currentPage - 1));
            }}
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Назад
          </button>

          <button
            type="button"
            disabled={
              page >= totalPages ||
              backendTotalPages === 0 ||
              queueQuery.isFetching
            }
            onClick={() => {
              startReviewMutation.reset();

              setPage((currentPage) => Math.min(totalPages, currentPage + 1));
            }}
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Вперёд
          </button>
        </div>
      </section>
    </div>
  );
}
