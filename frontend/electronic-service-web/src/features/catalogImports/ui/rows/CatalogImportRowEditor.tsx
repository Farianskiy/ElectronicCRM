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
import { updateCatalogImportRow } from "../../api/updateCatalogImportRow";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type {
  CatalogImportRow,
  UpdateCatalogImportRowRequest,
  UpdateCatalogImportRowResponse,
} from "../../model/types";

interface CatalogImportRowEditorProps {
  batchId: string;
  defaultProductTypeId?: string | null;
  row: CatalogImportRow;
  onCancel: () => void;
  onSaved: (result: UpdateCatalogImportRowResponse) => void;
}

interface ParsedNumberResult {
  value: number | null;
  error?: string;
}

const textareaClassName = [
  "min-h-28 w-full min-w-0 resize-y rounded-xl border px-4 py-3",
  "border-[var(--app-border)] bg-[var(--app-surface)]",
  "text-sm text-[var(--app-text)]",
  "placeholder:text-[var(--app-muted)]",
  "transition-colors motion-reduce:transition-none",
  "enabled:hover:border-[var(--app-border-strong)]",
  "focus:outline-none focus:border-[var(--app-accent)]",
  "focus:ring-2 focus:ring-[var(--app-accent)]",
  "disabled:cursor-not-allowed disabled:opacity-60",
].join(" ");

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

