"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useMemo, useState } from "react";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import type {
  CatalogCharacteristicDataType,
  CatalogProductTypeCharacteristicMetadata,
} from "@/features/catalogMetadata/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";
import { setCatalogProductCharacteristic } from "../api/setCatalogProductCharacteristic";
import type { CatalogProductDetails } from "../model/types";

interface TechnicalProductCharacteristicsEditorProps {
  product: CatalogProductDetails;
}

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data;

    if (typeof responseData === "string") {
      return responseData;
    }

    if (typeof responseData?.detail === "string") {
      return responseData.detail;
    }

    if (typeof responseData?.message === "string") {
      return responseData.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Не удалось изменить характеристику.";
}

function normalizeBooleanValue(value: string): string {
  const normalizedValue = value.trim().toLowerCase();

  if (
    normalizedValue === "true" ||
    normalizedValue === "да" ||
    normalizedValue === "есть" ||
    normalizedValue === "1" ||
    normalizedValue === "+"
  ) {
    return "true";
  }

  if (
    normalizedValue === "false" ||
    normalizedValue === "нет" ||
    normalizedValue === "отсутствует" ||
    normalizedValue === "0" ||
    normalizedValue === "-"
  ) {
    return "false";
  }

  return "";
}

function normalizeValue(
  dataType: CatalogCharacteristicDataType,
  value: string,
): string | null {
  const trimmedValue = value.trim();

  if (trimmedValue.length === 0) {
    return null;
  }

  if (dataType === "Number") {
    const normalizedNumber = trimmedValue.replace(",", ".");

    const parsedNumber = Number(normalizedNumber);

    if (!Number.isFinite(parsedNumber)) {
      return null;
    }

    return normalizedNumber;
  }

  if (dataType === "Boolean") {
    const booleanValue = normalizeBooleanValue(trimmedValue);

    return booleanValue || null;
  }

  return trimmedValue;
}

export function TechnicalProductCharacteristicsEditor({
  product,
}: TechnicalProductCharacteristicsEditorProps) {
  const queryClient = useQueryClient();

  const [draftValues, setDraftValues] = useState<Record<string, string>>({});

  const savedValues = useMemo(() => {
    const result: Record<string, string> = {};

    for (const characteristic of product.characteristics) {
      result[characteristic.code] = characteristic.value;
    }

    return result;
  }, [product.characteristics]);

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const metadataQuery = useQuery({
    queryKey: ["catalog-product-type-characteristics", product.productTypeCode],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(product.productTypeCode),
    enabled: product.productTypeCode.length > 0,
    staleTime: 5 * 60 * 1000,
  });

  const characteristics = useMemo(() => {
    return [...(metadataQuery.data ?? [])].sort((first, second) => {
      if (first.isRequired !== second.isRequired) {
        return first.isRequired ? -1 : 1;
      }

      return first.name.localeCompare(second.name, "ru");
    });
  }, [metadataQuery.data]);

  const mutation = useMutation({
    mutationFn: async (request: { code: string; value: string }) => {
      await setCatalogProductCharacteristic(product.id, request);
    },

    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details", product.id],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),
      ]);

      setDraftValues((currentValues) => {
        const nextValues = {
          ...currentValues,
        };

        delete nextValues[variables.code];

        return nextValues;
      });

      setValidationError(null);
      setSuccessMessage(`Характеристика ${variables.code} сохранена.`);
    },
  });

  function handleValueChange(characteristicCode: string, value: string): void {
    setDraftValues((currentValues) => ({
      ...currentValues,
      [characteristicCode]: value,
    }));

    setValidationError(null);
    setSuccessMessage(null);
  }

  function handleSave(
    characteristic: CatalogProductTypeCharacteristicMetadata,
  ): void {
    mutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    const rawValue =
      draftValues[characteristic.code] ??
      savedValues[characteristic.code] ??
      "";

    const normalizedValue = normalizeValue(characteristic.dataType, rawValue);

    if (normalizedValue === null) {
      setValidationError(
        `Укажите корректное значение характеристики «${characteristic.name}».`,
      );

      return;
    }

    mutation.mutate({
      code: characteristic.code,
      value: normalizedValue,
    });
  }

  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <div>
        <h3 className="font-semibold text-white">Характеристики</h3>

        <p className="mt-2 text-sm text-slate-400">
          Можно изменить существующее значение или добавить отсутствующую
          характеристику.
        </p>
      </div>

      {metadataQuery.isLoading && (
        <p className="mt-5 text-sm text-slate-400">
          Загружаем характеристики типа товара...
        </p>
      )}

      {metadataQuery.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(metadataQuery.error)}
        </div>
      )}

      {validationError && (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutation.error)}
        </div>
      )}

      {successMessage && (
        <div className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      {!metadataQuery.isLoading &&
        !metadataQuery.isError &&
        characteristics.length === 0 && (
          <p className="mt-5 text-sm text-slate-400">
            Для этого типа товара характеристики не настроены.
          </p>
        )}

      <div className="mt-5 grid gap-4">
        {characteristics.map((characteristic) => {
          const rawValue =
            draftValues[characteristic.code] ??
            savedValues[characteristic.code] ??
            "";

          const fieldValue =
            characteristic.dataType === "Boolean"
              ? normalizeBooleanValue(rawValue)
              : rawValue;

          const isSaving =
            mutation.isPending &&
            mutation.variables?.code === characteristic.code;

          const label = characteristic.unit
            ? `${characteristic.name}, ${characteristic.unit}`
            : characteristic.name;

          return (
            <div
              key={characteristic.id}
              className="grid gap-4 rounded-2xl border border-white/10 bg-white/[0.02] p-4 lg:grid-cols-[minmax(220px,1fr)_minmax(260px,1.4fr)_auto] lg:items-end"
            >
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <p className="font-medium text-slate-100">{label}</p>

                  {characteristic.isRequired && (
                    <span className="rounded-full bg-amber-500/15 px-2 py-1 text-xs font-medium text-amber-300">
                      Обязательная
                    </span>
                  )}
                </div>

                <p className="mt-1 text-xs text-slate-500">
                  {characteristic.code} · {characteristic.dataType}
                </p>
              </div>

              {characteristic.dataType === "Boolean" ? (
                <AppSelect
                  ariaLabel={label}
                  value={fieldValue}
                  onChange={(value) =>
                    handleValueChange(characteristic.code, value)
                  }
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
              ) : (
                <input
                  type="text"
                  inputMode={
                    characteristic.dataType === "Number" ? "decimal" : "text"
                  }
                  value={fieldValue}
                  onChange={(event) =>
                    handleValueChange(characteristic.code, event.target.value)
                  }
                  placeholder={
                    characteristic.dataType === "Number"
                      ? "Введите число"
                      : "Введите значение"
                  }
                  className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
                />
              )}

              <button
                type="button"
                disabled={isSaving}
                onClick={() => handleSave(characteristic)}
                className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {isSaving ? "Сохраняем..." : "Сохранить"}
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
}
