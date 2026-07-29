"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { deleteCatalogImportBatch } from "@/features/catalogImports/api/deleteCatalogImportBatch";
import { getMyCatalogImportBatches } from "@/features/catalogImports/api/getMyCatalogImportBatches";
import {
  catalogImportBatchStatuses,
  type CatalogImportBatchStatus,
  type MyCatalogImportBatchItem,
} from "@/features/catalogImports/model/types";
import { getCatalogImportStatusLabel } from "@/features/catalogImports/model/catalogImportStatus";
import { CatalogImportStatusBadge } from "@/features/catalogImports/ui/CatalogImportStatusBadge";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatFileSize } from "@/shared/lib/formatters";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";
import { catalogImportQueryKeys } from "@/features/catalogImports/model/queryKeys";

const pageSize = 20;

function CatalogImportRow({
  item,
  isDeleting,
  onDelete,
}: {
  item: MyCatalogImportBatchItem;
  isDeleting: boolean;
  onDelete: (item: MyCatalogImportBatchItem) => void;
}) {
  return (
    <tr className="bg-white/[0.02] transition hover:bg-white/[0.05]">
      <td className="px-4 py-4">
        <p className="font-medium text-white">{item.originalFileName}</p>
        <p className="mt-1 text-xs text-slate-500">
          {formatFileSize(item.fileSizeBytes)}
        </p>
      </td>

      <td className="px-4 py-4">
        <CatalogImportStatusBadge status={item.status} />
      </td>

      <td className="px-4 py-4 text-slate-300">
        <p>Всего: {item.rowsCount}</p>
        <p className="mt-1 text-xs text-green-300">
          Корректных: {item.validRowsCount}
        </p>
        <p className="mt-1 text-xs text-red-300">
          С ошибками: {item.errorRowsCount}
        </p>
      </td>

      <td className="px-4 py-4 text-slate-300">
        {formatDate(item.lastActivityAtUtc)}
      </td>

      <td className="max-w-80 px-4 py-4 text-slate-300">
        {item.changesRequestComment ? (
          <p className="line-clamp-3 text-sm text-orange-200">
            {item.changesRequestComment}
          </p>
        ) : item.rejectionReason ? (
          <p className="line-clamp-3 text-sm text-rose-200">
            {item.rejectionReason}
          </p>
        ) : (
          <span className="text-slate-600">—</span>
        )}
      </td>

      <td className="px-4 py-4">
        <div className="flex flex-wrap gap-2">
          <Link
            href={`/catalog/imports/${item.batchId}`}
            className="rounded-xl bg-white/[0.06] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-teal-500 hover:text-white"
          >
            Открыть
          </Link>

          {item.canDelete && (
            <button
              type="button"
              disabled={isDeleting}
              onClick={() => onDelete(item)}
              className="rounded-xl border border-red-500/30 bg-red-500/10 px-3 py-2 text-xs font-medium text-red-200 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isDeleting ? "Удаляем..." : "Удалить"}
            </button>
          )}
        </div>
      </td>
    </tr>
  );
}

