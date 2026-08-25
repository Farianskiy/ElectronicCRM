"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type {
  CatalogCharacteristicDataType,
  CatalogProductTypeCharacteristicMetadata,
} from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppSelect } from "@/shared/ui/AppSelect";
import { bulkUpdateCatalogImportRows } from "../../api/bulkUpdateCatalogImportRows";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  BulkUpdateCatalogImportRowRequest,
  BulkUpdateCatalogImportRowsRequest,
  BulkUpdateCatalogImportRowsResponse,
  CatalogImportRow,
} from "../../model/types";

interface CatalogImportBulkRowsEditorProps {
  batchId: string;
  productTypeId: string;
  expectedVersion: number;
  rows: CatalogImportRow[];
  onCancel: () => void;
  onSaved: (result: BulkUpdateCatalogImportRowsResponse) => void;
}

interface BulkCatalogImportRowDraft {
  rowId: string;
  rowNumber: number;
  name: string;
  article: string;
  manufacturerId: string;
  price: string;
  stockQuantity: string;
  characteristicValues: Record<string, string>;
}

interface ParsedNumberResult {
  value: number | null;
  error?: string;
}

const inputClassName = [
  "w-full rounded-2xl border border-white/10",
  "bg-black/30 px-4 py-3",
  "text-sm text-slate-100 outline-none transition",
  "placeholder:text-slate-600",
  "hover:border-white/20",
  "focus:border-teal-400",
  "focus:ring-2 focus:ring-teal-400/20",
  "disabled:cursor-not-allowed disabled:opacity-50",
].join(" ");

function createInitialDrafts(
  rows: CatalogImportRow[],
): Record<string, BulkCatalogImportRowDraft> {
  const drafts: Record<string, BulkCatalogImportRowDraft> = {};

  for (const row of rows) {
    drafts[row.rowId] = {
      rowId: row.rowId,
      rowNumber: row.rowNumber,
      name: row.data.name ?? "",
      article: row.data.article ?? "",
      manufacturerId: row.data.manufacturerId ?? "",
      price: row.data.price?.toString() ?? "",
      stockQuantity: row.data.stockQuantity?.toString() ?? "",
      characteristicValues: {
        ...row.data.characteristics,
      },
    };
  }

  return drafts;
}

function parseNullableDecimal(
  rawValue: string,
  fieldName: string,
): ParsedNumberResult {
  const normalizedValue = rawValue.trim().replace(",", ".");

  if (!normalizedValue) {
    return {
      value: null,
    };
  }

  const value = Number(normalizedValue);

  if (!Number.isFinite(value)) {
    return {
      value: null,
      error: `Поле «${fieldName}» должно содержать число.`,
    };
  }

  return {
    value,
  };
}

function parseNullableInteger(
  rawValue: string,
  fieldName: string,
): ParsedNumberResult {
  const normalizedValue = rawValue.trim();

  if (!normalizedValue) {
    return {
      value: null,
    };
  }

  const value = Number(normalizedValue);

  if (!Number.isSafeInteger(value)) {
    return {
      value: null,
      error: `Поле «${fieldName}» должно содержать целое число.`,
    };
  }

  return {
    value,
  };
}

function normalizeCharacteristicValue(
  value: string,
  dataType: CatalogCharacteristicDataType,
): string {
  const trimmedValue = value.trim();

  if (dataType === "Number") {
    return trimmedValue.replace(",", ".");
  }

  return trimmedValue;
}

function getCharacteristicLabel(
  characteristic: CatalogProductTypeCharacteristicMetadata,
): string {
  const unit = characteristic.unit ? `, ${characteristic.unit}` : "";

  return characteristic.isRequired
    ? `${characteristic.name}${unit} · обязательная`
    : `${characteristic.name}${unit}`;
}

