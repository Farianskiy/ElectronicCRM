"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { getCatalogProductAuditHistory } from "../api/getCatalogProductAuditHistory";
import { catalogProductAuditHistoryQueryKey } from "../model/queryKeys";
import type {
  ProductAuditHistoryChange,
  ProductAuditHistoryItem,
} from "../model/types";

interface TechnicalProductAuditHistoryProps {
  productId: string;
}

const PAGE_SIZE = 10;

const operationLabels: Readonly<Record<string, string>> = {
  GeneralInformationUpdated: "Обновлена основная информация",

  PriceUpdated: "Изменена цена",

  StockUpdated: "Изменён остаток",

  CharacteristicSet: "Сохранена характеристика",

  CharacteristicRemoved: "Удалена характеристика",

  AliasAdded: "Добавлено альтернативное название",

  AliasRemoved: "Удалено альтернативное название",

  ProductTypeMigrated: "Изменён тип товара",

  ImportApplied: "Применены данные импорта",
};

const sourceLabels: Readonly<Record<string, string>> = {
  Manual: "Ручное изменение",
  ImportBatch: "Excel-импорт",
  System: "Системная операция",
};

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  dateStyle: "medium",
  timeStyle: "short",
});

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

  if (error instanceof Error) {
    return error.message;
  }

  return "Не удалось загрузить историю товара.";
}

function formatOperation(operation: string): string {
  return operationLabels[operation] ?? operation;
}

function formatSource(source: string): string {
  return sourceLabels[source] ?? source;
}

function formatDate(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return dateFormatter.format(date);
}

function formatUserId(userId?: string | null): string {
  if (!userId) {
    return "Система";
  }

  if (userId.length <= 16) {
    return userId;
  }

  return `${userId.slice(0, 8)}` + `…${userId.slice(-4)}`;
}

function formatChangesCount(count: number): string {
  const lastTwoDigits = count % 100;

  const lastDigit = count % 10;

  if (lastTwoDigits >= 11 && lastTwoDigits <= 14) {
    return `${count} изменений`;
  }

  if (lastDigit === 1) {
    return `${count} изменение`;
  }

  if (lastDigit >= 2 && lastDigit <= 4) {
    return `${count} изменения`;
  }

  return `${count} изменений`;
}

function formatDiffValue(value?: string | null): string {
  if (value === null || value === undefined || value.length === 0) {
    return "—";
  }

  return value;
}

function AuditChangeRow({ change }: { change: ProductAuditHistoryChange }) {
  return (
    <div className="grid gap-3 rounded-2xl border border-white/10 bg-black/20 p-4 lg:grid-cols-[minmax(180px,0.8fr)_1fr_40px_1fr] lg:items-center">
      <div>
        <p className="text-xs text-slate-500">Поле</p>

        <p className="mt-1 break-words text-sm font-medium text-slate-200">
          {change.label}
        </p>
      </div>

      <div className="min-w-0 rounded-xl border border-red-500/20 bg-red-500/[0.05] p-3">
        <p className="text-xs text-red-300/70">Было</p>

        <p className="mt-1 break-words text-sm text-red-100">
          {formatDiffValue(change.before)}
        </p>
      </div>

      <div className="hidden text-center text-slate-500 lg:block">→</div>

      <div className="min-w-0 rounded-xl border border-green-500/20 bg-green-500/[0.05] p-3">
        <p className="text-xs text-green-300/70">Стало</p>

        <p className="mt-1 break-words text-sm text-green-100">
          {formatDiffValue(change.after)}
        </p>
      </div>
    </div>
  );
}

