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
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { setCatalogProductCharacteristic } from "../api/setCatalogProductCharacteristic";
import { removeCatalogProductCharacteristic } from "../api/removeCatalogProductCharacteristic";
import type { CatalogProductDetails } from "../model/types";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";

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

  const [
    characteristicCodePendingRemoval,
    setCharacteristicCodePendingRemoval,
  ] = useState<string | null>(null);

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

  const saveMutation = useMutation({
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
        queryClient.invalidateQueries({
          queryKey: catalogProductAuditHistoryQueryKey(product.id),
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

  const removeMutation = useMutation({
    mutationFn: async (request: { code: string; name: string }) => {
      await removeCatalogProductCharacteristic(product.id, request.code);
    },

    onSuccess: async (_data, removedCharacteristic) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details", product.id],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),

        queryClient.invalidateQueries({
          queryKey: catalogProductAuditHistoryQueryKey(product.id),
        }),
      ]);

      setDraftValues((currentValues) => {
        const nextValues = {
          ...currentValues,
        };

        delete nextValues[removedCharacteristic.code];

        return nextValues;
      });

      setCharacteristicCodePendingRemoval(null);
      setValidationError(null);

      setSuccessMessage(
        `Характеристика «${removedCharacteristic.name}» удалена.`,
      );
    },
  });

  function handleValueChange(characteristicCode: string, value: string): void {
    setDraftValues((currentValues) => ({
      ...currentValues,
      [characteristicCode]: value,
    }));

    saveMutation.reset();
    removeMutation.reset();

    setCharacteristicCodePendingRemoval(null);
    setValidationError(null);
    setSuccessMessage(null);
  }

  function handleSave(
    characteristic: CatalogProductTypeCharacteristicMetadata,
  ): void {
    saveMutation.reset();
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

    saveMutation.mutate({
      code: characteristic.code,
      value: normalizedValue,
    });
  }

  const mutationError = saveMutation.error ?? removeMutation.error;

  return (
    <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
      <div>
        <h3 className="text-base font-semibold text-[var(--app-text)]">
          Характеристики
        </h3>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Можно изменить существующее значение или добавить отсутствующую
          характеристику.
        </p>
      </div>

      {metadataQuery.isLoading && (
        <p role="status" className="mt-5 text-sm text-[var(--app-muted)]">
          Загружаем характеристики типа товара...
        </p>
      )}

      {metadataQuery.isError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(metadataQuery.error)}
        </div>
      )}

      {validationError && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {validationError}
        </div>
      )}

      {mutationError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(mutationError)}
        </div>
      )}

      {successMessage && (
        <div
          role="status"
          className="mt-5 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm leading-6 text-[var(--app-success)] [overflow-wrap:anywhere]"
        >
          {successMessage}
        </div>
      )}

      {!metadataQuery.isLoading &&
        !metadataQuery.isError &&
        characteristics.length === 0 && (
          <p className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
            Для этого типа товара характеристики не настроены.
          </p>
        )}

      <div className="mt-5 grid min-w-0 gap-4">
        {characteristics.map((characteristic) => {
          const rawValue =
            draftValues[characteristic.code] ??
            savedValues[characteristic.code] ??
            "";

          const fieldValue =
            characteristic.dataType === "Boolean"
              ? normalizeBooleanValue(rawValue)
              : rawValue;

          const hasSavedValue = Object.prototype.hasOwnProperty.call(
            savedValues,
            characteristic.code,
          );

          const isAwaitingRemovalConfirmation =
            characteristicCodePendingRemoval === characteristic.code;

          const isRemoving =
            removeMutation.isPending &&
            removeMutation.variables?.code === characteristic.code;

          const operationsArePending =
            saveMutation.isPending || removeMutation.isPending;

          const isSaving =
            saveMutation.isPending &&
            saveMutation.variables?.code === characteristic.code;

          const label = characteristic.unit
            ? `${characteristic.name}, ${characteristic.unit}`
            : characteristic.name;

          return (
            <div
              key={characteristic.id}
              className="grid min-w-0 gap-4 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)] lg:items-end xl:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)_minmax(0,1fr)]"
            >
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="min-w-0 font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                    {label}
                  </p>

                  {characteristic.isRequired && (
                    <span className="rounded-full border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-2 py-1 text-xs font-medium text-[var(--app-accent)]">
                      Обязательная
                    </span>
                  )}
                </div>

                <p className="mt-1 text-xs leading-5 text-[var(--app-muted)] [overflow-wrap:anywhere]">
                  {characteristic.code} · {characteristic.dataType}
                </p>
              </div>

              <div className="min-w-0">
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
                  <AppInput
                    type="text"
                    aria-label={label}
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
                  />
                )}
              </div>

              <div className="flex min-w-0 flex-wrap gap-2 lg:col-span-2 lg:justify-end xl:col-span-1">
                <AppButton
                  type="button"
                  variant="primary"
                  disabled={operationsArePending}
                  loading={isSaving}
                  onClick={() => {
                    setCharacteristicCodePendingRemoval(null);
                    removeMutation.reset();
                    handleSave(characteristic);
                  }}
                >
                  {isSaving
                    ? "Сохраняем..."
                    : hasSavedValue
                      ? "Сохранить"
                      : "Добавить"}
                </AppButton>

                {hasSavedValue && !characteristic.isRequired && (
                  <>
                    {isAwaitingRemovalConfirmation ? (
                      <>
                        <AppButton
                          type="button"
                          variant="danger"
                          disabled={operationsArePending}
                          loading={isRemoving}
                          onClick={() =>
                            removeMutation.mutate({
                              code: characteristic.code,
                              name: characteristic.name,
                            })
                          }
                        >
                          {isRemoving ? "Удаляем..." : "Подтвердить"}
                        </AppButton>

                        <AppButton
                          type="button"
                          variant="secondary"
                          disabled={operationsArePending}
                          onClick={() =>
                            setCharacteristicCodePendingRemoval(null)
                          }
                        >
                          Отмена
                        </AppButton>
                      </>
                    ) : (
                      <AppButton
                        type="button"
                        variant="danger"
                        disabled={operationsArePending}
                        onClick={() => {
                          saveMutation.reset();
                          removeMutation.reset();
                          setValidationError(null);
                          setSuccessMessage(null);

                          setCharacteristicCodePendingRemoval(
                            characteristic.code,
                          );
                        }}
                      >
                        Удалить
                      </AppButton>
                    )}
                  </>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
