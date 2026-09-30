"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { AppButton } from "@/shared/ui/AppButton";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { previewRecognitionIntegerBatch } from "../../api/previewRecognitionIntegerBatch";
import type { RecognitionTrainingScope } from "../../model/recognitionLiteralProposals";
import type { RecognitionIntegerPattern } from "../../model/recognitionIntegerPatterns";

interface CatalogRecognitionIntegerBatchPreviewPanelProps {
  batchId: string;
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  pattern: RecognitionIntegerPattern;
  disabled: boolean;
}

const statusLabels: Record<string, string> = {
  InvalidScope: "Некорректная область применения",
  InvalidPattern: "Некорректный шаблон",
  ScopeUnknown: "Не определён производитель или тип",
  OutsideScope: "Другой производитель или тип",
  MissingName: "Нет наименования",
  NameTooLong: "Название превышает допустимую длину",
  NoMatch: "Название не подходит под шаблон",
  AmbiguousMatch: "Неоднозначное извлечение",
  NumberOutOfRange: "Число превышает поддерживаемый диапазон",
  SuggestedValue: "Предложено значение",
  CurrentValueNotComparable: "Текущее значение нельзя сравнить как число",
  MatchesCurrentValue: "Совпадает с текущим значением",
  DiffersFromCurrentValue: "Отличается от текущего значения",
};

export function CatalogRecognitionIntegerBatchPreviewPanel({
  batchId,
  scope,
  generatorVersion,
  pattern,
  disabled,
}: CatalogRecognitionIntegerBatchPreviewPanelProps) {
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: (page: number) =>
      previewRecognitionIntegerBatch({
        batchId,
        scope,
        generatorVersion,
        pattern,
        page,
        pageSize: 25,
      }),
    retry: false,
  });

  function load(page: number): void {
    if (!canPreview || disabled || mutation.isPending) {
      return;
    }

    mutation.mutate(page);
  }

  if (!canPreview) {
    return null;
  }

  const result = mutation.isSuccess && !disabled ? mutation.data : null;
  const trainingCount =
    result?.items.filter((item) => item.matchesTrainingName).length ?? 0;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-3">
      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || mutation.isPending}
        onClick={() => load(1)}
      >
        {mutation.isPending
          ? "Проверяем строки..."
          : "Проверить на этом импорте"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Проверяются сохранённые строки. Эта кнопка запускает проверку с первой
        страницы; изменения формы до сохранения не учитываются.
      </p>

      {mutation.isPending && (
        <p role="status">
          Сервер повторно проверяет предложение и обрабатывает страницу...
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось проверить предложение на импорте.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p role="status">
            Страница {result.page}. Проверено строк: {result.items.length}.
          </p>
          <p>Названий из учебной выборки на странице: {trainingCount}.</p>
          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}.
          </p>

          {result.diagnostics.map((issue, index) => (
            <p
              key={`${issue.code}-${index}`}
              className="text-[var(--app-muted)]"
            >
              {issue.message}
            </p>
          ))}

          {result.items.length === 0 && <p>На этой странице нет строк.</p>}

          {result.items.map((item) => (
            <div
              key={item.result.rowId}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold">
                Строка {item.result.rowNumber}:{" "}
                {item.result.productName ?? "Без наименования"}
              </p>
              <p>
                {statusLabels[item.result.status] ??
                  `Неизвестный статус: ${item.result.status}`}
              </p>
              <p>
                Текущее значение: {item.result.currentValue || "Не заполнено"}
              </p>
              <p>
                Предложенное значение:{" "}
                {item.result.proposedValue ?? "Не предлагается"}
              </p>
              <p className="text-[var(--app-muted)]">
                {item.matchesTrainingName
                  ? "Это название уже было в учебной выборке."
                  : "Точного совпадения названия с учебной выборкой нет."}
              </p>
              {item.result.captures.map((capture) => (
                <p key={`${capture.spanStart}-${capture.spanLength}`}>
                  Извлечено: «{capture.rawValue}» → {capture.normalizedValue}
                </p>
              ))}
            </div>
          ))}

          <div className="flex flex-wrap items-center gap-2">
            <AppButton
              type="button"
              variant="secondary"
              disabled={result.page <= 1}
              onClick={() => load(result.page - 1)}
            >
              Назад
            </AppButton>
            <span>Страница {result.page}</span>
            <AppButton
              type="button"
              variant="secondary"
              onClick={() => load(result.page)}
            >
              Обновить страницу
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              disabled={!result.hasMore}
              onClick={() => load(result.page + 1)}
            >
              Далее
            </AppButton>
          </div>

          <p className="text-[var(--app-muted)]">
            Совпадение с текущим значением не доказывает правильность.
            Отсутствие названия в учебной выборке не гарантирует независимость
            примера. Проверка ничего не сохраняет и не активирует.
          </p>
        </div>
      )}
    </section>
  );
}