export function CatalogImportBulkRowsEditor({
  batchId,
  productTypeId,
  expectedVersion,
  rows,
  onCancel,
  onSaved,
}: CatalogImportBulkRowsEditorProps) {
  const queryClient = useQueryClient();

  const [drafts, setDrafts] = useState<
    Record<string, BulkCatalogImportRowDraft>
  >(() => createInitialDrafts(rows));

  const [rowErrors, setRowErrors] = useState<Record<string, string[]>>({});
  const [generalError, setGeneralError] = useState<string | null>(null);

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    staleTime: 5 * 60 * 1000,
  });

  const selectedProductType = productTypesQuery.data?.find(
    (productType) => productType.id === productTypeId,
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

  const manufacturers = manufacturersQuery.data ?? [];
  const characteristics = characteristicsQuery.data ?? [];

  const metadataError =
    productTypesQuery.error ??
    manufacturersQuery.error ??
    characteristicsQuery.error;

  const saveMutation = useMutation({
    mutationFn: (request: BulkUpdateCatalogImportRowsRequest) =>
      bulkUpdateCatalogImportRows(batchId, request),

    onSuccess: async (result) => {
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

      onSaved(result);
    },
  });

  const isMetadataLoading =
    productTypesQuery.isLoading ||
    manufacturersQuery.isLoading ||
    characteristicsQuery.isLoading;

  const isBusy = isMetadataLoading || saveMutation.isPending;

  function clearRowErrors(rowId: string): void {
    setRowErrors((currentErrors) => {
      if (!currentErrors[rowId]) {
        return currentErrors;
      }

      const nextErrors = {
        ...currentErrors,
      };

      delete nextErrors[rowId];

      return nextErrors;
    });
  }

  function updateDraft(
    rowId: string,
    update: (
      currentDraft: BulkCatalogImportRowDraft,
    ) => BulkCatalogImportRowDraft,
  ): void {
    setDrafts((currentDrafts) => {
      const currentDraft = currentDrafts[rowId];

      if (!currentDraft) {
        return currentDrafts;
      }

      return {
        ...currentDrafts,
        [rowId]: update(currentDraft),
      };
    });

    clearRowErrors(rowId);
    setGeneralError(null);
    saveMutation.reset();
  }

  function handleCharacteristicChange(
    rowId: string,
    characteristicId: string,
    value: string,
  ): void {
    updateDraft(rowId, (currentDraft) => ({
      ...currentDraft,
      characteristicValues: {
        ...currentDraft.characteristicValues,
        [characteristicId]: value,
      },
    }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const nextRowErrors: Record<string, string[]> = {};
    const requestRows: BulkUpdateCatalogImportRowRequest[] = [];

    for (const row of rows) {
      const draft = drafts[row.rowId];

      if (!draft) {
        nextRowErrors[row.rowId] = [
          "Не удалось найти данные строки в массовом редакторе.",
        ];

        continue;
      }

      const errors: string[] = [];

      const parsedPrice = parseNullableDecimal(draft.price, "Цена");

      if (parsedPrice.error) {
        errors.push(parsedPrice.error);
      }

      const parsedStockQuantity = parseNullableInteger(
        draft.stockQuantity,
        "Остаток",
      );

      if (parsedStockQuantity.error) {
        errors.push(parsedStockQuantity.error);
      }

      const requestCharacteristics: Record<string, string> = {};

      for (const characteristic of characteristics) {
        const rawValue = draft.characteristicValues[characteristic.id] ?? "";

        const normalizedValue = normalizeCharacteristicValue(
          rawValue,
          characteristic.dataType,
        );

        if (!normalizedValue) {
          continue;
        }

        requestCharacteristics[characteristic.id] = normalizedValue;
      }

      if (errors.length > 0) {
        nextRowErrors[row.rowId] = errors;

        continue;
      }

      requestRows.push({
        rowId: row.rowId,
        name: draft.name.trim() || null,
        article: draft.article.trim() || null,
        manufacturerId: draft.manufacturerId || null,
        price: parsedPrice.value,
        stockQuantity: parsedStockQuantity.value,
        characteristics: requestCharacteristics,
      });
    }

    if (Object.keys(nextRowErrors).length > 0) {
      setRowErrors(nextRowErrors);
      setGeneralError(
        "Некоторые строки содержат некорректные значения. Исправьте отмеченные строки перед сохранением.",
      );

      return;
    }

    if (requestRows.length === 0) {
      setGeneralError("Нет строк для сохранения.");

      return;
    }

    setRowErrors({});
    setGeneralError(null);

    saveMutation.mutate({
      expectedVersion,
      rows: requestRows,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mt-5 grid gap-5 rounded-3xl border border-teal-500/30 bg-teal-500/[0.05] p-5"
    >
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-xl font-semibold text-white">
            Массовое редактирование строк
          </h3>

          <p className="mt-2 text-sm leading-6 text-slate-400">
            Выбрано строк:{" "}
            <span className="font-semibold text-teal-200">{rows.length}</span>.
            Все изменения будут отправлены одним запросом и проверены backend.
          </p>

          <p className="mt-1 text-xs text-slate-500">
            Версия пакета: {expectedVersion}
          </p>
        </div>

        <button
          type="button"
          disabled={saveMutation.isPending}
          onClick={onCancel}
          className="rounded-xl border border-white/10 bg-white/[0.05] px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
        >
          Закрыть редактор
        </button>
      </div>

      {metadataError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getApiErrorMessage(
            metadataError,
            "Не удалось загрузить справочники для массового редактирования.",
          )}
        </div>
      )}

      {isMetadataLoading && (
        <div className="rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-slate-300">
          Загружаем производителей и характеристики выбранного типа товара...
        </div>
      )}

      <div className="grid gap-5">
        {rows.map((row) => {
          const draft = drafts[row.rowId];

          if (!draft) {
            return null;
          }

          const errors = rowErrors[row.rowId] ?? [];

          return (
            <section
              key={row.rowId}
              className="rounded-2xl border border-white/10 bg-black/20 p-5"
            >
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                <div>
                  <h4 className="font-semibold text-white">
                    Строка {row.rowNumber}
                  </h4>

                  <p className="mt-1 break-all text-xs text-slate-500">
                    {row.rowId}
                  </p>
                </div>

                <span
                  className={[
                    "w-fit rounded-full border px-3 py-1 text-xs font-medium",
                    row.status === "Error"
                      ? "border-red-500/30 bg-red-500/10 text-red-200"
                      : row.status === "Valid"
                        ? "border-green-500/30 bg-green-500/10 text-green-200"
                        : "border-amber-500/30 bg-amber-500/10 text-amber-200",
                  ].join(" ")}
                >
                  Текущий статус: {row.status}
                </span>
              </div>

              <div className="mt-5 grid gap-5 xl:grid-cols-2">
                <label className="grid gap-2">
                  <span className="text-sm font-medium text-slate-300">
                    Наименование
                  </span>

                  <textarea
                    value={draft.name}
                    disabled={isBusy}
                    rows={2}
                    onChange={(event) => {
                      const value = event.target.value;

                      updateDraft(row.rowId, (currentDraft) => ({
                        ...currentDraft,
                        name: value,
                      }));
                    }}
                    className={inputClassName}
                    placeholder="Наименование товара"
                  />
                </label>

                <label className="grid content-start gap-2">
                  <span className="text-sm font-medium text-slate-300">
                    Артикул
                  </span>

                  <input
                    type="text"
                    value={draft.article}
                    disabled={isBusy}
                    onChange={(event) => {
                      const value = event.target.value;

                      updateDraft(row.rowId, (currentDraft) => ({
                        ...currentDraft,
                        article: value,
                      }));
                    }}
                    className={inputClassName}
                    placeholder="Артикул товара"
                  />
                </label>

                <div className="grid content-start gap-2">
                  <span className="text-sm font-medium text-slate-300">
                    Производитель
                  </span>

                  <AppSelect
                    ariaLabel={`Производитель строки ${row.rowNumber}`}
                    value={draft.manufacturerId}
                    disabled={isBusy}
                    onChange={(value) => {
                      updateDraft(row.rowId, (currentDraft) => ({
                        ...currentDraft,
                        manufacturerId: value,
                      }));
                    }}
                    options={[
                      {
                        value: "",
                        label: "Производитель не выбран",
                      },
                      ...manufacturers.map((manufacturer) => ({
                        value: manufacturer.id,
                        label: manufacturer.name,
                      })),
                    ]}
                  />

                  {row.data.manufacturer && (
                    <p className="text-xs text-slate-500">
                      Значение после анализа: {row.data.manufacturer}
                    </p>
                  )}
                </div>

                <div className="grid gap-5 sm:grid-cols-2">
                  <label className="grid content-start gap-2">
                    <span className="text-sm font-medium text-slate-300">
                      Цена
                    </span>

                    <input
                      type="text"
                      inputMode="decimal"
                      value={draft.price}
                      disabled={isBusy}
                      onChange={(event) => {
                        const value = event.target.value;

                        updateDraft(row.rowId, (currentDraft) => ({
                          ...currentDraft,
                          price: value,
                        }));
                      }}
                      className={inputClassName}
                      placeholder="0.00"
                    />
                  </label>

                  <label className="grid content-start gap-2">
                    <span className="text-sm font-medium text-slate-300">
                      Остаток
                    </span>

                    <input
                      type="text"
                      inputMode="numeric"
                      value={draft.stockQuantity}
                      disabled={isBusy}
                      onChange={(event) => {
                        const value = event.target.value;

                        updateDraft(row.rowId, (currentDraft) => ({
                          ...currentDraft,
                          stockQuantity: value,
                        }));
                      }}
                      className={inputClassName}
                      placeholder="0"
                    />
                  </label>
                </div>
              </div>

              <div className="mt-5 rounded-2xl border border-white/10 bg-white/[0.025] p-4">
                <h5 className="font-semibold text-white">
                  Характеристики товара
                </h5>

                {characteristics.length === 0 ? (
                  <p className="mt-3 text-sm text-slate-500">
                    Для выбранного типа характеристики не определены.
                  </p>
                ) : (
                  <div className="mt-4 grid gap-4 xl:grid-cols-2">
                    {characteristics.map((characteristic) => (
                      <BulkCharacteristicInput
                        key={characteristic.id}
                        characteristic={characteristic}
                        rowNumber={row.rowNumber}
                        value={
                          draft.characteristicValues[characteristic.id] ?? ""
                        }
                        disabled={isBusy}
                        onChange={(value) =>
                          handleCharacteristicChange(
                            row.rowId,
                            characteristic.id,
                            value,
                          )
                        }
                      />
                    ))}
                  </div>
                )}
              </div>

              {errors.length > 0 && (
                <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
                  <h5 className="font-semibold text-red-100">
                    Ошибки строки {row.rowNumber}
                  </h5>

                  <ul className="mt-3 grid gap-2 text-sm text-red-200">
                    {errors.map((error) => (
                      <li key={error}>• {error}</li>
                    ))}
                  </ul>
                </div>
              )}
            </section>
          );
        })}
      </div>

      {generalError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          {generalError}
        </div>
      )}

      {saveMutation.isError && (
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          {getApiErrorMessage(
            saveMutation.error,
            "Не удалось массово сохранить строки импорта.",
          )}
        </div>
      )}

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        <button
          type="button"
          disabled={saveMutation.isPending}
          onClick={onCancel}
          className="rounded-2xl border border-white/10 bg-white/[0.05] px-5 py-3 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-50"
        >
          Отмена
        </button>

        <button
          type="submit"
          disabled={isBusy || Boolean(metadataError)}
          className="rounded-2xl bg-teal-500 px-6 py-3 text-sm font-semibold text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {saveMutation.isPending
            ? "Сохраняем и проверяем строки..."
            : `Сохранить строки: ${rows.length}`}
        </button>
      </div>
    </form>
  );
}

function BulkCharacteristicInput({
  characteristic,
  rowNumber,
  value,
  disabled,
  onChange,
}: {
  characteristic: CatalogProductTypeCharacteristicMetadata;
  rowNumber: number;
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  const label = getCharacteristicLabel(characteristic);

  if (characteristic.dataType === "Boolean") {
    return (
      <div className="grid content-start gap-2">
        <span className="text-sm font-medium text-slate-300">{label}</span>

        <AppSelect
          ariaLabel={`${label}, строка ${rowNumber}`}
          value={value}
          disabled={disabled}
          onChange={onChange}
          options={[
            {
              value: "",
              label: "Не указано",
            },
            {
              value: "true",
              label: "Да",
            },
            {
              value: "false",
              label: "Нет",
            },
          ]}
        />
      </div>
    );
  }

  return (
    <label className="grid content-start gap-2">
      <span className="text-sm font-medium text-slate-300">{label}</span>

      <input
        type="text"
        inputMode={characteristic.dataType === "Number" ? "decimal" : "text"}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        className={inputClassName}
        placeholder={
          characteristic.dataType === "Number"
            ? "Введите число"
            : "Введите значение"
        }
      />
    </label>
  );
}
