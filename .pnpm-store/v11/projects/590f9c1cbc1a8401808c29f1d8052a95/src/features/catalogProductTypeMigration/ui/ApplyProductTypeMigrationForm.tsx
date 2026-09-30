"use client";

import {
  useIsMutating,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import type { CatalogProductDetails } from "@/features/catalogProducts/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";
import { applyProductTypeMigration } from "../api/applyProductTypeMigration";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import type {
  ProductTypeMigrationMissingRequiredCharacteristic,
  ProductTypeMigrationPreview,
} from "../model/types";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";

interface ApplyProductTypeMigrationFormProps {
  product: CatalogProductDetails;
  preview: ProductTypeMigrationPreview;
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

  return "Не удалось применить смену типа.";
}

export function ApplyProductTypeMigrationForm({
  product,
  preview,
}: ApplyProductTypeMigrationFormProps) {
  const queryClient = useQueryClient();

  const mutationKey = ["apply-product-type-migration", product.id] as const;

  const isApplying = useIsMutating({ mutationKey, exact: true }) > 0;

  const [valueDrafts, setValueDrafts] = useState<Record<string, string>>({});

  const [isConfirmed, setIsConfirmed] = useState(false);

  const [validationError, setValidationError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationKey,

    mutationFn: async () => {
      await applyProductTypeMigration(product.id, {
        targetProductTypeId: preview.targetProductTypeId,

        expectedProductVersion: preview.productVersion,

        expectedCurrentProductTypeId: preview.currentProductTypeId,

        expectedRemovedCharacteristicDefinitionIds:
          preview.removedCharacteristics.map(
            (characteristic) => characteristic.definitionId,
          ),

        expectedMissingRequiredCharacteristicDefinitionIds:
          preview.missingRequiredCharacteristics.map(
            (characteristic) => characteristic.definitionId,
          ),

        requiredValues: preview.missingRequiredCharacteristics.map(
          (characteristic) => ({
            definitionId: characteristic.definitionId,

            value: valueDrafts[characteristic.definitionId]?.trim() ?? "",
          }),
        ),
      });
    },

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details", product.id],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristics"],
        }),

        queryClient.invalidateQueries({
          queryKey: catalogProductAuditHistoryQueryKey(product.id),
        }),
      ]);
    },
  });

  function isApplyInProgress(): boolean {
    return queryClient.isMutating({ mutationKey, exact: true }) > 0;
  }

  function updateDraft(definitionId: string, value: string): void {
    if (isApplyInProgress()) {
      return;
    }

    setValueDrafts((current) => ({
      ...current,
      [definitionId]: value,
    }));

    setValidationError(null);
    mutation.reset();
  }

  function handleApply(): void {
    if (isApplyInProgress()) {
      return;
    }

    setValidationError(null);
    mutation.reset();

    const missingValue = preview.missingRequiredCharacteristics.find(
      (characteristic) => {
        const value = valueDrafts[characteristic.definitionId]?.trim();

        return !value;
      },
    );

    if (missingValue) {
      setValidationError(
        `Заполните характеристику ` + `«${missingValue.name}».`,
      );

      return;
    }

    if (!isConfirmed) {
      setValidationError("Подтвердите применение миграции.");

      return;
    }

    mutation.mutate();
  }

  return (
    <section className="min-w-0 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5">
      <div className="flex min-w-0 items-start gap-3">
        <svg
          aria-hidden="true"
          focusable="false"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.8"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="mt-0.5 h-5 w-5 shrink-0 text-[var(--app-danger)]"
        >
          <path d="M10.3 3.9 1.8 18.1a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
          <path d="M12 9v4" />
          <path d="M12 17h.01" />
        </svg>

        <div className="min-w-0">
          <h4 className="font-semibold text-[var(--app-danger)]">
            Применить смену типа
          </h4>

          <p className="mt-2 text-sm text-[var(--app-text)]">
            Это действие изменит тип товара и применит показанный выше план.
          </p>

          <p className="mt-2 text-sm text-[var(--app-muted)]">
            Сервер повторно проверит план. Если товар или схема типа изменились
            после предпросмотра, операция будет отклонена.
          </p>
        </div>
      </div>

      {preview.missingRequiredCharacteristics.length > 0 && (
        <div className="mt-5 grid min-w-0 gap-4">
          {preview.missingRequiredCharacteristics.map((characteristic) => (
            <RequiredValueField
              key={characteristic.definitionId}
              characteristic={characteristic}
              value={valueDrafts[characteristic.definitionId] ?? ""}
              disabled={isApplying}
              onChange={(value) =>
                updateDraft(characteristic.definitionId, value)
              }
            />
          ))}
        </div>
      )}

      <label className="mt-5 flex min-w-0 cursor-pointer items-start gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
        <input
          type="checkbox"
          checked={isConfirmed}
          disabled={isApplying}
          onChange={(event) => {
            if (isApplyInProgress()) {
              return;
            }

            setIsConfirmed(event.target.checked);

            setValidationError(null);
            mutation.reset();
          }}
          className="mt-1 size-4 shrink-0 accent-[var(--app-danger)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--app-panel)]"
        />

        <span className="min-w-0 text-sm leading-6 text-[var(--app-text)] [overflow-wrap:anywhere]">
          Я подтверждаю смену типа с «{preview.currentProductTypeName}» на «
          {preview.targetProductTypeName}».
          {preview.removedCharacteristics.length > 0 && (
            <span className="mt-2 block font-medium text-[var(--app-danger)]">
              Будут удалены значения характеристик:{" "}
              {preview.removedCharacteristics.length}.
            </span>
          )}
        </span>
      </label>

      {validationError && (
        <div
          role="alert"
          className="mt-4 whitespace-pre-wrap rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-4 text-sm text-[var(--app-warning)] [overflow-wrap:anywhere]"
        >
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div
          role="alert"
          className="mt-4 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(mutation.error)}
        </div>
      )}

      <AppButton
        type="button"
        variant="dangerSolid"
        disabled={isApplying}
        loading={isApplying}
        onClick={handleApply}
        className="mt-5 w-full sm:w-auto"
      >
        {isApplying ? "Применяем миграцию..." : "Подтвердить и сменить тип"}
      </AppButton>
    </section>
  );
}

function RequiredValueField({
  characteristic,
  value,
  onChange,
  disabled,
}: {
  characteristic: ProductTypeMigrationMissingRequiredCharacteristic;
  value: string;
  onChange: (value: string) => void;
  disabled: boolean;
}) {
  if (characteristic.dataType === "Boolean") {
    return (
      <div className="grid min-w-0 gap-2 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
        <span className="text-sm font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {characteristic.name}
        </span>

        <AppSelect
          ariaLabel={characteristic.name}
          value={value}
          disabled={disabled}
          onChange={onChange}
          options={[
            {
              value: "",
              label: "Выберите значение",
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
    <label className="grid min-w-0 gap-2 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
      <span className="text-sm font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
        {characteristic.name}
        {characteristic.unit ? `, ${characteristic.unit}` : ""}
      </span>

      <AppInput
        type={characteristic.dataType === "Number" ? "number" : "text"}
        step={characteristic.dataType === "Number" ? "any" : undefined}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}
