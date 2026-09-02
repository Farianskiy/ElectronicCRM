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
import {
  catalogImportQueryKeys,
  CatalogImportStatusBadge,
} from "@/features/catalogImports";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatFileSize } from "@/shared/lib/formatters";
import { AppSelect } from "@/shared/ui/AppSelect";
import { AppButton } from "@/shared/ui/AppButton";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

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
    <tr className="bg-[var(--app-panel)] align-top transition-colors hover:bg-[var(--app-panel-hover)] motion-reduce:transition-none">
      <td className="px-4 py-4">
        <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {item.originalFileName}
        </p>
        <p className="mt-1 text-xs text-[var(--app-muted)]">
          {formatFileSize(item.fileSizeBytes)}
        </p>
      </td>

      <td className="px-4 py-4">
        <CatalogImportStatusBadge status={item.status} />
      </td>

      <td className="px-4 py-4 tabular-nums text-[var(--app-text)]">
        <p>Всего: {item.rowsCount}</p>
        <p className="mt-1 text-xs text-[var(--app-success)]">
          Корректных: {item.validRowsCount}
        </p>
        <p className="mt-1 text-xs text-[var(--app-danger)]">
          С ошибками: {item.errorRowsCount}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {formatDate(item.lastActivityAtUtc)}
      </td>

      <td className="px-4 py-4 [overflow-wrap:anywhere]">
        {item.changesRequestComment ? (
          <p
            title={item.changesRequestComment}
            className="line-clamp-3 text-sm text-[var(--app-warning)]"
          >
            {item.changesRequestComment}
          </p>
        ) : item.rejectionReason ? (
          <p
            title={item.rejectionReason}
            className="line-clamp-3 text-sm text-[var(--app-danger)]"
          >
            {item.rejectionReason}
          </p>
        ) : (
          <span className="text-[var(--app-muted)]">—</span>
        )}
      </td>

      <td className="px-4 py-4">
        <div className="flex flex-wrap gap-2">
          <Link
            href={`/catalog/imports/${item.batchId}`}
            aria-label={`Открыть пакет ${item.originalFileName}`}
            className="inline-flex min-h-10 items-center justify-center rounded-xl border border-[var(--app-button-primary-border)] bg-[var(--app-button-primary-bg)] px-3 py-2 text-xs font-semibold text-[var(--app-button-primary-text)] transition-colors hover:border-[var(--app-button-primary-hover-border)] hover:bg-[var(--app-button-primary-hover-bg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none"
          >
            Открыть
          </Link>

          {item.canDelete && (
            <AppButton
              type="button"
              variant="danger"
              size="sm"
              disabled={isDeleting}
              loading={isDeleting}
              aria-label={`Удалить пакет ${item.originalFileName}`}
              onClick={() => onDelete(item)}
            >
              {isDeleting ? "Удаляем..." : "Удалить"}
            </AppButton>
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
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Импорт каталога"
      description="История загруженных Excel-файлов и состояние обработки каждого пакета."
      contentClassName="grid min-w-0 gap-6"
      actions={
        <Link
          href="/catalog/imports/new"
          className="inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-xl border border-[var(--app-button-primary-border)] bg-[var(--app-button-primary-bg)] px-4 py-2.5 text-sm font-semibold text-[var(--app-button-primary-text)] transition-colors hover:border-[var(--app-button-primary-hover-border)] hover:bg-[var(--app-button-primary-hover-bg)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none sm:w-auto"
        >
          <span aria-hidden="true">+</span>
          Загрузить Excel
        </Link>
      }
    >
      <section
        aria-label="Фильтры импорта"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="grid min-w-0 gap-4 md:grid-cols-[minmax(0,320px)_1fr] md:items-end">
          <div className="grid min-w-0 gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
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
          </div>

          <div className="flex min-w-0 items-center justify-start md:justify-end">
            <p className="text-sm text-[var(--app-muted)]">
              Найдено пакетов:{" "}
              <span className="font-semibold tabular-nums text-[var(--app-text)]">
                {totalCount}
              </span>
            </p>
          </div>
        </div>
      </section>

      {importsQuery.isError && (
        <section
          role="alert"
          className="min-w-0 rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-[var(--app-danger)]"
        >
          <h2 className="font-semibold">
            Не удалось загрузить список импортов
          </h2>
          <p className="mt-2 whitespace-pre-wrap text-sm [overflow-wrap:anywhere]">
            {getApiErrorMessage(
              importsQuery.error,
              "Не удалось загрузить список импортов.",
            )}
          </p>
          {items.length > 0 && (
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              Ниже показаны ранее загруженные данные. Они могут быть
              неактуальны.
            </p>
          )}
        </section>
      )}

      {deleteMutation.isError && (
        <section
          role="alert"
          className="min-w-0 rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getApiErrorMessage(
            deleteMutation.error,
            "Не удалось удалить пакет импорта.",
          )}
        </section>
      )}

      <section
        aria-labelledby="my-imports-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="flex flex-col justify-between gap-3 md:flex-row md:items-center">
          <div className="min-w-0">
            <h2
              id="my-imports-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Мои загрузки
            </h2>

            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Страница {page} из {totalPages}
            </p>
          </div>

          {importsQuery.isFetching && !importsQuery.isLoading && (
            <p role="status" className="text-sm text-[var(--app-accent)]">
              Обновляем список...
            </p>
          )}
        </div>

        {importsQuery.isLoading ? (
          <div
            role="status"
            className="mt-6 flex min-h-32 items-center justify-center gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-5 text-sm text-[var(--app-muted)]"
          >
            <span
              aria-hidden="true"
              className="h-5 w-5 shrink-0 animate-spin rounded-full border-2 border-[var(--app-accent-border)] border-t-[var(--app-accent)] motion-reduce:animate-none"
            />
            Загружаем пакеты импорта...
          </div>
        ) : items.length > 0 ? (
          <div
            role="region"
            aria-label="Таблица пакетов импорта"
            tabIndex={0}
            className="mt-6 min-w-0 max-w-full overflow-x-auto rounded-2xl border border-[var(--app-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
          >
            <table className="w-full min-w-[1120px] table-fixed border-collapse text-left text-sm">
              <caption className="sr-only">Загруженные пакеты импорта</caption>
              <colgroup>
                <col className="w-[28%]" />
                <col className="w-[18%]" />
                <col className="w-[12%]" />
                <col className="w-[16%]" />
                <col className="w-[14%]" />
                <col className="w-[12%]" />
              </colgroup>
              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Файл
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Статус
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Строки
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Последняя активность
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Комментарий
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Действия
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-[var(--app-border)]">
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
        ) : !importsQuery.isError ? (
          <div
            role="status"
            className="mt-6 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-6"
          >
            <h3 className="text-lg font-semibold text-[var(--app-text)]">
              {status ? "Пакеты не найдены" : "Загрузок пока нет"}
            </h3>
            <p className="mt-2 text-sm text-[var(--app-muted)]">
              {status
                ? "Нет пакетов с выбранным статусом. Попробуйте выбрать другой статус или «Все статусы»."
                : "Чтобы начать работу, нажмите «Загрузить Excel» вверху страницы."}
            </p>
          </div>
        ) : null}

        <nav
          aria-label="Страницы импортов"
          className="mt-5 grid grid-cols-2 gap-3 sm:flex sm:items-center sm:justify-between"
        >
          <AppButton
            type="button"
            variant="secondary"
            disabled={page <= 1 || importsQuery.isFetching}
            onClick={() =>
              setPage((currentPage) => Math.max(1, currentPage - 1))
            }
          >
            Назад
          </AppButton>

          <AppButton
            type="button"
            variant="secondary"
            disabled={
              page >= totalPages ||
              backendTotalPages === 0 ||
              importsQuery.isFetching
            }
            onClick={() =>
              setPage((currentPage) => Math.min(totalPages, currentPage + 1))
            }
          >
            Вперёд
          </AppButton>
        </nav>
      </section>
    </PageWorkspace>
  );
}
