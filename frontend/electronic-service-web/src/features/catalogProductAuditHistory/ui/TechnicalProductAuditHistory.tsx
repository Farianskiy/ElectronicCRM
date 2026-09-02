"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { getCatalogProductAuditHistory } from "../api/getCatalogProductAuditHistory";
import { catalogProductAuditHistoryQueryKey } from "../model/queryKeys";
import { AppButton } from "@/shared/ui/AppButton";
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
    <div className="grid min-w-0 gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1fr)_24px_minmax(0,1fr)] lg:items-center">
      <div className="min-w-0">
        <p className="text-xs text-[var(--app-muted)]">Поле</p>

        <p className="mt-1 text-sm font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {change.label}
        </p>
      </div>

      <div className="min-w-0 rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3">
        <p className="text-xs font-medium text-[var(--app-danger)]">Было</p>

        <p className="mt-1 whitespace-pre-wrap text-sm text-[var(--app-text)] [overflow-wrap:anywhere]">
          {formatDiffValue(change.before)}
        </p>
      </div>

      <div
        aria-hidden="true"
        className="hidden text-center text-[var(--app-muted)] lg:block"
      >
        →
      </div>

      <div className="min-w-0 rounded-xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-3">
        <p className="text-xs font-medium text-[var(--app-success)]">Стало</p>

        <p className="mt-1 whitespace-pre-wrap text-sm text-[var(--app-text)] [overflow-wrap:anywhere]">
          {formatDiffValue(change.after)}
        </p>
      </div>
    </div>
  );
}

function AuditHistoryItem({ item }: { item: ProductAuditHistoryItem }) {
  return (
    <details className="group min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)]">
      <summary className="flex cursor-pointer list-none items-start gap-3 rounded-2xl px-4 py-4 transition-colors hover:bg-[var(--app-panel-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none sm:px-5 [&::-webkit-details-marker]:hidden">
        <div className="flex min-w-0 flex-1 flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
              {formatOperation(item.operation)}
            </p>

            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-[var(--app-muted)]">
              <span>{formatDate(item.changedAtUtc)}</span>

              <span className="[overflow-wrap:anywhere]">
                {formatSource(item.source)}
              </span>

              <span
                title={item.changedByUserId ?? "Системная операция"}
                className="[overflow-wrap:anywhere]"
              >
                Пользователь: {formatUserId(item.changedByUserId)}
              </span>
            </div>
          </div>

          <span className="w-fit shrink-0 rounded-full border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-3 py-1 text-xs text-[var(--app-accent)]">
            {formatChangesCount(item.changes.length)}
          </span>
        </div>

        <svg
          aria-hidden="true"
          focusable="false"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="mt-0.5 h-5 w-5 shrink-0 text-[var(--app-muted)] transition-transform group-open:rotate-180 motion-reduce:transition-none"
        >
          <path d="m6 9 6 6 6-6" />
        </svg>
      </summary>

      <div className="min-w-0 border-t border-[var(--app-border)] p-4 sm:p-5">
        {item.changes.length === 0 ? (
          <p className="text-sm text-[var(--app-muted)]">
            Значимые различия между снимками не обнаружены.
          </p>
        ) : (
          <div className="grid min-w-0 gap-3">
            {item.changes.map((change) => (
              <AuditChangeRow key={change.field} change={change} />
            ))}
          </div>
        )}

        {item.sourceId && (
          <div className="mt-4 min-w-0 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3">
            <p className="text-xs text-[var(--app-muted)]">
              Идентификатор источника
            </p>

            <p className="mt-1 break-all font-mono text-xs text-[var(--app-text)]">
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
    <section className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
      <div className="flex min-w-0 flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <h3 className="font-semibold text-[var(--app-text)]">
            История изменений
          </h3>

          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Журнал изменений товара. Нажмите на событие, чтобы увидеть значения
            до и после операции.
          </p>
        </div>

        <AppButton
          type="button"
          variant="secondary"
          disabled={historyQuery.isFetching}
          loading={historyQuery.isFetching}
          onClick={() => {
            void historyQuery.refetch();
          }}
          className="w-full sm:w-auto"
        >
          {historyQuery.isFetching ? "Обновляем..." : "Обновить"}
        </AppButton>
      </div>

      {historyQuery.isLoading && (
        <p role="status" className="mt-5 text-sm text-[var(--app-muted)]">
          Загружаем историю товара...
        </p>
      )}

      {historyQuery.isError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(historyQuery.error)}
        </div>
      )}

      {page && (
        <>
          <div className="mt-5 flex flex-wrap items-center justify-between gap-3 text-sm text-[var(--app-muted)]">
            <span>
              Всего событий:{" "}
              <strong className="font-medium text-[var(--app-text)]">
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
            <div className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-5 text-sm text-[var(--app-muted)]">
              История изменений этого товара пока пуста.
            </div>
          ) : (
            <div className="mt-5 grid min-w-0 gap-3">
              {page.items.map((item) => (
                <AuditHistoryItem key={item.id} item={item} />
              ))}
            </div>
          )}

          {page.totalPages > 1 && (
            <div className="mt-5 flex flex-col gap-3 border-t border-[var(--app-border)] pt-5 sm:flex-row sm:items-center sm:justify-between">
              <AppButton
                type="button"
                variant="secondary"
                disabled={!hasPreviousPage || historyQuery.isFetching}
                onClick={() =>
                  setPageNumber((currentPage) => Math.max(1, currentPage - 1))
                }
                className="w-full sm:w-auto"
              >
                Назад
              </AppButton>

              <span className="text-center text-sm text-[var(--app-muted)]">
                {page.pageNumber}
                {" / "}
                {page.totalPages}
              </span>

              <AppButton
                type="button"
                variant="secondary"
                disabled={!hasNextPage || historyQuery.isFetching}
                onClick={() => setPageNumber((currentPage) => currentPage + 1)}
                className="w-full sm:w-auto"
              >
                Далее
              </AppButton>
            </div>
          )}
        </>
      )}
    </section>
  );
}
