"use client";

import { useQuery } from "@tanstack/react-query";
import { Fragment, useState } from "react";
import { getCatalogImportRows } from "../../api/getCatalogImportRows";
import {
  catalogImportRowFilterStatuses,
  type CatalogImportRow,
  type CatalogImportRowFilterStatus,
  type CatalogImportRowIssue,
} from "../../model/types";
import { getCatalogImportRowFilterStatusLabel } from "../../model/catalogImportRowStatus";
import { CatalogImportRowStatusBadge } from ".././CatalogImportRowStatusBadge";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { CatalogImportRowEditor } from "./CatalogImportRowEditor";
import { catalogImportQueryKeys } from "../../model/queryKeys";

interface CatalogImportRowsPreviewProps {
  batchId: string;
  productTypeId?: string | null;
  canEditRows: boolean;
}

const pageSize = 25;

const priceFormatter = new Intl.NumberFormat("ru-RU", {
  style: "currency",
  currency: "RUB",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatNullableText(value: string | null | undefined): string {
  const normalizedValue = value?.trim();

  return normalizedValue ? normalizedValue : "—";
}

function formatNullablePrice(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return priceFormatter.format(value);
}

function formatNullableInteger(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return value.toLocaleString("ru-RU");
}

function getRawDataEntries(row: CatalogImportRow): Array<[string, string]> {
  return Object.entries(row.rawData).sort(
    ([leftColumn], [rightColumn]) => Number(leftColumn) - Number(rightColumn),
  );
}

export function CatalogImportRowsPreview({
  batchId,
  productTypeId,
  canEditRows,
}: CatalogImportRowsPreviewProps) {
  const [status, setStatus] = useState<CatalogImportRowFilterStatus | null>(
    null,
  );

  const [page, setPage] = useState(1);

  const [expandedRowId, setExpandedRowId] = useState<string | null>(null);

  const [editingRowId, setEditingRowId] = useState<string | null>(null);

  const rowsQuery = useQuery({
    queryKey: catalogImportQueryKeys.rows(batchId, status, page, pageSize),
    queryFn: () =>
      getCatalogImportRows({
        batchId,
        status,
        page,
        pageSize,
      }),
    enabled: batchId.length > 0,
    placeholderData: (previousData) => previousData,
  });

  const items = rowsQuery.data?.items ?? [];
  const totalCount = rowsQuery.data?.totalCount ?? 0;
  const backendTotalPages = rowsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  function handleStatusChange(value: string): void {
    setStatus(
      value.length === 0 ? null : (value as CatalogImportRowFilterStatus),
    );

    setExpandedRowId(null);
    setEditingRowId(null);
    setPage(1);
  }

  function toggleRow(rowId: string): void {
    setEditingRowId(null);

    setExpandedRowId((currentRowId) => (currentRowId === rowId ? null : rowId));
  }

  function editRow(rowId: string): void {
    setExpandedRowId(null);

    setEditingRowId((currentRowId) => (currentRowId === rowId ? null : rowId));
  }

  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-end">
        <div>
          <h2 className="text-xl font-semibold text-white">Строки Excel</h2>

          <p className="mt-2 text-sm text-slate-400">
            Исходные значения, результат нормализации и найденные проблемы.
          </p>
        </div>

        <div className="w-full lg:w-72">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Статус строки
            </span>

            <AppSelect
              ariaLabel="Статус строки импорта"
              value={status ?? ""}
              onChange={handleStatusChange}
              options={[
                {
                  value: "",
                  label: "Все строки",
                },
                ...catalogImportRowFilterStatuses.map((statusItem) => ({
                  value: statusItem,
                  label: getCatalogImportRowFilterStatusLabel(statusItem),
                })),
              ]}
            />
          </label>
        </div>
      </div>

      <div className="mt-5 flex flex-col justify-between gap-2 text-sm text-slate-400 sm:flex-row sm:items-center">
        <p>
          Найдено строк:{" "}
          <span className="font-semibold text-white">{totalCount}</span>
        </p>

        <div className="flex items-center gap-3">
          <span>
            Страница {page} из {totalPages}
          </span>

          {rowsQuery.isFetching && !rowsQuery.isLoading && (
            <span className="text-teal-300">Обновляем...</span>
          )}
        </div>
      </div>

      {rowsQuery.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          {getApiErrorMessage(
            rowsQuery.error,
            "Не удалось загрузить строки пакета импорта.",
          )}
        </div>
      )}

      {rowsQuery.isLoading ? (
        <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
          Загружаем строки Excel...
        </div>
      ) : items.length === 0 ? (
        <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-6">
          <h3 className="font-semibold text-white">Строки не найдены</h3>

          <p className="mt-2 text-sm text-slate-400">
            Для выбранного фильтра в пакете нет строк.
          </p>
        </div>
      ) : (
        <div className="mt-5 overflow-x-auto rounded-2xl border border-white/10">
          <table className="w-full min-w-[1280px] border-collapse text-left text-sm">
            <thead className="bg-black/30 text-slate-400">
              <tr>
                <th className="px-4 py-3 font-medium">Строка</th>

                <th className="px-4 py-3 font-medium">Статус</th>

                <th className="px-4 py-3 font-medium">Наименование</th>

                <th className="px-4 py-3 font-medium">Артикул</th>

                <th className="px-4 py-3 font-medium">Производитель</th>

                <th className="px-4 py-3 font-medium">Цена</th>

                <th className="px-4 py-3 font-medium">Остаток</th>

                <th className="px-4 py-3 font-medium">Проблемы</th>

                <th className="px-4 py-3 font-medium">Действия</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-white/10">
              {items.map((row) => {
                const isExpanded = expandedRowId === row.rowId;

                const isEditing = editingRowId === row.rowId;

                const rowCanBeEdited = canEditRows && Boolean(productTypeId);

                return (
                  <Fragment key={row.rowId}>
                    <tr className="bg-white/[0.01] align-top transition hover:bg-white/[0.04]">
                      <td className="px-4 py-4 font-medium text-white">
                        {row.rowNumber}
                      </td>

                      <td className="px-4 py-4">
                        <CatalogImportRowStatusBadge status={row.status} />
                      </td>

                      <td className="max-w-72 px-4 py-4 text-slate-200">
                        <p className="line-clamp-3">
                          {formatNullableText(row.data.name)}
                        </p>
                      </td>

                      <td className="px-4 py-4 text-slate-300">
                        {formatNullableText(row.data.article)}
                      </td>

                      <td className="max-w-56 px-4 py-4 text-slate-300">
                        <p className="line-clamp-2">
                          {formatNullableText(row.data.manufacturer)}
                        </p>
                      </td>

                      <td className="whitespace-nowrap px-4 py-4 text-slate-300">
                        {formatNullablePrice(row.data.price)}
                      </td>

                      <td className="px-4 py-4 text-slate-300">
                        {formatNullableInteger(row.data.stockQuantity)}
                      </td>

                      <td className="px-4 py-4">
                        <div className="flex flex-wrap gap-2">
                          {row.issues.length > 0 && (
                            <span className="rounded-full border border-red-500/30 bg-red-500/10 px-2.5 py-1 text-xs font-medium text-red-200">
                              Ошибок: {row.issues.length}
                            </span>
                          )}

                          {row.warnings.length > 0 && (
                            <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-2.5 py-1 text-xs font-medium text-amber-200">
                              Предупреждений: {row.warnings.length}
                            </span>
                          )}

                          {row.issues.length === 0 &&
                            row.warnings.length === 0 && (
                              <span className="text-slate-600">—</span>
                            )}
                        </div>
                      </td>

                      <td className="px-4 py-4">
                        <div className="flex flex-wrap gap-2">
                          <button
                            type="button"
                            onClick={() => toggleRow(row.rowId)}
                            className="rounded-xl bg-white/[0.06] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-teal-500 hover:text-white"
                          >
                            {isExpanded ? "Скрыть" : "Подробнее"}
                          </button>

                          {rowCanBeEdited && (
                            <button
                              type="button"
                              onClick={() => editRow(row.rowId)}
                              className={[
                                "rounded-xl border px-3 py-2",
                                "text-xs font-medium transition",
                                isEditing
                                  ? "border-teal-400/40 bg-teal-500/20 text-teal-100"
                                  : "border-teal-500/30 bg-teal-500/10 text-teal-200 hover:bg-teal-500/20",
                              ].join(" ")}
                            >
                              {isEditing ? "Закрыть редактор" : "Редактировать"}
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>

                    {(isExpanded || isEditing) && (
                      <tr className="bg-black/20">
                        <td colSpan={9} className="px-5 py-5">
                          {isEditing && productTypeId ? (
                            <CatalogImportRowEditor
                              key={`${row.rowId}-${row.status}`}
                              batchId={batchId}
                              productTypeId={productTypeId}
                              row={row}
                              onCancel={() => {
                                setEditingRowId(null);
                              }}
                              onSaved={() => {
                                setEditingRowId(null);
                              }}
                            />
                          ) : (
                            <CatalogImportRowDetails row={row} />
                          )}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <div className="mt-5 flex items-center justify-between">
        <button
          type="button"
          disabled={page <= 1 || rowsQuery.isFetching}
          onClick={() => {
            setExpandedRowId(null);
            setEditingRowId(null);

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
            rowsQuery.isFetching
          }
          onClick={() => {
            setExpandedRowId(null);
            setEditingRowId(null);

            setPage((currentPage) => Math.min(totalPages, currentPage + 1));
          }}
          className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
        >
          Вперёд
        </button>
      </div>
    </section>
  );
}

function CatalogImportRowDetails({ row }: { row: CatalogImportRow }) {
  const rawDataEntries = getRawDataEntries(row);

  const characteristicEntries = Object.entries(row.data.characteristics).sort(
    ([leftKey], [rightKey]) => leftKey.localeCompare(rightKey),
  );

  return (
    <div className="grid gap-5">
      {(row.issues.length > 0 || row.warnings.length > 0) && (
        <div className="grid gap-4 xl:grid-cols-2">
          <IssueCollection title="Ошибки" items={row.issues} type="error" />

          <IssueCollection
            title="Предупреждения"
            items={row.warnings}
            type="warning"
          />
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        <div className="rounded-2xl border border-white/10 bg-white/[0.025] p-4">
          <h3 className="font-semibold text-white">Исходные значения Excel</h3>

          {rawDataEntries.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">
              Исходные значения отсутствуют.
            </p>
          ) : (
            <div className="mt-4 grid gap-2">
              {rawDataEntries.map(([columnNumber, value]) => (
                <div
                  key={columnNumber}
                  className="grid gap-1 rounded-xl border border-white/10 bg-black/20 p-3 sm:grid-cols-[120px_1fr]"
                >
                  <p className="text-xs font-medium text-slate-500">
                    Колонка {columnNumber}
                  </p>

                  <p className="break-words text-sm text-slate-200">
                    {value || "—"}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-2xl border border-white/10 bg-white/[0.025] p-4">
          <h3 className="font-semibold text-white">Характеристики</h3>

          {characteristicEntries.length === 0 ? (
            <p className="mt-3 text-sm text-slate-500">
              Характеристики пока не определены.
            </p>
          ) : (
            <div className="mt-4 grid gap-2">
              {characteristicEntries.map(([characteristicId, value]) => (
                <div
                  key={characteristicId}
                  className="rounded-xl border border-white/10 bg-black/20 p-3"
                >
                  <p className="break-all text-xs text-slate-500">
                    {characteristicId}
                  </p>

                  <p className="mt-1 break-words text-sm text-slate-200">
                    {value || "—"}
                  </p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function IssueCollection({
  title,
  items,
  type,
}: {
  title: string;
  items: CatalogImportRowIssue[];
  type: "error" | "warning";
}) {
  const isError = type === "error";

  return (
    <div
      className={[
        "rounded-2xl border p-4",
        isError
          ? "border-red-500/30 bg-red-500/[0.06]"
          : "border-amber-500/30 bg-amber-500/[0.06]",
      ].join(" ")}
    >
      <h3
        className={
          isError
            ? "font-semibold text-red-100"
            : "font-semibold text-amber-100"
        }
      >
        {title}
      </h3>

      {items.length === 0 ? (
        <p className="mt-3 text-sm text-slate-500">Нет.</p>
      ) : (
        <div className="mt-4 grid gap-3">
          {items.map((item, index) => (
            <div
              key={[
                item.code,
                item.field ?? "",
                item.sourceColumnNumber ?? "",
                index,
              ].join("-")}
              className="rounded-xl border border-white/10 bg-black/20 p-3"
            >
              <p
                className={
                  isError ? "text-sm text-red-200" : "text-sm text-amber-200"
                }
              >
                {item.message}
              </p>

              <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500">
                <span>Код: {item.code}</span>

                {item.field && <span>Поле: {item.field}</span>}

                {item.sourceColumnNumber !== null &&
                  item.sourceColumnNumber !== undefined && (
                    <span>Колонка: {item.sourceColumnNumber}</span>
                  )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