function AuditHistoryItem({ item }: { item: ProductAuditHistoryItem }) {
  return (
    <details className="group rounded-2xl border border-white/10 bg-black/20">
      <summary className="cursor-pointer px-5 py-4 marker:text-slate-500">
        <div className="ml-2 inline-flex max-w-[calc(100%-24px)] flex-col gap-3 align-middle sm:w-[calc(100%-24px)] sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <p className="font-medium text-white">
              {formatOperation(item.operation)}
            </p>

            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-400">
              <span>{formatDate(item.changedAtUtc)}</span>

              <span>{formatSource(item.source)}</span>

              <span title={item.changedByUserId ?? "Системная операция"}>
                Пользователь: {formatUserId(item.changedByUserId)}
              </span>
            </div>
          </div>

          <span className="shrink-0 rounded-full border border-teal-500/20 bg-teal-500/10 px-3 py-1 text-xs text-teal-200">
            {formatChangesCount(item.changes.length)}
          </span>
        </div>
      </summary>

      <div className="border-t border-white/10 p-5">
        {item.changes.length === 0 ? (
          <p className="text-sm text-slate-500">
            Значимые различия между снимками не обнаружены.
          </p>
        ) : (
          <div className="grid gap-3">
            {item.changes.map((change) => (
              <AuditChangeRow key={change.field} change={change} />
            ))}
          </div>
        )}

        {item.sourceId && (
          <div className="mt-4 rounded-xl border border-white/10 bg-white/[0.02] px-4 py-3">
            <p className="text-xs text-slate-500">Идентификатор источника</p>

            <p className="mt-1 break-all font-mono text-xs text-slate-300">
              {item.sourceId}
            </p>
          </div>
        )}
      </div>
    </details>
  );
}

export function TechnicalProductAuditHistory({
  productId,
}: TechnicalProductAuditHistoryProps) {
  const [pageNumber, setPageNumber] = useState(1);

  const historyQuery = useQuery({
    queryKey: [
      ...catalogProductAuditHistoryQueryKey(productId),
      pageNumber,
      PAGE_SIZE,
    ],

    queryFn: () =>
      getCatalogProductAuditHistory(productId, pageNumber, PAGE_SIZE),

    enabled: productId.length > 0,

    placeholderData: (previousData) => previousData,
  });

  const page = historyQuery.data;

  const hasPreviousPage = pageNumber > 1;

  const hasNextPage = page !== undefined && pageNumber < page.totalPages;

  return (
    <section className="rounded-2xl border border-violet-500/20 bg-violet-500/[0.04] p-5">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h3 className="font-semibold text-white">История изменений</h3>

          <p className="mt-2 text-sm text-slate-400">
            Журнал ручных изменений товара. Нажмите на событие, чтобы увидеть
            значения до и после операции.
          </p>
        </div>

        <button
          type="button"
          disabled={historyQuery.isFetching}
          onClick={() => {
            void historyQuery.refetch();
          }}
          className="shrink-0 rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-50"
        >
          {historyQuery.isFetching ? "Обновляем..." : "Обновить"}
        </button>
      </div>

      {historyQuery.isLoading && (
        <p className="mt-5 text-sm text-slate-400">
          Загружаем историю товара...
        </p>
      )}

      {historyQuery.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(historyQuery.error)}
        </div>
      )}

      {page && (
        <>
          <div className="mt-5 flex flex-wrap items-center justify-between gap-3 text-sm text-slate-400">
            <span>
              Всего событий:{" "}
              <strong className="font-medium text-slate-200">
                {page.totalCount}
              </strong>
            </span>

            {page.totalPages > 0 && (
              <span>
                Страница {page.pageNumber} из {page.totalPages}
              </span>
            )}
          </div>

          {page.items.length === 0 ? (
            <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-500">
              История изменений этого товара пока пуста.
            </div>
          ) : (
            <div className="mt-5 grid gap-3">
              {page.items.map((item) => (
                <AuditHistoryItem key={item.id} item={item} />
              ))}
            </div>
          )}

          {page.totalPages > 1 && (
            <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <button
                type="button"
                disabled={!hasPreviousPage || historyQuery.isFetching}
                onClick={() =>
                  setPageNumber((currentPage) => Math.max(1, currentPage - 1))
                }
                className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-40"
              >
                Назад
              </button>

              <span className="text-center text-sm text-slate-400">
                {page.pageNumber}
                {" / "}
                {page.totalPages}
              </span>

              <button
                type="button"
                disabled={!hasNextPage || historyQuery.isFetching}
                onClick={() => setPageNumber((currentPage) => currentPage + 1)}
                className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.08] disabled:cursor-not-allowed disabled:opacity-40"
              >
                Далее
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
