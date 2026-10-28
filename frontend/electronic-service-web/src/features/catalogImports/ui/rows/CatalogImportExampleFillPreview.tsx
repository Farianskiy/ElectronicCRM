"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { applyCatalogImportExampleFill } from "../../api/applyCatalogImportExampleFill";
import { previewCatalogImportExampleFill } from "../../api/previewCatalogImportExampleFill";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  CatalogImportExampleFillRow,
  CatalogImportExampleFillRowState,
  CatalogImportRow,
  PreviewCatalogImportExampleFillResponse,
} from "../../model/types";

interface CatalogImportExampleFillPreviewProps {
  batchId: string;
  expectedVersion: number;
  exampleRows: CatalogImportRow[];
  onCancel: () => void;
  onApplied: () => void;
}

function getStateLabel(row: CatalogImportExampleFillRow): string {
  if (row.state === "Ready") {
    return "Готово к заполнению";
  }

  if (row.state === "Review") {
    return "Нужно проверить";
  }

  if (row.state === "Unmatched") {
    return "Не найдено";
  }

  return "Есть конфликт";
}

function getStateClasses(row: CatalogImportExampleFillRow): string {
  if (row.state === "Ready") {
    return "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]";
  }

  if (row.state === "Review") {
    return "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]";
  }

  if (row.state === "Unmatched") {
    return "border-[var(--app-border)] bg-[var(--app-panel-strong)] text-[var(--app-muted)]";
  }

  return "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]";
}

