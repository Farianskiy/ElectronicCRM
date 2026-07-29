"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type {
  CatalogProductTypeCharacteristicMetadata,
  CatalogProductTypeMetadata,
} from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { analyzeCatalogImportBatch } from "../api/analyzeCatalogImportBatch";
import { getCatalogImportMapping } from "../api/getCatalogImportMapping";
import { updateCatalogImportMapping } from "../api/updateCatalogImportMapping";
import { getCatalogImportColumnTargetLabel } from "../model/catalogImportColumnTarget";
import { getCatalogImportStatusLabel } from "../model/catalogImportStatus";
import {
  catalogImportColumnTargetKinds,
  type AnalyzeCatalogImportBatchResponse,
  type CatalogImportColumnTargetKind,
  type CatalogImportMappingColumn,
  type GetCatalogImportMappingResponse,
  type UpdateCatalogImportMappingResponse,
} from "../model/types";

interface CatalogImportMappingEditorProps {
  batchId: string;
}

interface SaveMappingResult {
  mapping: UpdateCatalogImportMappingResponse;
  analysis: AnalyzeCatalogImportBatchResponse;
}

const standardTargetKinds: readonly CatalogImportColumnTargetKind[] = [
  "Name",
  "Article",
  "Manufacturer",
  "Price",
  "StockQuantity",
];

const requiredStandardTargetKinds: readonly CatalogImportColumnTargetKind[] = [
  "Name",
  "Article",
  "Manufacturer",
];

function formatConfidence(confidence: number): string {
  if (!Number.isFinite(confidence)) {
    return "—";
  }

  return `${Math.round(confidence * 100)}%`;
}

function formatCharacteristicDataType(dataType: string): string {
  switch (dataType) {
    case "Text":
      return "Текст";

    case "Number":
      return "Число";

    case "Boolean":
      return "Да / Нет";

    default:
      return dataType;
  }
}

function getCharacteristicLabel(
  characteristic: CatalogProductTypeCharacteristicMetadata,
): string {
  const unit = characteristic.unit ? `, ${characteristic.unit}` : "";

  const required = characteristic.isRequired ? " · обязательная" : "";

  return `${characteristic.name}${unit} · ${formatCharacteristicDataType(
    characteristic.dataType,
  )}${required}`;
}

function validateMapping(
  productTypeId: string,
  columns: CatalogImportMappingColumn[],
  characteristics: CatalogProductTypeCharacteristicMetadata[],
): string[] {
  const errors: string[] = [];

  if (!productTypeId) {
    errors.push("Выберите тип товара.");
  }

  const unmappedColumns = columns.filter(
    (column) => column.targetKind === "Unmapped",
  );

  if (unmappedColumns.length > 0) {
    const headers = unmappedColumns
      .slice(0, 5)
      .map((column) => `«${column.sourceHeader}»`)
      .join(", ");

    const suffix =
      unmappedColumns.length > 5 ? ` и ещё ${unmappedColumns.length - 5}` : "";

    errors.push(
      `Для всех колонок нужно выбрать назначение или Ignore. Не сопоставлены: ${headers}${suffix}.`,
    );
  }

  for (const requiredTarget of requiredStandardTargetKinds) {
    const mappedCount = columns.filter(
      (column) => column.targetKind === requiredTarget,
    ).length;

    if (mappedCount === 0) {
      errors.push(
        `Не назначена колонка «${getCatalogImportColumnTargetLabel(
          requiredTarget,
        )}».`,
      );
    }
  }

  for (const standardTarget of standardTargetKinds) {
    const mappedCount = columns.filter(
      (column) => column.targetKind === standardTarget,
    ).length;

    if (mappedCount > 1) {
      errors.push(
        `Назначение «${getCatalogImportColumnTargetLabel(
          standardTarget,
        )}» выбрано более одного раза.`,
      );
    }
  }

  const characteristicColumns = columns.filter(
    (column) => column.targetKind === "Characteristic",
  );

  const incompleteCharacteristicColumns = characteristicColumns.filter(
    (column) => !column.characteristicDefinitionId,
  );

  if (incompleteCharacteristicColumns.length > 0) {
    errors.push(
      "Для каждой колонки с назначением «Характеристика» нужно выбрать конкретную характеристику.",
    );
  }

  const characteristicCounts = new Map<string, number>();

  for (const column of characteristicColumns) {
    const definitionId = column.characteristicDefinitionId;

    if (!definitionId) {
      continue;
    }

    characteristicCounts.set(
      definitionId,
      (characteristicCounts.get(definitionId) ?? 0) + 1,
    );
  }

  for (const [definitionId, count] of characteristicCounts) {
    if (count <= 1) {
      continue;
    }

    const characteristic = characteristics.find(
      (item) => item.id === definitionId,
    );

    errors.push(
      `Характеристика «${
        characteristic?.name ?? definitionId
      }» назначена более чем одной Excel-колонке.`,
    );
  }

  const requiredCharacteristics = characteristics.filter(
    (characteristic) => characteristic.isRequired,
  );

  for (const characteristic of requiredCharacteristics) {
    const isMapped = characteristicColumns.some(
      (column) => column.characteristicDefinitionId === characteristic.id,
    );

    if (!isMapped) {
      errors.push(
        `Не сопоставлена обязательная характеристика «${characteristic.name}».`,
      );
    }
  }

  return errors;
}