export default function CatalogImportsPage() {
  const queryClient = useQueryClient();

  const [status, setStatus] = useState<CatalogImportBatchStatus | null>(null);
  const [page, setPage] = useState(1);

  const importsQuery = useQuery({
    queryKey: catalogImportQueryKeys.my(status, page, pageSize),
    queryFn: () =>
      getMyCatalogImportBatches({
        status,
        page,
        pageSize,
      }),
    placeholderData: (previousData) => previousData,
  });

  const deleteMutation = useMutation({
    mutationFn: deleteCatalogImportBatch,
    onSuccess: async () => {
      const itemsCount = importsQuery.data?.items.length ?? 0;

      if (itemsCount === 1 && page > 1) {
        setPage((currentPage) => Math.max(1, currentPage - 1));
      }

      await queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.myRoot,
      });
    },
  });

  const items = importsQuery.data?.items ?? [];
  const totalCount = importsQuery.data?.totalCount ?? 0;
  const backendTotalPages = importsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  function handleStatusChange(value: string): void {
    setStatus(value.length === 0 ? null : (value as CatalogImportBatchStatus));

    setPage(1);
  }

  function handleDelete(item: MyCatalogImportBatchItem): void {
    const confirmed = window.confirm(
      `Удалить пакет «${item.originalFileName}»?\n\nЭто действие нельзя отменить.`,
    );

    if (!confirmed) {
      return;
    }

    deleteMutation.mutate(item.batchId);
  }

  return (
    <div className="grid gap-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <PageHeader
          title="Импорт каталога"
          description="История загруженных Excel-файлов и состояние обработки каждого пакета."
        />

        <Link
          href="/catalog/imports/new"
          className="inline-flex shrink-0 items-center justify-center rounded-2xl bg-teal-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-400"
        >
          Загрузить Excel
        </Link>
      </div>

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="grid gap-4 md:grid-cols-[minmax(0,320px)_1fr] md:items-end">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Статус пакета
            </span>

            <AppSelect
              ariaLabel="Статус пакета импорта"
              value={status ?? ""}
              onChange={handleStatusChange}
              options={[
                {
                  value: "",
                  label: "Все статусы",
                },
                ...catalogImportBatchStatuses.map((statusItem) => ({
                  value: statusItem,
                  label: getCatalogImportStatusLabel(statusItem),
                })),
              ]}
            />
          </label>

          <div className="flex items-center justify-start md:justify-end">
            <p className="text-sm text-slate-400">
              Найдено пакетов:{" "}
              <span className="font-semibold text-white">{totalCount}</span>
            </p>
          </div>
        </div>
      </section>

      {importsQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {getApiErrorMessage(
            importsQuery.error,
            "Не удалось загрузить список импортов.",
          )}
        </section>
      )}

      {deleteMutation.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {getApiErrorMessage(
            deleteMutation.error,
            "Не удалось удалить пакет импорта.",
          )}
        </section>
      )}

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="flex flex-col justify-between gap-3 md:flex-row md:items-center">
          <div>
            <h2 className="text-xl font-semibold text-white">Мои загрузки</h2>

            <p className="mt-1 text-sm text-slate-400">
              Страница {page} из {totalPages}
            </p>
          </div>

          {importsQuery.isFetching && !importsQuery.isLoading && (
            <p className="text-sm text-teal-300">Обновляем список...</p>
          )}
        </div>

        {importsQuery.isLoading ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5 text-slate-300">
            Загружаем пакеты импорта...
          </div>
        ) : items.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-6">
            <h3 className="text-lg font-semibold text-white">
              Пакеты не найдены
            </h3>

            <p className="mt-2 text-sm text-slate-400">
              Для выбранного статуса у пользователя пока нет импортов.
            </p>
          </div>
        ) : (
          <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10">
            <table className="w-full min-w-[1120px] border-collapse text-left text-sm">
              <thead className="bg-black/30 text-slate-400">
                <tr>
                  <th className="px-4 py-3 font-medium">Файл</th>
                  <th className="px-4 py-3 font-medium">Статус</th>
                  <th className="px-4 py-3 font-medium">Строки</th>
                  <th className="px-4 py-3 font-medium">
                    Последняя активность
                  </th>
                  <th className="px-4 py-3 font-medium">Комментарий</th>
                  <th className="px-4 py-3 font-medium">Действия</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-white/10">
                {items.map((item) => (
                  <CatalogImportRow
                    key={item.batchId}
                    item={item}
                    isDeleting={
                      deleteMutation.isPending &&
                      deleteMutation.variables === item.batchId
                    }
                    onDelete={handleDelete}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="mt-5 flex items-center justify-between">
          <button
            type="button"
            disabled={page <= 1 || importsQuery.isFetching}
            onClick={() =>
              setPage((currentPage) => Math.max(1, currentPage - 1))
            }
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Назад
          </button>

          <button
            type="button"
            disabled={
              page >= totalPages ||
              backendTotalPages === 0 ||
              importsQuery.isFetching
            }
            onClick={() =>
              setPage((currentPage) => Math.min(totalPages, currentPage + 1))
            }
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Вперёд
          </button>
        </div>
      </section>
    </div>
  );
}
