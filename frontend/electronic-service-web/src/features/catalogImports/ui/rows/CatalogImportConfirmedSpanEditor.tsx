"use client";

import { useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppSelect } from "@/shared/ui/AppSelect";
import {
  catalogImportConfirmedSpansQueryKey,
  getCatalogImportRowConfirmedSpans,
} from "../../api/getCatalogImportRowConfirmedSpans";
import type { CatalogImportConfirmedSpan } from "../../model/types";
import { CatalogImportTrainingExampleConfirmation } from "./CatalogImportTrainingExampleConfirmation";
import { CatalogRecognitionLiteralProposalPreview } from "./CatalogRecognitionLiteralProposalPreview";
import { CatalogRecognitionMultiIntegerPreview } from "./CatalogRecognitionMultiIntegerPreview";

interface CatalogImportConfirmedSpanEditorProps {
  batchId: string;
  rowId: string;
  productName: string;
  manufacturerId: string;
  productTypeId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  characteristicValues: Record<string, string>;
  drafts: Record<string, CatalogImportConfirmedSpan>;
  disabled: boolean;
  onChange: (drafts: Record<string, CatalogImportConfirmedSpan>) => void;
}

export function CatalogImportConfirmedSpanEditor({
  batchId,
  rowId,
  productName,
  manufacturerId,
  productTypeId,
  characteristics,
  characteristicValues,
  drafts,
  disabled,
  onChange,
}: CatalogImportConfirmedSpanEditorProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [characteristicId, setCharacteristicId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const query = useQuery({
    queryKey: catalogImportConfirmedSpansQueryKey(batchId, rowId),
    queryFn: () => getCatalogImportRowConfirmedSpans(batchId, rowId),
    staleTime: 0,
  });

  const savedEntries = query.data ?? [];
  const selectedFeedback = savedEntries.find(
    (entry) => entry.characteristicDefinitionId === characteristicId,
  );
  const selectedValue = characteristicValues[characteristicId]?.trim() ?? "";
  const name = productName.trim();

  const contextMismatch = Boolean(
    selectedFeedback &&
    (selectedFeedback.productName !== name ||
      selectedFeedback.manufacturerId !== manufacturerId ||
      selectedFeedback.productTypeId !== productTypeId),
  );

  const cannotSelect =
    disabled ||
    !query.isSuccess ||
    !characteristicId ||
    !selectedValue ||
    !manufacturerId ||
    !name ||
    Boolean(selectedFeedback?.isFinalized) ||
    contextMismatch;

  function bindSelection(): void {
    if (cannotSelect) {
      return;
    }

    const textarea = textareaRef.current;

    if (!textarea) {
      return;
    }

    const start = textarea.selectionStart;
    const length = textarea.selectionEnd - start;
    const fragment = name.slice(start, start + length);

    if (length <= 0 || !fragment.trim()) {
      setError("Выделите нужный фрагмент в названии.");
      return;
    }

    onChange({
      ...drafts,
      [characteristicId]: { productName: name, start, length },
    });

    setError(null);
  }

  function cancelDraft(id: string): void {
    const nextDrafts = { ...drafts };
    delete nextDrafts[id];
    onChange(nextDrafts);
    setError(null);
  }

  return (
    <section className="grid gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
      <div>
        <h3 className="font-semibold text-[var(--app-text)]">
          Разметка и правила распознавания
        </h3>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Выберите характеристику. Для просмотра сохранённых шаблонов и проверки
          импорта разметка этой строки не нужна. Чтобы добавить учебный пример,
          заполните правильное значение, выделите соответствующий фрагмент
          названия и сохраните строку.
        </p>
      </div>

      {query.isPending && (
        <p role="status">Загружаем сохранённую разметку...</p>
      )}

      {query.isError && (
        <div role="alert" className="text-sm text-[var(--app-danger)]">
          <p>
            {getApiErrorMessage(query.error, "Не удалось загрузить разметку.")}
          </p>
          <AppButton
            type="button"
            variant="secondary"
            onClick={() => void query.refetch()}
          >
            Повторить
          </AppButton>
        </div>
      )}

      <AppSelect
        ariaLabel="Характеристика для разметки и просмотра правил"
        value={characteristicId}
        disabled={disabled}
        onChange={(value) => {
          setCharacteristicId(value);
          setError(null);
        }}
        options={[
          { value: "", label: "Выберите характеристику" },
          ...characteristics.map((item) => ({
            value: item.id,
            label: item.name,
          })),
        ]}
      />

      {characteristicId && (
        <p className="text-sm">
          Правильное значение:{" "}
          {selectedValue || "сначала заполните характеристику выше"}
        </p>
      )}

      <textarea
        ref={textareaRef}
        aria-label="Выделите фрагмент названия для характеристики"
        value={name}
        readOnly
        rows={3}
        className="w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 text-sm text-[var(--app-text)]"
      />

      {selectedFeedback?.isFinalized && (
        <p className="text-sm text-[var(--app-muted)]">
          Решение по распознаванию уже финализировано. Изменение разметки здесь
          недоступно. Подтверждение для обучения отображается отдельно ниже.
        </p>
      )}
      {contextMismatch && (
        <p className="text-sm text-[var(--app-danger)]">
          Сохранённый Feedback относится к другому названию, производителю или
          типу товара. Для него нельзя назначить этот фрагмент.
        </p>
      )}
      {error && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {error}
        </p>
      )}

      <AppButton
        type="button"
        variant="secondary"
        disabled={cannotSelect}
        onClick={bindSelection}
      >
        Связать выделенный фрагмент
      </AppButton>

      <div className="grid gap-2">
        {characteristics.map((characteristic) => {
          const draft = drafts[characteristic.id];
          const saved = savedEntries.find(
            (entry) => entry.characteristicDefinitionId === characteristic.id,
          );
          const fragment = draft
            ? draft.productName.slice(draft.start, draft.start + draft.length)
            : saved?.confirmedRawValue;

          if (!fragment) {
            return null;
          }

          return (
            <div
              key={characteristic.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[var(--app-border)] p-3 text-sm"
            >
              <span>
                {characteristic.name}: «{fragment}» —{" "}
                {draft ? "будет сохранено со строкой" : "сохранено ранее"}
              </span>
              {draft && (
                <AppButton
                  type="button"
                  variant="secondary"
                  disabled={disabled}
                  onClick={() => cancelDraft(characteristic.id)}
                >
                  Отменить выделение
                </AppButton>
              )}
            </div>
          );
        })}
      </div>

      {selectedFeedback && (
        <CatalogImportTrainingExampleConfirmation
          key={JSON.stringify([
            batchId,
            rowId,
            selectedFeedback,
            name,
            manufacturerId,
            productTypeId,
            selectedValue,
            drafts,
          ])}
          batchId={batchId}
          rowId={rowId}
          characteristicName={
            characteristics.find((item) => item.id === characteristicId)
              ?.name ?? "Характеристика"
          }
          saved={selectedFeedback}
          productName={name}
          manufacturerId={manufacturerId}
          productTypeId={productTypeId}
          currentValue={selectedValue}
          hasDrafts={Object.keys(drafts).length > 0}
          disabled={disabled || !query.isSuccess || query.isFetching}
        />
      )}

      {characteristicId && !selectedFeedback && query.isSuccess && (
        <p className="text-sm text-[var(--app-muted)]">
          У этой строки пока нет сохранённой разметки выбранной характеристики.
          Это не мешает просматривать правила и проверять импорт. Разметка
          потребуется только для добавления нового учебного примера.
        </p>
      )}

      {characteristicId && (!manufacturerId || !productTypeId) && (
        <p className="text-sm text-[var(--app-muted)]">
          Для просмотра правил укажите производителя и тип товара.
        </p>
      )}

      {characteristicId && manufacturerId && productTypeId && (
        <div className="grid gap-3">
          <p className="text-sm text-[var(--app-muted)]">
            Ниже показаны правила для выбранных в форме производителя, типа
            товара и характеристики. Проверка импорта читает сохранённые строки
            из базы — несохранённые изменения формы не учитываются.
          </p>

          <CatalogRecognitionLiteralProposalPreview
            key={JSON.stringify([
              "recognition-rules",
              batchId,
              rowId,
              manufacturerId,
              productTypeId,
              characteristicId,
              query.dataUpdatedAt,
            ])}
            batchId={batchId}
            manufacturerId={manufacturerId}
            productTypeId={productTypeId}
            characteristicDefinitionId={characteristicId}
            characteristicName={
              characteristics.find((item) => item.id === characteristicId)
                ?.name ?? "Характеристика"
            }
            disabled={disabled}
          />
        </div>
      )}

      {manufacturerId && productTypeId && (
        <CatalogRecognitionMultiIntegerPreview
          key={JSON.stringify([
            "multi-integer-preview",
            batchId,
            rowId,
            manufacturerId,
            productTypeId,
          ])}
          batchId={batchId}
          manufacturerId={manufacturerId}
          productTypeId={productTypeId}
          characteristics={characteristics}
          disabled={disabled}
        />
      )}

      <p className="text-xs text-[var(--app-muted)]">
        «Сохранить строку» сохраняет разметку. «Подтвердить для обучения»
        отдельно сохраняет проверенный учебный пример. «Отменить выделение»
        отменяет только новое несохранённое выделение.
      </p>
    </section>
  );
}
