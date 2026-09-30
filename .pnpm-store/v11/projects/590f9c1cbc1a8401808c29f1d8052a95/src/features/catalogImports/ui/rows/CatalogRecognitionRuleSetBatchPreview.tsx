"use client";

import { useRef } from "react";
import { useMutation } from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionRuleSetBatch } from "../../api/previewRecognitionRuleSetBatch";

interface CatalogRecognitionRuleSetBatchPreviewProps {
  versionId: string;
  batchId: string;
  disabled: boolean;
}

function statusLabel(status: string): string {
  switch (status) {
    case "Proposed":
      return "Есть предложенные значения";
    case "Conflict":
      return "Конфликт между правилами";
    case "NoMatch":
      return "Правила не предложили значений";
    case "OutsideScope":
      return "Другой или не определённый производитель / тип товара";
    default:
      return `Неизвестный статус: ${status}`;
  }
}

function comparisonLabel(status: string): string {
  switch (status) {
    case "SuggestedValue":
      return "Предложено заполнение пустого поля";
    case "MatchesCurrentValue":
      return "Совпадает с сохранённым значением";
    case "DiffersFromCurrentValue":
      return "Отличается от сохранённого значения";
    case "CurrentValueNotComparable":
      return "Невозможно корректно сравнить значения";
    case "RuleConflict":
      return "Конфликт правил — единое значение не выбрано";
    case "NoProposal":
      return "Версия не предложила значение";
    default:
      return `Неизвестный результат: ${status}`;
  }
}

