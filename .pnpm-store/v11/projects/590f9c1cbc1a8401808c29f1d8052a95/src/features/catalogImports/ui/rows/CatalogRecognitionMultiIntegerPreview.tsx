"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionMultiInteger } from "../../api/previewRecognitionMultiInteger";
import type { RecognitionMultiIntegerRequest } from "../../model/recognitionMultiIntegerProposals";
import { CatalogRecognitionMultiIntegerBatchPreviewPanel } from "./CatalogRecognitionMultiIntegerBatchPreviewPanel";
import { CatalogRecognitionMultiIntegerDraftSave } from "./CatalogRecognitionMultiIntegerDraftSave";
import { CatalogRecognitionMultiIntegerDraftList } from "./CatalogRecognitionMultiIntegerDraftList";
import { CatalogRecognitionRuleSetVersionList } from "./CatalogRecognitionRuleSetVersionList";
import { CatalogRecognitionRuleSetVersionCreate } from "./CatalogRecognitionRuleSetVersionCreate";

interface CatalogRecognitionMultiIntegerPreviewProps {
  hideVersionManagement?: boolean;
  batchId: string;
  manufacturerId: string;
  productTypeId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

export function CatalogRecognitionMultiIntegerPreview({
  batchId,
  manufacturerId,
  productTypeId,
  characteristics,
  hideVersionManagement = false,
  disabled,
}: CatalogRecognitionMultiIntegerPreviewProps) {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [namesText, setNamesText] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");
  const numericCharacteristics = characteristics.filter(
    (item) => item.dataType === "Number",
  );

  const mutation = useMutation({
    mutationFn: (request: RecognitionMultiIntegerRequest) =>
      previewRecognitionMultiInteger(request),
    retry: false,
  });

  const locked = disabled || mutation.isPending;
  const result = mutation.isSuccess && !disabled ? mutation.data : null;

  function characteristicName(id: string): string {
    return characteristics.find((item) => item.id === id)?.name ?? id;
  }

  function toggleCharacteristic(id: string): void {
    if (locked) {
      return;
    }

    setSelectedIds((current) =>
      current.includes(id)
        ? current.filter((value) => value !== id)
        : [...current, id],
    );
    setValidationError(null);
    mutation.reset();
  }

  function preview(): void {
    if (!canPreview || locked) {
      return;
    }

    const productNames = namesText
      .split(/\r\n|\n|\r/)
      .filter((value) => value.trim().length > 0);

    if (selectedIds.length < 2 || selectedIds.length > 16) {
      setValidationError("Выберите от 2 до 16 числовых характеристик.");
      return;
    }

    if (
      selectedIds.some(
        (id) => !numericCharacteristics.some((item) => item.id === id),
      )
    ) {
      setValidationError(
        "Состав характеристик изменился. Выберите действующие числовые характеристики.",
      );
      return;
    }

    if (productNames.length < 2 || productNames.length > 200) {
      setValidationError("Введите от 2 до 200 разных учебных названий.");
      return;
    }

    if (productNames.some((value) => value.length > 2000)) {
      setValidationError("Название не должно превышать 2000 символов.");
      return;
    }

    if (new Set(productNames).size !== productNames.length) {
      setValidationError("В подборке повторяются одинаковые названия.");
      return;
    }

    setValidationError(null);
    mutation.mutate({
      manufacturerId,
      productTypeId,
      characteristicDefinitionIds: selectedIds,
      productNames,
    });
  }

  if (!canPreview) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <h4 className="font-semibold">
        Составной шаблон: несколько числовых характеристик
      </h4>

      <p className="text-[var(--app-muted)]">
        Выберите характеристики и вставьте точные сохранённые названия учебных
        товаров. Для каждого названия должны быть отдельно подтверждены
        фрагменты всех выбранных характеристик.
      </p>

      {numericCharacteristics.length < 2 && (
        <p className="text-[var(--app-muted)]">
          У этого типа товара меньше двух числовых характеристик. Построение
          составного числового шаблона недоступно.
        </p>
      )}

      <fieldset disabled={locked} className="grid gap-2">
        <legend className="mb-2 font-medium">Извлекаемые характеристики</legend>
        {numericCharacteristics.map((item) => (
          <label key={item.id} className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={selectedIds.includes(item.id)}
              disabled={
                locked ||
                (!selectedIds.includes(item.id) && selectedIds.length >= 16)
              }
              onChange={() => toggleCharacteristic(item.id)}
            />
            <span>
              {item.name}
              {item.unit ? `, ${item.unit}` : ""}
            </span>
          </label>
        ))}
      </fieldset>

      <label className="grid gap-2">
        <span>Учебные названия — по одному на строку</span>
        <textarea
          value={namesText}
          disabled={locked}
          rows={6}
          spellCheck={false}
          onChange={(event) => {
            setNamesText(event.target.value);
            setValidationError(null);
            mutation.reset();
          }}
          className="w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-[var(--app-text)]"
        />
      </label>

      <p className="text-[var(--app-muted)]">
        Пробелы внутри и по краям названия сохраняются. Вставка названий не
        создаёт учебные примеры. Для каждого переменного поля нужны минимум два
        разных подтверждённых значения.
      </p>

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked || numericCharacteristics.length < 2}
        onClick={preview}
      >
        {mutation.isPending
          ? "Строим и проверяем..."
          : "Построить составные предложения"}
      </AppButton>