export function CatalogImportExampleFillPreview({
  batchId,
  expectedVersion,
  exampleRows,
  onCancel,
  onApplied,
}: CatalogImportExampleFillPreviewProps) {
  const queryClient = useQueryClient();
  const [preview, setPreview] =
    useState<PreviewCatalogImportExampleFillResponse | null>(null);
  const [selectedTargetIds, setSelectedTargetIds] = useState<Set<string>>(
    () => new Set(),
  );
  const [resultFilter, setResultFilter] = useState<
    CatalogImportExampleFillRowState | "All"
  >("All");

  const previewMutation = useMutation({
    mutationFn: () =>
      previewCatalogImportExampleFill(batchId, {
        expectedVersion,
        exampleRowIds: exampleRows.map((row) => row.rowId),
        targetRowIds: [],
      }),
    onSuccess: (result) => {
      setPreview(result);
      setResultFilter("All");
      setSelectedTargetIds(
        new Set(
          result.rows
            .filter(
              (row) => row.state === "Ready" && row.proposals.length > 0,
            )
            .map((row) => row.rowId),
        ),
      );
    },
  });

  const applyMutation = useMutation({
    mutationFn: async () => {
      if (!preview) {
        throw new Error("Сначала выполните предпросмотр заполнения.");
      }

      return applyCatalogImportExampleFill(batchId, {
        expectedVersion: preview.version,
        exampleRowIds: exampleRows.map((row) => row.rowId),
        targetRowIds: preview.rows
          .filter((row) => selectedTargetIds.has(row.rowId))
          .map((row) => row.rowId),
      });
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowsRoot(batchId),
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowProblemCodesRoot(batchId),
          refetchType: "none",
        }),
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),
      ]);

      onApplied();
    },
  });

  const isBusy = previewMutation.isPending || applyMutation.isPending;
  const selectedCount = selectedTargetIds.size;
  const visibleRows = preview?.rows.filter(
    (row) => resultFilter === "All" || row.state === resultFilter,
  ) ?? [];

  function toggleTarget(rowId: string): void {
    setSelectedTargetIds((currentIds) => {
      const nextIds = new Set(currentIds);
      if (nextIds.has(rowId)) {
        nextIds.delete(rowId);
      } else {
        nextIds.add(rowId);
      }

      return nextIds;
    });
  }

  return (
    <section className="mt-4 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="max-w-3xl">
          <h3 className="font-semibold text-[var(--app-text)]">
            Заполнение всего импорта по подтверждённым примерам
          </h3>
          <p className="mt-1 text-sm leading-6 text-[var(--app-muted)]">
            Выбранные строки используются только как примеры. CRM найдёт в
            во всём импорте такие же фрагменты наименования и сначала покажет
            предлагаемые значения. Тип товара и производитель должны совпадать.
          </p>
        </div>

        <AppButton size="sm" variant="ghost" disabled={isBusy} onClick={onCancel}>
          Закрыть
        </AppButton>
      </div>

      <div className="mt-3 flex flex-wrap gap-2 text-xs text-[var(--app-muted)]">
        <span className="rounded-full border border-[var(--app-border)] px-2.5 py-1">
          Примеров выбрано: {exampleRows.length}
        </span>
        <span className="rounded-full border border-[var(--app-border)] px-2.5 py-1">
          Проверяем: {preview ? `${preview.targetRowsCount} строк` : "весь импорт, до 2000 строк"}
        </span>
      </div>

      {!preview && (
        <div className="mt-4 flex flex-wrap items-center gap-3">
          <AppButton
            variant="primary"
            loading={previewMutation.isPending}
            onClick={() => previewMutation.mutate()}
          >
            Найти значения по примерам
          </AppButton>
          <p className="text-xs text-[var(--app-muted)]">
            Данные не изменятся, пока вы отдельно не примените результат.
          </p>
        </div>
      )}

      {previewMutation.isError && (
        <p role="alert" className="mt-3 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            previewMutation.error,
            "Не удалось построить предложения. Убедитесь, что для выбранных строк уже подтверждены обучающие примеры.",
          )}
        </p>
      )}

      {preview && (
        <>
          <dl className="mt-4 grid gap-2 sm:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">Готово</dt>
              <dd className="mt-1 text-xl font-semibold text-[var(--app-success)]">
                {preview.readyRowsCount}
              </dd>
            </div>
            <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">Проверить</dt>
              <dd className="mt-1 text-xl font-semibold text-[var(--app-warning)]">
                {preview.reviewRowsCount}
              </dd>
            </div>
            <div className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">Конфликты</dt>
              <dd className="mt-1 text-xl font-semibold text-[var(--app-danger)]">
                {preview.conflictRowsCount}
              </dd>
            </div>
            <div className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3">
              <dt className="text-xs text-[var(--app-muted)]">Без совпадений</dt>
              <dd className="mt-1 text-xl font-semibold text-[var(--app-text)]">
                {preview.unmatchedRowsCount}
              </dd>
            </div>
          </dl>

          <div className="mt-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3 text-xs leading-5 text-[var(--app-muted)]">
            <p><strong className="text-[var(--app-text)]">Готово</strong> — значение подтверждают минимум два разных примера; такие строки выбраны автоматически.</p>
            <p><strong className="text-[var(--app-text)]">Проверить</strong> — найден только один пример; строку можно выбрать вручную.</p>
            <p><strong className="text-[var(--app-text)]">Конфликт</strong> — найдено несколько значений или в строке уже сохранено другое значение.</p>
            <p><strong className="text-[var(--app-text)]">Без совпадений</strong> — ниже показана конкретная причина для каждой строки.</p>
          </div>

          <div className="mt-3 flex flex-wrap gap-2">
            {([
              ["All", `Все · ${preview.targetRowsCount}`],
              ["Ready", `Готово · ${preview.readyRowsCount}`],
              ["Review", `Проверить · ${preview.reviewRowsCount}`],
              ["Conflict", `Конфликты · ${preview.conflictRowsCount}`],
              ["Unmatched", `Без совпадений · ${preview.unmatchedRowsCount}`],
            ] as const).map(([value, label]) => (
              <AppButton
                key={value}
                size="sm"
                variant={resultFilter === value ? "primary" : "secondary"}
                onClick={() => setResultFilter(value)}
              >
                {label}
              </AppButton>
            ))}
          </div>

          {visibleRows.length === 0 ? (
            <p className="mt-4 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm text-[var(--app-muted)]">
              В выбранной группе строк нет.
            </p>
          ) : (
            <div className="mt-4 grid max-h-[52vh] gap-3 overflow-y-auto pr-1">
              {visibleRows.map((row) => {
                const canSelect =
                  row.state !== "Conflict" && row.proposals.length > 0;

                return (
                  <article
                    key={row.rowId}
                    className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-3"
                  >
                    <div className="flex items-start gap-3">
                      <input
                        type="checkbox"
                        checked={selectedTargetIds.has(row.rowId)}
                        disabled={!canSelect || isBusy}
                        aria-label={`Применить предложения к строке ${row.rowNumber}`}
                        onChange={() => toggleTarget(row.rowId)}
                        className="mt-1 h-4 w-4 accent-[var(--app-accent-strong)]"
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center justify-between gap-2">
                          <strong className="text-sm text-[var(--app-text)]">
                            Строка {row.rowNumber}
                          </strong>
                          <span
                            className={`rounded-full border px-2.5 py-1 text-xs font-medium ${getStateClasses(row)}`}
                          >
                            {getStateLabel(row)}
                          </span>
                        </div>
                        <p className="mt-1 break-words text-sm text-[var(--app-text)]">
                          {row.productName || "Наименование не указано"}
                        </p>

                        {row.reason && (
                          <p className="mt-2 rounded-lg border border-[var(--app-border)] bg-[var(--app-panel)] p-2.5 text-xs leading-5 text-[var(--app-muted)]">
                            {row.reason}
                          </p>
                        )}

                        {row.proposals.length > 0 && (
                          <div className="mt-3 grid gap-2">
                            {row.proposals.map((proposal) => (
                              <div
                                key={proposal.characteristicDefinitionId}
                                className="rounded-lg border border-[var(--app-border)] bg-[var(--app-panel)] p-2.5 text-sm"
                              >
                                <strong>{proposal.characteristicName}:</strong>{" "}
                                {proposal.proposedValue}
                                <span className="ml-2 text-xs text-[var(--app-muted)]">
                                  фрагмент «{proposal.matchedRawValue}» · {Math.round(proposal.confidence * 100)}%
                                </span>
                              </div>
                            ))}
                          </div>
                        )}

                        {row.conflicts.map((conflict) => (
                          <p
                            key={conflict.characteristicDefinitionId}
                            className="mt-2 text-xs leading-5 text-[var(--app-danger)]"
                          >
                            <strong>{conflict.characteristicName}:</strong>{" "}
                            {conflict.reason}
                          </p>
                        ))}
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}

          <div className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-[var(--app-border)] pt-4">
            <p className="text-sm text-[var(--app-muted)]">
              Выбрано для заполнения: <strong className="text-[var(--app-text)]">{selectedCount}</strong>
            </p>
            <div className="flex flex-wrap gap-2">
              <AppButton
                size="sm"
                variant="secondary"
                disabled={isBusy}
                onClick={() => previewMutation.mutate()}
              >
                Пересчитать
              </AppButton>
              <AppButton
                size="sm"
                variant="primary"
                loading={applyMutation.isPending}
                disabled={selectedCount === 0}
                onClick={() => applyMutation.mutate()}
              >
                Заполнить выбранные строки
              </AppButton>
            </div>
          </div>
        </>
      )}

      {applyMutation.isError && (
        <p role="alert" className="mt-3 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            applyMutation.error,
            "Не удалось применить найденные значения.",
          )}
        </p>
      )}
    </section>
  );
}