export function CatalogRecognitionRuleSetBatchPreview({
  versionId,
  batchId,
  disabled,
}: CatalogRecognitionRuleSetBatchPreviewProps) {
  const pendingRef = useRef(false);

  const mutation = useMutation({
    mutationFn: (page: number) =>
      previewRecognitionRuleSetBatch(versionId, batchId, page),
    retry: false,
    onSettled: () => {
      pendingRef.current = false;
    },
  });

  const locked = disabled || mutation.isPending;
  const result = mutation.isSuccess ? mutation.data : undefined;

  function checkPage(page: number): void {
    if (locked || pendingRef.current || page < 1 || page > 10000) {
      return;
    }

    pendingRef.current = true;
    mutation.mutate(page);
  }

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
      <h5 className="font-semibold">Проверка версии на импорте</h5>

      <p className="text-[var(--app-muted)]">
        Проверяются сохранённые строки, по 25 на странице. Несохранённые
        изменения редактора не учитываются. Данные импорта не изменяются.
      </p>

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked}
        onClick={() => checkPage(1)}
      >
        Проверить на этом импорте
      </AppButton>

      {mutation.isPending && (
        <p role="status">Проверяем страницу {mutation.variables}...</p>
      )}

      {mutation.isError && (
        <div className="grid gap-2">
          <p role="alert" className="text-[var(--app-danger)]">
            {getApiErrorMessage(
              mutation.error,
              "Не удалось проверить строки импорта.",
            )}
          </p>
          <AppButton
            type="button"
            variant="secondary"
            disabled={locked}
            onClick={() => checkPage(mutation.variables ?? 1)}
          >
            Повторить проверку страницы
          </AppButton>
        </div>
      )}

      {result && (
        <div className="grid gap-3" aria-live="polite">
          <p className="font-semibold">
            Версия {result.versionNumber}. Страница {result.page}. Проверено
            строк: {result.items.length}.
          </p>

          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          <div className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3">
            <p>С предложениями: {result.proposedRowsCount}</p>
            <p>С конфликтами: {result.conflictRowsCount}</p>
            <p>Без совпадений: {result.noMatchRowsCount}</p>
            <p>Вне области версии: {result.outsideScopeRowsCount}</p>
          </div>

          <p className="text-[var(--app-muted)]">
            Это сводка только этой страницы. Наличие предложения не подтверждает
            его правильность или полноту характеристик.
          </p>

          {result.items.length === 0 && <p>На этой странице нет строк.</p>}

          {result.items.map((row) => (
            <article
              key={row.rowId}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="break-words font-semibold">
                Строка {row.rowNumber}: {row.productName ?? "Без названия"}
              </p>

              <p
                className={
                  row.status === "Conflict"
                    ? "text-[var(--app-danger)]"
                    : "text-[var(--app-muted)]"
                }
              >
                {statusLabel(row.status)}
              </p>

              {row.comparisons && row.comparisons.length > 0 && (
                <div className="grid gap-2">
                  <p className="font-semibold">
                    Сравнение с сохранёнными значениями
                  </p>

                  {row.comparisons.map((comparison) => (
                    <div
                      key={comparison.characteristicDefinitionId}
                      className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
                    >
                      <p className="font-medium">
                        {comparison.name}
                        {comparison.unit ? `, ${comparison.unit}` : ""}
                      </p>

                      <p>
                        Сохранено:{" "}
                        {comparison.currentValue?.trim()
                          ? comparison.currentValue
                          : "Не заполнено"}
                      </p>

                      <p>
                        Предложено:{" "}
                        {comparison.proposedValue ?? "Нет единого предложения"}
                      </p>

                      <p
                        className={
                          [
                            "DiffersFromCurrentValue",
                            "CurrentValueNotComparable",
                            "RuleConflict",
                          ].includes(comparison.status)
                            ? "text-[var(--app-danger)]"
                            : "text-[var(--app-muted)]"
                        }
                      >
                        {comparisonLabel(comparison.status)}
                      </p>
                    </div>
                  ))}

                  <p className="text-[var(--app-muted)]">
                    Сохранённое значение не обязательно подтверждено человеком.
                    Совпадение не является оценкой точности. Ни одно значение
                    этой проверкой не перезаписывается.
                  </p>
                </div>
              )}

              {row.preview?.resolution.characteristics.map((item) => {
                const definition = row.preview?.characteristicDefinitions?.find(
                  (candidate) =>
                    candidate.characteristicDefinitionId ===
                    item.characteristicDefinitionId,
                );

                const label =
                  definition?.name ?? item.characteristicDefinitionId;

                return (
                  <div
                    key={item.characteristicDefinitionId}
                    className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
                  >
                    <p className="break-words font-medium">
                      {label}
                      {definition?.unit ? `, ${definition.unit}` : ""}
                    </p>

                    {item.hasConflict ? (
                      <p className="text-[var(--app-danger)]">
                        Конфликт: {item.alternativeValues.join(", ")}. Единое
                        значение не выбрано.
                      </p>
                    ) : (
                      <p>Предложено: {item.proposedValue}</p>
                    )}

                    <details>
                      <summary className="cursor-pointer">
                        Основания ({item.sources.length})
                      </summary>

                      <div className="mt-2 grid gap-2">
                        {item.sources.map((source, index) => (
                          <div
                            key={`${source.draftId}-${source.spanStart}-${index}`}
                            className="grid gap-1"
                          >
                            <p>
                              «{source.rawValue}» → {source.normalizedValue}
                            </p>
                            <p className="break-all text-[var(--app-muted)]">
                              Черновик: {source.draftId}
                            </p>
                          </div>
                        ))}
                      </div>
                    </details>
                  </div>
                );
              })}
            </article>
          ))}

          <div className="flex flex-wrap items-center gap-2">
            <AppButton
              type="button"
              variant="secondary"
              disabled={locked || result.page <= 1}
              onClick={() => checkPage(result.page - 1)}
            >
              Предыдущая страница строк
            </AppButton>

            <AppButton
              type="button"
              variant="secondary"
              disabled={locked}
              onClick={() => checkPage(result.page)}
            >
              Перепроверить страницу
            </AppButton>

            <AppButton
              type="button"
              variant="secondary"
              disabled={locked || !result.hasMore || result.page >= 10000}
              onClick={() => checkPage(result.page + 1)}
            >
              Следующая страница строк
            </AppButton>
          </div>

          <p className="text-[var(--app-muted)]">
            Результат относится к моменту запроса. После изменения строк
            выполните проверку повторно. Правила не активированы, предложенные
            значения не сохранены в импорт.
          </p>
        </div>
      )}
    </section>
  );
}