function getCharacteristicLabel(
  characteristic: CatalogProductTypeCharacteristicMetadata,
): string {
  const unit = characteristic.unit ? `, ${characteristic.unit}` : "";

  return characteristic.isRequired
    ? `${characteristic.name}${unit} · обязательная`
    : `${characteristic.name}${unit}`;
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

export function CatalogImportRowEditor({
  batchId,
  defaultProductTypeId,
  row,
  onCancel,
  onSaved,
}: CatalogImportRowEditorProps) {
  const queryClient = useQueryClient();

  const [name, setName] = useState(row.data.name ?? "");

  const [article, setArticle] = useState(row.data.article ?? "");

  const [productTypeId, setProductTypeId] = useState(
    row.data.productTypeId ?? defaultProductTypeId ?? "",
  );

  const [manufacturerId, setManufacturerId] = useState(
    row.data.manufacturerId ?? "",
  );

  const [price, setPrice] = useState(row.data.price?.toString() ?? "");

  const [stockQuantity, setStockQuantity] = useState(
    row.data.stockQuantity?.toString() ?? "",
  );

  const [characteristicValues, setCharacteristicValues] = useState<
    Record<string, string>
  >({
    ...row.data.characteristics,
  });

  const [formErrors, setFormErrors] = useState<string[]>([]);

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

  const characteristics = characteristicsQuery.data ?? [];
  const manufacturers = manufacturersQuery.data ?? [];

  const metadataError =
    productTypesQuery.error ??
    manufacturersQuery.error ??
    characteristicsQuery.error;

  const saveMutation = useMutation({
    mutationKey: catalogImportQueryKeys.saveRows(batchId),
    mutationFn: (request: UpdateCatalogImportRowRequest) =>
      updateCatalogImportRow(batchId, row.rowId, request),

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

  function handleCharacteristicChange(
    characteristicId: string,
    value: string,
  ): void {
    setCharacteristicValues((currentValues) => ({
      ...currentValues,
      [characteristicId]: value,
    }));

    setFormErrors([]);
    saveMutation.reset();
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const errors: string[] = [];

    const parsedPrice = parseNullableDecimal(price, "Цена");

    if (parsedPrice.error) {
      errors.push(parsedPrice.error);
    }

    const parsedStockQuantity = parseNullableInteger(stockQuantity, "Остаток");

    if (parsedStockQuantity.error) {
      errors.push(parsedStockQuantity.error);
    }

    if (!productTypeId) {
      errors.push("Выберите тип товара.");
    }

    if (errors.length > 0) {
      setFormErrors(errors);

      return;
    }

    const requestCharacteristics: Record<string, string> = {};

    for (const characteristic of characteristics) {
      const rawValue = characteristicValues[characteristic.id] ?? "";

      const normalizedValue = normalizeCharacteristicValue(
        rawValue,
        characteristic.dataType,
      );

      /*
       * Пустые необязательные характеристики не отправляем.
       * Для обязательных backend самостоятельно сформирует issue.
       */
      if (!normalizedValue) {
        continue;
      }

      requestCharacteristics[characteristic.id] = normalizedValue;
    }

    const request: UpdateCatalogImportRowRequest = {
      name: name.trim() || null,
      article: article.trim() || null,
      productTypeId: productTypeId || null,
      manufacturerId: manufacturerId || null,
      price: parsedPrice.value,
      stockQuantity: parsedStockQuantity.value,
      characteristics: requestCharacteristics,
    };

    setFormErrors([]);
    saveMutation.mutate(request);
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid min-w-0 gap-5 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-4 text-[var(--app-text)] sm:p-5"
    >
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <h3 className="text-lg font-semibold text-[var(--app-text)]">
            Редактирование строки {row.rowNumber}
          </h3>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            После сохранения строка будет повторно проверена backend.
          </p>
        </div>

        <AppButton
          type="button"
          size="sm"
          variant="secondary"
          disabled={saveMutation.isPending}
          onClick={onCancel}
        >
          Закрыть
        </AppButton>
      </div>

      {metadataError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            metadataError,
            "Не удалось загрузить справочники для редактирования строки.",
          )}
        </div>
      )}

      {isMetadataLoading && (
        <div className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 text-sm text-[var(--app-text)]">
          Загружаем производителей и характеристики...
        </div>
      )}

      <div className="grid gap-4 xl:grid-cols-2">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Наименование
          </span>

          <textarea
            value={name}
            disabled={isBusy}
            rows={3}
            onChange={(event) => {
              setName(event.target.value);
              setFormErrors([]);
              saveMutation.reset();
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
            value={article}
            disabled={isBusy}
            onChange={(event) => {
              setArticle(event.target.value);
              setFormErrors([]);
              saveMutation.reset();
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
            value={manufacturerId}
            disabled={isBusy}
            onChange={(value) => {
              setManufacturerId(value);
              setFormErrors([]);
              saveMutation.reset();
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

        <div className="grid content-start gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Тип товара
          </span>

          <AppSelect
            ariaLabel={`Тип товара строки ${row.rowNumber}`}
            value={productTypeId}
            disabled={isBusy}
            onChange={(value) => {
              setProductTypeId(value);
              setCharacteristicValues({});
              setFormErrors([]);
              saveMutation.reset();
            }}
            options={[
              { value: "", label: "Тип товара не выбран" },
              ...(productTypesQuery.data ?? []).map((productType) => ({
                value: productType.id,
                label: `${productType.name} · ${productType.code}`,
              })),
            ]}
          />
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="grid content-start gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Цена
            </span>

            <AppInput
              type="text"
              inputMode="decimal"
              value={price}
              disabled={isBusy}
              onChange={(event) => {
                setPrice(event.target.value);
                setFormErrors([]);
                saveMutation.reset();
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
              value={stockQuantity}
              disabled={isBusy}
              onChange={(event) => {
                setStockQuantity(event.target.value);
                setFormErrors([]);
                saveMutation.reset();
              }}
              placeholder="0"
            />
          </label>
        </div>
      </div>

      <section className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
        <div>
          <h3 className="font-semibold text-[var(--app-text)]">
            Характеристики товара
          </h3>

          <p className="mt-1 text-sm text-[var(--app-muted)]">
            Тип товара:{" "}
            <span className="text-[var(--app-text)]">
              {selectedProductType?.name ?? "не выбран"}
            </span>
          </p>
        </div>

        {characteristics.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--app-muted)]">
            Для выбранного типа характеристики не определены.
          </p>
        ) : (
          <div className="mt-5 grid gap-4 xl:grid-cols-2">
            {characteristics.map((characteristic) => (
              <CharacteristicInput
                key={characteristic.id}
                characteristic={characteristic}
                value={characteristicValues[characteristic.id] ?? ""}
                disabled={isBusy}
                onChange={(value) =>
                  handleCharacteristicChange(characteristic.id, value)
                }
              />
            ))}
          </div>
        )}
      </section>

      {formErrors.length > 0 && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5">
          <h3 className="font-semibold text-[var(--app-danger)]">
            Проверьте введённые значения
          </h3>

          <ul className="mt-3 grid gap-2 text-sm text-[var(--app-danger)]">
            {formErrors.map((error) => (
              <li key={error}>• {error}</li>
            ))}
          </ul>
        </div>
      )}

      {saveMutation.isError && (
        <div className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            saveMutation.error,
            "Не удалось сохранить строку импорта.",
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
            ? "Сохраняем и проверяем..."
            : "Сохранить строку"}
        </AppButton>
      </div>
    </form>
  );
}

function CharacteristicInput({
  characteristic,
  value,
  disabled,
  onChange,
}: {
  characteristic: CatalogProductTypeCharacteristicMetadata;
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
          ariaLabel={label}
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
