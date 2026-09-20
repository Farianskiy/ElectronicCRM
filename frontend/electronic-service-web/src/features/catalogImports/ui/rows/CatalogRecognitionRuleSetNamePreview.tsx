"use client";

import { useId, useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import {
  previewRecognitionRuleSetName,
  type RecognitionRuleSetCharacteristicDescription,
} from "../../api/previewRecognitionRuleSetName";

interface CatalogRecognitionRuleSetNamePreviewProps {
  versionId: string;
  manufacturerId: string;
  productTypeId: string;
  disabled: boolean;
}

function getCharacteristicLabel(
  characteristicId: string,
  definitions: RecognitionRuleSetCharacteristicDescription[] | undefined,
): string {
  const definition = definitions?.find(
    (item) => item.characteristicDefinitionId === characteristicId,
  );

  if (!definition) {
    return `Характеристика: ${characteristicId}`;
  }

  if (!definition.unit) {
    return definition.name;
  }

  return `${definition.name}, ${definition.unit}`;
}

export function CatalogRecognitionRuleSetNamePreview({
  versionId,
  manufacturerId,
  productTypeId,
  disabled,
}: CatalogRecognitionRuleSetNamePreviewProps) {
  const inputId = useId();
  const [productName, setProductName] = useState("");
  const pendingRef = useRef(false);

  const preview = useMutation({
    mutationFn: (name: string) =>
      previewRecognitionRuleSetName(versionId, {
        manufacturerId,
        productTypeId,
        productName: name,
      }),
    retry: false,
    onSettled: () => {
      pendingRef.current = false;
    },
  });

  const locked = disabled || preview.isPending;
  const validName = productName.trim().length > 0 && productName.length <= 2000;

  function checkName(): void {
    if (locked || pendingRef.current || !validName) {
      return;
    }

    pendingRef.current = true;
    preview.mutate(productName);
  }

  const result = preview.isSuccess ? preview.data : undefined;

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
      <h5 className="font-semibold">Проверить название этой версией</h5>

      <p className="text-[var(--app-muted)]">
        Введите название, в том числе новое, которого не было в учебных
        примерах. Разметка не требуется. Проверка ничего не сохраняет и не
        активирует правила.
      </p>

      <label htmlFor={inputId}>Наименование товара</label>

      <textarea
        id={inputId}
        rows={3}
        maxLength={2000}
        value={productName}
        disabled={locked}
        onChange={(event) => {
          setProductName(event.target.value);
          preview.reset();
        }}
        className="w-full rounded-lg border border-[var(--app-border)] bg-[var(--app-surface)] p-3 text-[var(--app-text)]"
        placeholder="Вставьте полное название товара"
      />

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked || !validName}
        onClick={checkName}
      >
        {preview.isPending ? "Проверяем..." : "Проверить название"}
      </AppButton>

      {preview.isPending && (
        <p role="status">Проверяем название всеми правилами версии...</p>
      )}

      {preview.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(preview.error, "Не удалось проверить название.")}
        </p>
      )}

      {result && (
        <div className="grid gap-3" aria-live="polite">
          <p className="font-medium">
            Результат проверки версии {result.versionNumber}
          </p>

          <p className="break-words">
            Проверенное название: {result.productName}
          </p>

          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          {result.resolution.hasConflicts && (
            <p role="alert" className="text-[var(--app-danger)]">
              Правила предлагают разные значения для одной характеристики. Для
              конфликтующих характеристик единое значение не выбрано.
            </p>
          )}

          {result.resolution.characteristics.length === 0 && (
            <p>
              Ни одно правило этой версии не предложило значение. Это не
              означает, что в названии отсутствуют характеристики.
            </p>
          )}

          {result.resolution.characteristics.map((item) => (
            <article
              key={item.characteristicDefinitionId}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="break-words font-semibold">
                {getCharacteristicLabel(
                  item.characteristicDefinitionId,
                  result.characteristicDefinitions,
                )}
              </p>

              {item.hasConflict ? (
                <p className="text-[var(--app-danger)]">
                  Конфликт значений: {item.alternativeValues.join(", ")}
                </p>
              ) : (
                <p className="font-semibold">
                  Предложенное значение: {item.proposedValue}
                </p>
              )}

              <details>
                <summary className="cursor-pointer">
                  Основания распознавания ({item.sources.length})
                </summary>

                <div className="mt-2 grid gap-2">
                  {item.sources.map((source, index) => (
                    <div
                      key={`${source.draftId}-${source.spanStart}-${index}`}
                      className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2"
                    >
                      <p>
                        Фрагмент: «{source.rawValue}» → {source.normalizedValue}
                      </p>
                      <p className="break-all text-[var(--app-muted)]">
                        Черновик: {source.draftId}
                      </p>
                    </div>
                  ))}
                </div>
              </details>
            </article>
          ))}

          <p className="text-[var(--app-muted)]">
            Это предварительный результат, а не подтверждение правильности
            товара. Полнота обязательных характеристик здесь не проверяется.
            Импорт не изменён.
          </p>
        </div>
      )}
    </section>
  );
}
