"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState, type FormEvent } from "react";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type {
  CatalogProductTypeCharacteristicMetadata,
  CatalogProductTypeMetadata,
} from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { analyzeCatalogImportBatch } from "../../api/analyzeCatalogImportBatch";
import { getCatalogImportMapping } from "../../api/getCatalogImportMapping";
import { updateCatalogImportMapping } from "../../api/updateCatalogImportMapping";
import { getCatalogImportColumnTargetLabel } from "../../model/catalogImportColumnTarget";
import { getCatalogImportStatusLabel } from "../../model/catalogImportStatus";
import {
  catalogImportColumnTargetKinds,
  type AnalyzeCatalogImportBatchResponse,
  type CatalogImportColumnTargetKind,
  type CatalogImportMappingColumn,
  type GetCatalogImportMappingResponse,
  type UpdateCatalogImportMappingResponse,
} from "../../model/types";
import { validateCatalogImportMapping } from "../../model/mappingValidation";
import { catalogImportQueryKeys } from "../../model/queryKeys";

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

export function CatalogImportMappingEditor({
  batchId,
}: CatalogImportMappingEditorProps) {
  const mappingQuery = useQuery({
    queryKey: catalogImportQueryKeys.mapping(batchId),
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

  const mappingValidation = useMemo(
    () =>
      validateCatalogImportMapping(
        selectedProductTypeId,
        columns,
        characteristics,
      ),
    [selectedProductTypeId, columns, characteristics],
  );

  const mappingMetrics = mappingValidation.metrics;

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
        currentColumns.map((column) => {
          const hasCompleteAssignment =
            column.targetKind !== "Unmapped" &&
            (column.targetKind !== "Characteristic" ||
              Boolean(column.characteristicDefinitionId));

          return {
            ...column,
            confidence: hasCompleteAssignment ? 1 : column.confidence,
            isConfirmed: hasCompleteAssignment,
          };
        }),
      );

      setSuccessMessage(
        `Сопоставление сохранено. Повторный анализ завершён со статусом «${getCatalogImportStatusLabel(
          analysis.status,
        )}».`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.details(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.rowsRoot(batchId),
        }),

        queryClient.invalidateQueries({
          queryKey: catalogImportQueryKeys.myRoot,
        }),
      ]);
    },

    onError: () => {
      setSuccessMessage(null);
    },
  });

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
      currentColumns.map((column) => {
        if (column.columnId !== columnId) {
          return column;
        }

        const requiresCharacteristic = targetKind === "Characteristic";

        const isConfirmed =
          targetKind !== "Unmapped" && !requiresCharacteristic;

        return {
          ...column,
          targetKind,
          characteristicDefinitionId: null,
          confidence: targetKind === "Unmapped" ? 0 : column.confidence,
          isConfirmed,
        };
      }),
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

    if (!mappingValidation.metrics.isComplete) {
      setFormErrors(mappingValidation.errors);
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

        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <MappingSummaryCard
            label="Системные поля"
            value={[
              mappingMetrics.requiredSystemFieldsMapped,
              mappingMetrics.requiredSystemFieldsTotal,
            ].join(" из ")}
            warning={
              mappingMetrics.requiredSystemFieldsMapped !==
              mappingMetrics.requiredSystemFieldsTotal
            }
          />

          <MappingSummaryCard
            label="Обязательные характеристики"
            value={[
              mappingMetrics.requiredCharacteristicsMapped,
              mappingMetrics.requiredCharacteristicsTotal,
            ].join(" из ")}
            warning={
              mappingMetrics.requiredCharacteristicsMapped !==
              mappingMetrics.requiredCharacteristicsTotal
            }
          />

          <MappingSummaryCard
            label="Не сопоставлено"
            value={mappingMetrics.unmappedColumnsCount.toString()}
            warning={mappingMetrics.unmappedColumnsCount > 0}
          />

          <MappingSummaryCard
            label="Характеристика не выбрана"
            value={mappingMetrics.incompleteCharacteristicColumnsCount.toString()}
            warning={mappingMetrics.incompleteCharacteristicColumnsCount > 0}
          />

          <MappingSummaryCard
            label="Дубли назначений"
            value={mappingMetrics.duplicateAssignmentsCount.toString()}
            warning={mappingMetrics.duplicateAssignmentsCount > 0}
          />
        </div>

        <div
          className={[
            "rounded-2xl border p-5",
            mappingMetrics.isComplete
              ? "border-green-500/30 bg-green-500/10"
              : "border-amber-500/30 bg-amber-500/10",
          ].join(" ")}
        >
          <h3
            className={[
              "font-semibold",
              mappingMetrics.isComplete ? "text-green-100" : "text-amber-100",
            ].join(" ")}
          >
            {mappingMetrics.isComplete
              ? "Сопоставление готово"
              : "Сопоставление не завершено"}
          </h3>

          <p className="mt-2 text-sm leading-6 text-slate-300">
            {mappingMetrics.isComplete
              ? "Все обязательные поля и характеристики назначены. Можно сохранить mapping и повторно запустить анализ."
              : "Заполните обязательные системные поля, выберите характеристики и устраните дублирующиеся назначения."}
          </p>
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

                const currentColumnErrors =
                  mappingValidation.columnErrors[column.columnId] ?? [];

                const hasCompleteAssignment =
                  column.targetKind !== "Unmapped" &&
                  (column.targetKind !== "Characteristic" ||
                    Boolean(column.characteristicDefinitionId));

                const recognitionLabel =
                  column.confidence > 0
                    ? "Распознано автоматически"
                    : "Автораспознавание отсутствует";

                const readinessLabel =
                  currentColumnErrors.length > 0
                    ? currentColumnErrors[0]
                    : hasCompleteAssignment
                      ? "Сопоставление готово"
                      : "Требуется завершить сопоставление";

                return (
                  <tr
                    key={column.columnId}
                    className={[
                      "align-top transition",
                      currentColumnErrors.length > 0
                        ? "bg-red-500/[0.04]"
                        : "bg-white/[0.01]",
                    ].join(" ")}
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

                    <td className="w-64 px-4 py-4">
                      <p className="text-slate-200">
                        {formatConfidence(column.confidence)}
                      </p>

                      <p className="mt-1 text-xs text-slate-500">
                        {recognitionLabel}
                      </p>

                      <p
                        className={[
                          "mt-2 text-xs leading-5",
                          currentColumnErrors.length > 0
                            ? "text-red-300"
                            : hasCompleteAssignment
                              ? "text-green-300"
                              : "text-amber-300",
                        ].join(" ")}
                      >
                        {readinessLabel}
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
                      {currentColumnErrors.length > 0 && (
                        <div className="mt-2 grid gap-1">
                          {currentColumnErrors.map((error) => (
                            <p
                              key={error}
                              className="text-xs leading-5 text-red-300"
                            >
                              {error}
                            </p>
                          ))}
                        </div>
                      )}
                    </td>

                    <td className="w-[420px] px-4 py-4">
                      {column.targetKind === "Characteristic" ? (
                        <div>
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

                          {currentColumnErrors.length > 0 && (
                            <div className="mt-2 grid gap-1">
                              {currentColumnErrors.map((error) => (
                                <p
                                  key={error}
                                  className="text-xs leading-5 text-red-300"
                                >
                                  {error}
                                </p>
                              ))}
                            </div>
                          )}
                        </div>
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

        {!mappingMetrics.isComplete && mappingValidation.errors.length > 0 && (
          <div className="rounded-2xl border border-amber-500/30 bg-amber-500/10 p-5">
            <h3 className="font-semibold text-amber-100">
              Чтобы продолжить, исправьте:
            </h3>

            <ul className="mt-3 grid gap-2 text-sm leading-6 text-amber-100">
              {mappingValidation.errors.map((error) => (
                <li key={error}>• {error}</li>
              ))}
            </ul>
          </div>
        )}

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

        <div className="flex flex-col items-end gap-2">
          {!mappingMetrics.isComplete && (
            <p className="max-w-xl text-right text-xs leading-5 text-amber-300">
              Кнопка станет доступна после заполнения всех обязательных
              назначений.
            </p>
          )}

          <button
            type="submit"
            disabled={
              !initialMapping.canEdit ||
              !mappingMetrics.isComplete ||
              characteristicsQuery.isError ||
              isBusy
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
