"use client";

import Link from "next/link";
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

  const learningHref =
    manufacturerId && productTypeId
      ? `/catalog/recognition/learning?${new URLSearchParams({
          manufacturerId,
          productTypeId,
          branch: "rules",
          section: "prepare",
          batchId,
        }).toString()}`
      : null;

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
          Разметка учебного примера
        </h3>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Чтобы добавить учебный пример, заполните правильное значение, выберите
          характеристику, выделите соответствующий фрагмент названия и сохраните
          строку. После сохранения пример нужно подтвердить отдельно.
        </p>
        <p className="mt-2 text-sm text-[var(--app-muted)]">
          Подтверждение сохраняет проверенное значение и фрагмент названия, но
          не создаёт и не включает новое правило автоматически.
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
        ariaLabel="Характеристика для разметки учебного примера"
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
          Заполните правильное значение, выделите фрагмент названия, свяжите его
          с характеристикой и нажмите «Сохранить строку».
        </p>
      )}

      <section className="grid gap-3 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4">
        <div>
          <h4 className="font-semibold text-[var(--app-text)]">
            Подготовка и включение правил
          </h4>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Генерация черновиков, составление версии, оценка и включение
            изменений выполняются в рабочем пространстве обучения.
          </p>
        </div>

        {learningHref ? (
          <>
            <p className="text-sm text-[var(--app-muted)]">
              Производитель, тип товара и пакет импорта будут переданы
              автоматически. Обучение откроется в новой вкладке, поэтому
              несохранённые изменения этой строки останутся в форме.
            </p>

            <Link
              href={learningHref}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center justify-center rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] px-4 py-3 text-sm font-semibold text-[var(--app-accent)] transition hover:bg-[var(--app-panel-strong)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--app-accent)]"
            >
              Открыть обучение для этого товара
            </Link>
          </>
        ) : (
          <p className="text-sm text-[var(--app-muted)]">
            Выберите производителя и тип товара, чтобы открыть обучение для
            нужных товаров. Введённые данные сначала сохраните кнопкой
            «Сохранить строку».
          </p>
        )}
      </section>

      <p className="text-xs text-[var(--app-muted)]">
        «Сохранить строку» сохраняет значения товара и разметку фрагмента.
        «Подтвердить для обучения» отдельно создаёт проверенный учебный пример.
        Подготовка правила и его включение выполняются позже в рабочем
        пространстве обучения.
      </p>
    </section>
  );
}
