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
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { CatalogImportRowStatusBadge } from "../CatalogImportRowStatusBadge";
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
  defaultProductTypeId?: string | null;
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

const textareaClassName = [
  "min-h-24 w-full min-w-0 resize-y rounded-xl border px-4 py-3",
  "border-[var(--app-border)] bg-[var(--app-surface)]",
  "text-sm text-[var(--app-text)]",
  "placeholder:text-[var(--app-muted)]",
  "transition-colors motion-reduce:transition-none",
  "enabled:hover:border-[var(--app-border-strong)]",
  "focus:outline-none focus:border-[var(--app-accent)]",
  "focus:ring-2 focus:ring-[var(--app-accent)]",
  "disabled:cursor-not-allowed disabled:opacity-60",
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
  defaultProductTypeId,
  expectedVersion,
  rows,
  onCancel,
  onSaved,
}: CatalogImportBulkRowsEditorProps) {
  const queryClient = useQueryClient();

  const [drafts, setDrafts] = useState<
    Record<string, BulkCatalogImportRowDraft>
  >(() => createInitialDrafts(rows));

  const [productTypeId, setProductTypeId] = useState(() => {
    const rowProductTypeIds = [
      ...new Set(
        rows
          .map((row) => row.data.productTypeId)
          .filter((value): value is string => Boolean(value)),
      ),
    ];

    return rowProductTypeIds.length === 1
      ? rowProductTypeIds[0]
      : (defaultProductTypeId ?? "");
  });

  const [commonManufacturerId, setCommonManufacturerId] = useState("");
  const [confirmRecognitionSuggestions, setConfirmRecognitionSuggestions] =
    useState(false);

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
    mutationKey: catalogImportQueryKeys.saveRows(batchId),
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
          queryKey: catalogImportQueryKeys.rowProblemCodesRoot(batchId),
          refetchType: "none",
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

    if (!productTypeId) {
      setGeneralError(
        "Выберите тип товара, который будет применён ко всем выбранным строкам.",
      );

      return;
    }

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
        productTypeId,
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
      confirmRecognitionSuggestions,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid min-w-0 gap-5 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-4 text-[var(--app-text)] sm:p-5"
    >
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-xl font-semibold text-[var(--app-text)]">
            Массовое редактирование строк
          </h3>

          <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
            Выбрано строк:{" "}
            <span className="font-semibold text-[var(--app-accent)]">
              {rows.length}
            </span>
            . Все изменения будут отправлены одним запросом и проверены backend.
          </p>

          <p className="mt-1 text-xs text-[var(--app-muted)]">
            Версия пакета: {expectedVersion}
          </p>
        </div>

        <AppButton
          type="button"
          size="sm"
          variant="secondary"
          disabled={saveMutation.isPending}
          onClick={onCancel}
        >
          Закрыть редактор
        </AppButton>
      </div>

      {metadataError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            metadataError,
            "Не удалось загрузить справочники для массового редактирования.",
          )}
        </div>
      )}

      {isMetadataLoading && (
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm text-[var(--app-text)]">
          Загружаем производителей и характеристики выбранного типа товара...
        </div>
      )}

      <label className="flex items-start gap-3 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4">
        <input
          type="checkbox"
          checked={confirmRecognitionSuggestions}
          disabled={isBusy}
          onChange={(event) => {
            setConfirmRecognitionSuggestions(event.target.checked);
            setGeneralError(null);
            saveMutation.reset();
          }}
          className="mt-1 h-4 w-4 shrink-0 accent-amber-500"
        />

        <span>
          <span className="block text-sm font-semibold text-amber-100">
            Подтвердить текущие значения как решение конфликтов
          </span>

          <span className="mt-1 block text-xs leading-5 text-amber-100/70">
            Включайте только после проверки выбранных строк. При применении
            импорта подтверждённые решения будут использованы для подготовки
            предложений словаря. Правила не включаются автоматически.
          </span>
        </span>
      </label>

      <div className="grid gap-4 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4 md:grid-cols-2">
        <div className="grid content-start gap-2">
          <span className="text-sm font-semibold text-[var(--app-text)]">
            Общее исправление: тип товара
          </span>

          <p className="text-xs leading-5 text-[var(--app-muted)]">
            Выбранный тип будет установлен для всех строк этой группы; набор
            характеристик ниже обновится под него.
          </p>

          <AppSelect
            ariaLabel="Тип товара для выбранных строк"
            value={productTypeId}
            disabled={isBusy}
            onChange={(value) => {
              setProductTypeId(value);
              setGeneralError(null);
              saveMutation.reset();
            }}
            options={[
              { value: "", label: "Выберите тип товара" },
              ...(productTypesQuery.data ?? []).map((productType) => ({
                value: productType.id,
                label: `${productType.name} · ${productType.code}`,
              })),
            ]}
          />
        </div>

        <div className="grid content-start gap-2">
          <span className="text-sm font-semibold text-[var(--app-text)]">
            Общее исправление: производитель
          </span>

          <p className="text-xs leading-5 text-[var(--app-muted)]">
            При выборе производитель будет установлен сразу во всех строках
            открытой группы.
          </p>

          <AppSelect
            ariaLabel="Производитель для выбранных строк"
            value={commonManufacturerId}
            disabled={isBusy}
            onChange={(value) => {
              setCommonManufacturerId(value);

              if (!value) {
                return;
              }

              setDrafts((currentDrafts) =>
                Object.fromEntries(
                  Object.entries(currentDrafts).map(([rowId, draft]) => [
                    rowId,
                    {
                      ...draft,
                      manufacturerId: value,
                    },
                  ]),
                ),
              );

              setRowErrors({});
              setGeneralError(null);
              saveMutation.reset();
            }}
            options={[
              { value: "", label: "Не изменять совместно" },
              ...manufacturers.map((manufacturer) => ({
                value: manufacturer.id,
                label: manufacturer.name,
              })),
            ]}
          />
        </div>
      </div>

      <div className="grid gap-4">
        {rows.map((row) => {
          const draft = drafts[row.rowId];

          if (!draft) {
            return null;
          }

          const errors = rowErrors[row.rowId] ?? [];

          return (
            <section
              key={row.rowId}
              className={[
                "min-w-0 rounded-2xl border bg-[var(--app-panel-strong)] p-4",
                errors.length > 0
                  ? "border-[var(--app-danger)]"
                  : "border-[var(--app-border)]",
              ].join(" ")}
            >
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
                <div>
                  <h4 className="font-semibold text-[var(--app-text)]">
                    Строка {row.rowNumber}
                  </h4>

                  <p className="mt-1 break-all text-xs text-[var(--app-muted)]">
                    {row.rowId}
                  </p>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <span className="text-xs text-[var(--app-muted)]">
                    Статус до сохранения:
                  </span>

                  <CatalogImportRowStatusBadge status={row.status} />
                </div>
              </div>

              <div className="mt-5 grid gap-4 xl:grid-cols-2">
                <label className="grid gap-2">
                  <span className="text-sm font-medium text-[var(--app-text)]">
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
                    className={textareaClassName}
                    placeholder="Наименование товара"
                  />
                </label>

                <label className="grid content-start gap-2">
                  <span className="text-sm font-medium text-[var(--app-text)]">
                    Артикул
                  </span>

                  <AppInput
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
                    placeholder="Артикул товара"
                  />
                </label>

                <div className="grid content-start gap-2">
                  <span className="text-sm font-medium text-[var(--app-text)]">
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
                    <p className="text-xs text-[var(--app-muted)]">
                      Значение после анализа: {row.data.manufacturer}
                    </p>
                  )}
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <label className="grid content-start gap-2">
                    <span className="text-sm font-medium text-[var(--app-text)]">
                      Цена
                    </span>

                    <AppInput
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
                      placeholder="0.00"
                    />
                  </label>

                  <label className="grid content-start gap-2">
                    <span className="text-sm font-medium text-[var(--app-text)]">
                      Остаток
                    </span>

                    <AppInput
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
                      placeholder="0"
                    />
                  </label>
                </div>
              </div>

              <div className="mt-5 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
                <h5 className="font-semibold text-[var(--app-text)]">
                  Характеристики товара
                </h5>

                {characteristics.length === 0 ? (
                  <p className="mt-3 text-sm text-[var(--app-muted)]">
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
                <div className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4">
                  <h5 className="font-semibold text-[var(--app-danger)]">
                    Ошибки строки {row.rowNumber}
                  </h5>

                  <ul className="mt-3 grid gap-2 text-sm text-[var(--app-danger)]">
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
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
          {generalError}
        </div>
      )}

      {saveMutation.isError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            saveMutation.error,
            "Не удалось массово сохранить строки импорта.",
          )}
        </div>
      )}

      <div className="flex flex-col-reverse gap-3 border-t border-[var(--app-border)] pt-4 sm:flex-row sm:justify-end">
        <AppButton
          type="button"
          variant="secondary"
          disabled={saveMutation.isPending}
          onClick={onCancel}
        >
          Отмена
        </AppButton>

        <AppButton
          type="submit"
          variant="primary"
          loading={saveMutation.isPending}
          disabled={isBusy || Boolean(metadataError)}
        >
          {saveMutation.isPending
            ? "Сохраняем и проверяем строки..."
            : `Сохранить строки: ${rows.length}`}
        </AppButton>
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
        <span className="text-sm font-medium text-[var(--app-text)]">
          {label}
        </span>

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
      <span className="text-sm font-medium text-[var(--app-text)]">
        {label}
      </span>

      <AppInput
        type="text"
        inputMode={characteristic.dataType === "Number" ? "decimal" : "text"}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        placeholder={
          characteristic.dataType === "Number"
            ? "Введите число"
            : "Введите значение"
        }
      />
    </label>
  );
}
