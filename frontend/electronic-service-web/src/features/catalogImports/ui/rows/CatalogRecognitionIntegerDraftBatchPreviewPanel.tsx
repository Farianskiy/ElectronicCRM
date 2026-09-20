"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionIntegerDraftBatch } from "../../api/previewRecognitionIntegerDraftBatch";

interface CatalogRecognitionIntegerDraftBatchPreviewPanelProps {
  draftId: string;
  batchId: string;
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

export function CatalogRecognitionIntegerDraftBatchPreviewPanel({
  draftId,
  batchId,
}: CatalogRecognitionIntegerDraftBatchPreviewPanelProps) {
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: (page: number) =>
      previewRecognitionIntegerDraftBatch(draftId, batchId, page),
    retry: false,
  });

  function load(page: number): void {
    if (!canPreview || mutation.isPending || page < 1 || page > 10000) {
      return;
    }

    mutation.mutate(page);
  }

  if (!canPreview) {
    return null;
  }

  const result = mutation.isSuccess ? mutation.data : null;

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3 text-sm">
      <AppButton
        type="button"
        variant="secondary"
        disabled={mutation.isPending}
        onClick={() => load(1)}
      >
        {mutation.isPending
          ? "Проверяем сохранённый шаблон..."
          : "Проверить на этом импорте"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Используется сохранённый числовой шаблон. Проверяются строки из базы, по
        25 на странице. Несохранённые изменения редактора не учитываются.
        Нажатие этой кнопки начинает проверку с первой страницы.
      </p>

      {mutation.isPending && (
        <p role="status">
          Перепроверяем учебные основания и сопоставляем строки импорта...
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось проверить сохранённый шаблон на импорте.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p role="status" className="font-semibold">
            Страница {result.page}. Проверено строк: {result.items.length}.
          </p>
          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>
          <p>
            Учебные основания перепроверены:{" "}
            {new Date(result.recheck.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          {!result.recheck.evidenceUnchanged && (
            <p className="text-[var(--app-muted)]">
              Набор подтверждений изменился после сохранения черновика, но
              сохранённый шаблон прошёл текущую перепроверку. История черновика
              не перезаписана.
            </p>
          )}

          {result.items.length === 0 && <p>На этой странице нет строк.</p>}

          {result.items.map((item) => (
            <article
              key={item.rowId}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="break-words font-semibold">
                Строка {item.rowNumber}:{" "}
                {item.productName ?? "Без наименования"}
              </p>
              <p>
                {statusLabels[item.status] ??
                  `Неизвестный статус: ${item.status}`}
              </p>
              <p>Текущее значение: {item.currentValue || "Не заполнено"}</p>
              <p>
                Предложенное значение: {item.proposedValue ?? "Не предлагается"}
              </p>

              {item.captures.map((capture) => (
                <p key={`${capture.spanStart}-${capture.spanLength}`}>
                  Извлечено: «{capture.rawValue}» → {capture.normalizedValue}
                </p>
              ))}
            </article>
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
              disabled={!result.hasMore || result.page >= 10000}
              onClick={() => load(result.page + 1)}
            >
              Далее
            </AppButton>
          </div>

          <p className="text-[var(--app-muted)]">
            Это предварительное сравнение, а не применение правила. Значения
            строк не изменены. Совпадение с текущим значением не доказывает
            правильность. Страницы проверяются отдельными запросами и не
            являются единым зафиксированным снимком импорта.
          </p>
        </div>
      )}
    </section>
  );
}
