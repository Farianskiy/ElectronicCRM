"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionMultiIntegerBatch } from "../../api/previewRecognitionMultiIntegerBatch";
import type {
  RecognitionMultiIntegerRequest,
  RecognitionMultiIntegerProposal,
} from "../../model/recognitionMultiIntegerProposals";

interface CatalogRecognitionMultiIntegerBatchPreviewPanelProps {
  batchId: string;
  trainingRequest: RecognitionMultiIntegerRequest;
  generatorVersion: string;
  proposal: RecognitionMultiIntegerProposal;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

const statusLabels: Record<string, string> = {
  Matched: "Название соответствует структуре шаблона",
  InvalidScope: "Некорректная область применения",
  InvalidPattern: "Некорректный шаблон",
  ScopeUnknown: "Не определён производитель или тип",
  OutsideScope: "Другой производитель или тип",
  MissingName: "Нет наименования",
  NameTooLong: "Название превышает допустимую длину",
  NoMatch: "Название не подходит под шаблон",
  NumberOutOfRange: "Число превышает поддерживаемый диапазон",
  SuggestedValue: "Предложено заполнить значение",
  CurrentValueNotComparable: "Текущее значение нельзя сравнить как число",
  MatchesCurrentValue: "Совпадает с текущим значением",
  DiffersFromCurrentValue: "Отличается от текущего значения",
};

export function CatalogRecognitionMultiIntegerBatchPreviewPanel({
  batchId,
  trainingRequest,
  generatorVersion,
  proposal,
  characteristics,
  disabled,
}: CatalogRecognitionMultiIntegerBatchPreviewPanelProps) {
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: (page: number) =>
      previewRecognitionMultiIntegerBatch({
        ...trainingRequest,
        batchId,
        generatorVersion,
        pattern: proposal.pattern,
        page,
        pageSize: 25,
      }),
    retry: false,
  });

  function load(page: number): void {
    if (
      !canPreview ||
      disabled ||
      !proposal.passedExamples ||
      mutation.isPending ||
      page < 1 ||
      page > 10000
    ) {
      return;
    }

    mutation.mutate(page);
  }

  function characteristicName(id: string): string {
    return characteristics.find((item) => item.id === id)?.name ?? id;
  }

  if (!canPreview || !proposal.passedExamples) {
    return null;
  }

  const result = mutation.isSuccess && !disabled ? mutation.data : null;

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3 text-sm">
      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || mutation.isPending}
        onClick={() => load(1)}
      >
        {mutation.isPending
          ? "Проверяем составной шаблон..."
          : "Проверить на этом импорте"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Проверяются сохранённые строки по 25 на странице. Несохранённые
        изменения формы не учитываются. Значения в импорте не изменяются.
      </p>

      {mutation.isPending && (
        <p role="status">
          Повторно проверяем предложение по учебным примерам и обрабатываем
          строки...
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось проверить составное предложение на импорте.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p role="status">
            Страница {result.page}. Проверено строк: {result.items.length}.
          </p>
          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          {result.items.length === 0 && <p>На этой странице нет строк.</p>}

          {result.items.map((item) => (
            <article
              key={item.result.rowId}
              className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="break-words font-semibold">
                Строка {item.result.rowNumber}:{" "}
                {item.result.productName ?? "Без наименования"}
              </p>
              <p>{statusLabels[item.result.status] ?? item.result.status}</p>
              <p className="text-[var(--app-muted)]">
                {item.matchesSelectedTrainingName
                  ? "Название входит в выбранную учебную подборку."
                  : "Название не входит в выбранную учебную подборку. Это само по себе не доказывает независимость примера."}
              </p>

              {item.result.fields.map((entry) => (
                <div
                  key={entry.characteristicDefinitionId}
                  className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3"
                >
                  <p className="font-medium">
                    {characteristicName(entry.characteristicDefinitionId)}
                  </p>
                  <p>{statusLabels[entry.status] ?? entry.status}</p>
                  <p>
                    Текущее значение: {entry.currentValue || "Не заполнено"}
                  </p>
                  <p>
                    Предложенное значение:{" "}
                    {entry.proposedValue ?? "Не предлагается"}
                  </p>
                  {entry.capture && (
                    <p>
                      Извлечено: «{entry.capture.rawValue}» →{" "}
                      {entry.capture.normalizedValue}
                    </p>
                  )}
                </div>
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
            Совпадение структуры не означает правильность всех характеристик.
            Совпадение с текущими значениями не является независимой оценкой
            точности. Каждая страница проверяется отдельным запросом. Правило не
            сохранено и не активировано.
          </p>
        </div>
      )}
    </section>
  );
}