export function CatalogImportMappingEditor({
  batchId,
}: CatalogImportMappingEditorProps) {
  const mappingQuery = useQuery({
    queryKey: ["catalog-import-batches", "mapping", batchId],
    queryFn: () => getCatalogImportMapping(batchId),
    enabled: batchId.length > 0,
  });

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const queryError = mappingQuery.error ?? productTypesQuery.error;

  if (mappingQuery.isLoading || productTypesQuery.isLoading) {
    return (
      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
        Загружаем сопоставление колонок...
      </section>
    );
  }

  if (queryError) {
    return (
      <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
        {getApiErrorMessage(
          queryError,
          "Не удалось загрузить сопоставление колонок.",
        )}
      </section>
    );
  }

  const mapping = mappingQuery.data;
  const productTypes = productTypesQuery.data ?? [];

  if (!mapping) {
    return null;
  }

  return (
    <CatalogImportMappingForm
      key={`${mapping.version}-${mapping.productTypeId ?? "none"}`}
      batchId={batchId}
      initialMapping={mapping}
      productTypes={productTypes}
    />
  );
}

function CatalogImportMappingForm({
  batchId,
  initialMapping,
  productTypes,
}: {
  batchId: string;
  initialMapping: GetCatalogImportMappingResponse;
  productTypes: CatalogProductTypeMetadata[];
}) {
  const queryClient = useQueryClient();

  const [selectedProductTypeId, setSelectedProductTypeId] = useState(
    initialMapping.productTypeId ?? "",
  );

  const [columns, setColumns] = useState<CatalogImportMappingColumn[]>(
    initialMapping.columns.map((column) => ({
      ...column,
    })),
  );

  const [formErrors, setFormErrors] = useState<string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const selectedProductType = productTypes.find(
    (productType) => productType.id === selectedProductTypeId,
  );

  const characteristicsQuery = useQuery({
    queryKey: [
      "catalog-product-type-characteristics",
      selectedProductType?.code ?? "",
    ],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(selectedProductType?.code ?? ""),
    enabled: Boolean(selectedProductType?.code),
    staleTime: 5 * 60 * 1000,
  });

  const characteristics = characteristicsQuery.data ?? [];

  const saveMutation = useMutation({
    mutationFn: async (): Promise<SaveMappingResult> => {
      const mapping = await updateCatalogImportMapping(batchId, {
        productTypeId: selectedProductTypeId,
        columns: columns.map((column) => ({
          columnId: column.columnId,
          targetKind: column.targetKind,
          characteristicDefinitionId:
            column.targetKind === "Characteristic"
              ? (column.characteristicDefinitionId ?? null)
              : null,
        })),
      });

      const analysis = await analyzeCatalogImportBatch(batchId);

      return {
        mapping,
        analysis,
      };
    },

    onSuccess: async ({ analysis }) => {
      setFormErrors([]);

      setColumns((currentColumns) =>
        currentColumns.map((column) => ({
          ...column,
          confidence: column.targetKind === "Unmapped" ? 0 : 1,
          isConfirmed: column.targetKind !== "Unmapped",
        })),
      );

      setSuccessMessage(
        `Сопоставление сохранено. Повторный анализ завершён со статусом «${getCatalogImportStatusLabel(
          analysis.status,
        )}».`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "details", batchId],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "rows", batchId],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-import-batches", "my"],
        }),
      ]);
    },

    onError: () => {
      setSuccessMessage(null);
    },
  });

  const unmappedCount = columns.filter(
    (column) => column.targetKind === "Unmapped",
  ).length;

  const ignoredCount = columns.filter(
    (column) => column.targetKind === "Ignore",
  ).length;

  const characteristicCount = columns.filter(
    (column) => column.targetKind === "Characteristic",
  ).length;

  function handleProductTypeChange(productTypeId: string): void {
    if (productTypeId === selectedProductTypeId) {
      return;
    }

    setSelectedProductTypeId(productTypeId);
    setFormErrors([]);
    setSuccessMessage(null);
    saveMutation.reset();

    /*
     * Назначения характеристик относятся к конкретному
     * типу товара. При смене типа их безопаснее сбросить.
     */
    setColumns((currentColumns) =>
      currentColumns.map((column) =>
        column.targetKind === "Characteristic"
          ? {
              ...column,
              targetKind: "Unmapped",
              characteristicDefinitionId: null,
              confidence: 0,
              isConfirmed: false,
            }
          : column,
      ),
    );
  }

  function handleTargetChange(
    columnId: string,
    targetKind: CatalogImportColumnTargetKind,
  ): void {
    setFormErrors([]);
    setSuccessMessage(null);
    saveMutation.reset();

    setColumns((currentColumns) =>
      currentColumns.map((column) =>
        column.columnId === columnId
          ? {
              ...column,
              targetKind,
              characteristicDefinitionId: null,
              confidence: targetKind === "Unmapped" ? 0 : 1,
              isConfirmed: targetKind !== "Unmapped",
            }
          : column,
      ),
    );
  }

  function handleCharacteristicChange(
    columnId: string,
    characteristicDefinitionId: string,
  ): void {
    setFormErrors([]);
    setSuccessMessage(null);
    saveMutation.reset();

    setColumns((currentColumns) =>
      currentColumns.map((column) =>
        column.columnId === columnId
          ? {
              ...column,
              characteristicDefinitionId: characteristicDefinitionId || null,
              confidence: characteristicDefinitionId ? 1 : 0,
              isConfirmed: Boolean(characteristicDefinitionId),
            }
          : column,
      ),
    );
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (characteristicsQuery.isError) {
      setFormErrors([
        "Не удалось загрузить характеристики выбранного типа товара.",
      ]);

      return;
    }

    const validationErrors = validateMapping(
      selectedProductTypeId,
      columns,
      characteristics,
    );

    if (validationErrors.length > 0) {
      setFormErrors(validationErrors);
      setSuccessMessage(null);

      return;
    }

    setFormErrors([]);
    setSuccessMessage(null);
    saveMutation.mutate();
  }

  const isBusy = saveMutation.isPending || characteristicsQuery.isFetching;

  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h2 className="text-xl font-semibold text-white">
            Сопоставление колонок
          </h2>

          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
            Выберите тип товара и укажите назначение каждой колонки Excel.
            Лишние колонки нужно явно отметить как игнорируемые.
          </p>
        </div>

        {!initialMapping.canEdit && (
          <span className="rounded-full border border-slate-500/30 bg-slate-500/10 px-3 py-1 text-xs font-medium text-slate-300">
            Только просмотр
          </span>
        )}
      </div>

      <form onSubmit={handleSubmit} className="mt-6 grid gap-6">
        <div className="grid gap-2">
          <label className="text-sm font-medium text-slate-300">
            Тип товара
          </label>

          <AppSelect
            ariaLabel="Тип товара для импорта"
            value={selectedProductTypeId}
            disabled={
              !initialMapping.canEdit ||
              saveMutation.isPending ||
              productTypes.length === 0
            }
            onChange={handleProductTypeChange}
            options={[
              {
                value: "",
                label: "Выберите тип товара",
              },
              ...productTypes.map((productType) => ({
                value: productType.id,
                label: `${productType.name} · ${productType.code}`,
              })),
            ]}
          />

          {selectedProductType && (
            <p className="text-xs text-slate-500">
              Код типа: {selectedProductType.code}
            </p>
          )}
        </div>

        {characteristicsQuery.isLoading && (
          <div className="rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-slate-300">
            Загружаем характеристики выбранного типа...
          </div>
        )}

        {characteristicsQuery.isError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {getApiErrorMessage(
              characteristicsQuery.error,
              "Не удалось загрузить характеристики выбранного типа.",
            )}
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <MappingSummaryCard
            label="Колонок Excel"
            value={columns.length.toString()}
          />

          <MappingSummaryCard
            label="Не сопоставлено"
            value={unmappedCount.toString()}
            warning={unmappedCount > 0}
          />

          <MappingSummaryCard
            label="Игнорируется"
            value={ignoredCount.toString()}
          />

          <MappingSummaryCard
            label="Характеристик"
            value={characteristicCount.toString()}
          />
        </div>

        <div className="rounded-2xl border border-teal-500/20 bg-teal-500/[0.06] p-4">
          <p className="text-sm font-medium text-teal-100">
            Обязательные системные поля
          </p>

          <p className="mt-2 text-sm leading-6 text-slate-300">
            Наименование, артикул и производитель должны быть назначены ровно по
            одному разу. Цена и остаток необязательны.
          </p>
        </div>

        <div className="overflow-x-auto rounded-2xl border border-white/10">
          <table className="w-full min-w-[1150px] border-collapse text-left text-sm">
            <thead className="bg-black/30 text-slate-400">
              <tr>
                <th className="px-4 py-3 font-medium">№</th>

                <th className="px-4 py-3 font-medium">Заголовок Excel</th>

                <th className="px-4 py-3 font-medium">Автораспознавание</th>

                <th className="px-4 py-3 font-medium">Назначение</th>

                <th className="px-4 py-3 font-medium">Характеристика</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-white/10">
              {columns.map((column) => {
                const usedStandardTargets = new Set(
                  columns
                    .filter(
                      (otherColumn) => otherColumn.columnId !== column.columnId,
                    )
                    .map((otherColumn) => otherColumn.targetKind),
                );

                const usedCharacteristicIds = new Set(
                  columns
                    .filter(
                      (otherColumn) => otherColumn.columnId !== column.columnId,
                    )
                    .map(
                      (otherColumn) => otherColumn.characteristicDefinitionId,
                    )
                    .filter((definitionId): definitionId is string =>
                      Boolean(definitionId),
                    ),
                );

                return (
                  <tr
                    key={column.columnId}
                    className="bg-white/[0.01] align-top"
                  >
                    <td className="px-4 py-4 font-medium text-white">
                      {column.sourceColumnNumber}
                    </td>

                    <td className="max-w-72 px-4 py-4">
                      <p className="break-words font-medium text-white">
                        {column.sourceHeader}
                      </p>

                      <p className="mt-1 break-all text-xs text-slate-600">
                        {column.columnId}
                      </p>
                    </td>

                    <td className="px-4 py-4">
                      <p className="text-slate-300">
                        {formatConfidence(column.confidence)}
                      </p>

                      <p
                        className={[
                          "mt-1 text-xs",
                          column.isConfirmed
                            ? "text-green-300"
                            : "text-amber-300",
                        ].join(" ")}
                      >
                        {column.isConfirmed
                          ? "Подтверждено"
                          : "Требует проверки"}
                      </p>
                    </td>

                    <td className="w-72 px-4 py-4">
                      <AppSelect
                        ariaLabel={`Назначение колонки ${column.sourceHeader}`}
                        value={column.targetKind}
                        disabled={
                          !initialMapping.canEdit || saveMutation.isPending
                        }
                        onChange={(value) =>
                          handleTargetChange(
                            column.columnId,
                            value as CatalogImportColumnTargetKind,
                          )
                        }
                        options={catalogImportColumnTargetKinds.map(
                          (targetKind) => ({
                            value: targetKind,
                            label:
                              getCatalogImportColumnTargetLabel(targetKind),
                            disabled:
                              standardTargetKinds.includes(targetKind) &&
                              column.targetKind !== targetKind &&
                              usedStandardTargets.has(targetKind),
                          }),
                        )}
                      />
                    </td>

                    <td className="w-[420px] px-4 py-4">
                      {column.targetKind === "Characteristic" ? (
                        <AppSelect
                          ariaLabel={`Характеристика колонки ${column.sourceHeader}`}
                          value={column.characteristicDefinitionId ?? ""}
                          disabled={
                            !initialMapping.canEdit ||
                            saveMutation.isPending ||
                            characteristicsQuery.isFetching ||
                            !selectedProductTypeId
                          }
                          onChange={(value) =>
                            handleCharacteristicChange(column.columnId, value)
                          }
                          options={[
                            {
                              value: "",
                              label: "Выберите характеристику",
                            },
                            ...characteristics.map((characteristic) => ({
                              value: characteristic.id,
                              label: getCharacteristicLabel(characteristic),
                              disabled: usedCharacteristicIds.has(
                                characteristic.id,
                              ),
                            })),
                          ]}
                        />
                      ) : (
                        <span className="text-slate-600">Не требуется</span>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {formErrors.length > 0 && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5">
            <h3 className="font-semibold text-red-100">
              Сопоставление не завершено
            </h3>

            <ul className="mt-3 grid gap-2 text-sm text-red-200">
              {formErrors.map((error) => (
                <li key={error}>• {error}</li>
              ))}
            </ul>
          </div>
        )}

        {saveMutation.isError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
            {getApiErrorMessage(
              saveMutation.error,
              "Не удалось сохранить сопоставление и повторно проанализировать пакет.",
            )}
          </div>
        )}

        {successMessage && (
          <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-5 text-sm text-green-200">
            {successMessage}
          </div>
        )}

        <div className="flex justify-end">
          <button
            type="submit"
            disabled={
              !initialMapping.canEdit || !selectedProductTypeId || isBusy
            }
            className="rounded-2xl bg-teal-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {saveMutation.isPending
              ? "Сохраняем и анализируем..."
              : "Сохранить и повторно проанализировать"}
          </button>
        </div>
      </form>
    </section>
  );
}

function MappingSummaryCard({
  label,
  value,
  warning = false,
}: {
  label: string;
  value: string;
  warning?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p
        className={[
          "mt-2 text-2xl font-semibold",
          warning ? "text-amber-300" : "text-white",
        ].join(" ")}
      >
        {value}
      </p>
    </div>
  );
}