      {validationError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {validationError}
        </p>
      )}

      {mutation.isPending && (
        <p role="status">Загружаем подтверждения и проверяем структуры...</p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось построить составные предложения.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p>Версия генератора: {result.generatorVersion}</p>

          {result.issues.map((issue, index) => (
            <div key={`${issue.code}-${index}`} className="grid gap-1">
              <p className="text-[var(--app-danger)]">{issue.message}</p>
              <p className="text-[var(--app-muted)]">Код: {issue.code}</p>
              {issue.exampleIds.length > 0 && (
                <details>
                  <summary>Связанные подтверждения</summary>
                  <p className="break-all">{issue.exampleIds.join(", ")}</p>
                </details>
              )}
            </div>
          ))}

          {result.proposals.length === 0 && (
            <p>
              Составные предложения не сформированы. Проверьте диагностику и
              полноту подтверждённой разметки.
            </p>
          )}

          {result.proposals.map((proposal) => (
            <article
              key={JSON.stringify(proposal.pattern)}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="whitespace-pre-wrap break-all font-mono">
                {proposal.pattern.parts.map((part, index) => (
                  <span key={index}>
                    {part.characteristicDefinitionId
                      ? `{${characteristicName(part.characteristicDefinitionId)}}`
                      : part.literal}
                  </span>
                ))}
              </p>

              <p className="font-semibold">
                {proposal.passedExamples
                  ? "Прошло проверку на выбранных учебных примерах"
                  : "Не прошло проверку — требуется дополнительная разметка или разбор конфликтов"}
              </p>

              <p>Названий с совпадением: {proposal.matchedNameCount}</p>
              <p>
                Полностью поддерживающих названий:{" "}
                {proposal.supportingNameCount}
              </p>

              {proposal.fields.map((coverage) => (
                <p key={coverage.characteristicDefinitionId}>
                  {characteristicName(coverage.characteristicDefinitionId)}:{" "}
                  разных подтверждённых значений — {coverage.distinctValueCount}
                  {coverage.distinctValueCount < 2 ? " (нужно минимум 2)" : ""}
                </p>
              ))}

              <p>
                Поддерживающих подтверждений:{" "}
                {proposal.supportingExampleIds.length}
              </p>
              <p>
                Конфликтующих подтверждений:{" "}
                {proposal.conflictingExampleIds.length}
              </p>

              {proposal.passedExamples && mutation.variables && (
                <CatalogRecognitionMultiIntegerDraftSave
                  key={JSON.stringify([
                    "multi-integer-save",
                    batchId,
                    mutation.variables,
                    result.generatorVersion,
                    proposal.pattern,
                    proposal.supportingExampleIds,
                  ])}
                  trainingRequest={mutation.variables}
                  generatorVersion={result.generatorVersion}
                  proposal={proposal}
                  disabled={disabled || mutation.isPending}
                />
              )}

              {batchId && proposal.passedExamples && mutation.variables && (
                <CatalogRecognitionMultiIntegerBatchPreviewPanel
                  key={JSON.stringify([
                    "multi-integer-batch",
                    batchId,
                    mutation.variables,
                    result.generatorVersion,
                    proposal.pattern,
                  ])}
                  batchId={batchId}
                  trainingRequest={mutation.variables}
                  generatorVersion={result.generatorVersion}
                  proposal={proposal}
                  characteristics={characteristics}
                  disabled={disabled || mutation.isPending}
                />
              )}

              {proposal.conflictingExampleIds.length > 0 && (
                <details>
                  <summary>Конфликтующие подтверждения</summary>
                  <p className="break-all">
                    {proposal.conflictingExampleIds.join(", ")}
                  </p>
                </details>
              )}
            </article>
          ))}

          <p className="text-[var(--app-muted)]">
            Предложения построены по выбранной учебной подборке. Прошедший
            проверку шаблон можно отдельно сохранить как черновик.
            Автоматической активации нет. Проверка на учебных примерах не
            доказывает правильность на новых товарах.
          </p>
        </div>
      )}

      <CatalogRecognitionMultiIntegerDraftList
        key={JSON.stringify([
          "multi-integer-draft-list",
          manufacturerId,
          productTypeId,
        ])}
        manufacturerId={manufacturerId}
        productTypeId={productTypeId}
        characteristics={characteristics}
        disabled={disabled}
      />

      {!hideVersionManagement && <>
      <CatalogRecognitionRuleSetVersionCreate
        key={JSON.stringify([
          "create-recognition-rule-set-version",
          manufacturerId,
          productTypeId,
        ])}
        manufacturerId={manufacturerId}
        productTypeId={productTypeId}
        characteristics={characteristics}
        disabled={disabled}
      />

      <CatalogRecognitionRuleSetVersionList
        key={JSON.stringify([
          "recognition-rule-set-versions",
          batchId,
          manufacturerId,
          productTypeId,
        ])}
        batchId={batchId}
        manufacturerId={manufacturerId}
        productTypeId={productTypeId}
        disabled={disabled}
      />
      </>}
    </section>
  );
}
