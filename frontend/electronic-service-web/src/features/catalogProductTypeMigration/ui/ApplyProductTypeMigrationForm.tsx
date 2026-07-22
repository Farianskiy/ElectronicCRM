"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import type { CatalogProductDetails } from "@/features/catalogProducts/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";
import { applyProductTypeMigration } from "../api/applyProductTypeMigration";
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

  const [valueDrafts, setValueDrafts] = useState<Record<string, string>>({});

  const [isConfirmed, setIsConfirmed] = useState(false);

  const [validationError, setValidationError] = useState<string | null>(null);

  const mutation = useMutation({
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

  function updateDraft(definitionId: string, value: string): void {
    setValueDrafts((current) => ({
      ...current,
      [definitionId]: value,
    }));

    setValidationError(null);
    mutation.reset();
  }

  function handleApply(): void {
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
    <section className="rounded-2xl border border-red-500/25 bg-red-500/[0.05] p-5">
      <div>
        <h4 className="font-semibold text-white">Применить смену типа</h4>

        <p className="mt-2 text-sm text-slate-400">
          Backend повторно построит план и отклонит операцию, если товар или
          схема типа изменились после preview.
        </p>
      </div>

      {preview.missingRequiredCharacteristics.length > 0 && (
        <div className="mt-5 grid gap-4">
          {preview.missingRequiredCharacteristics.map((characteristic) => (
            <RequiredValueField
              key={characteristic.definitionId}
              characteristic={characteristic}
              value={valueDrafts[characteristic.definitionId] ?? ""}
              onChange={(value) =>
                updateDraft(characteristic.definitionId, value)
              }
            />
          ))}
        </div>
      )}

      <label className="mt-5 flex items-start gap-3 rounded-2xl border border-white/10 bg-black/20 p-4">
        <input
          type="checkbox"
          checked={isConfirmed}
          onChange={(event) => {
            setIsConfirmed(event.target.checked);

            setValidationError(null);
            mutation.reset();
          }}
          className="mt-1 size-4"
        />

        <span className="text-sm text-slate-300">
          Я подтверждаю смену типа с «{preview.currentProductTypeName}» на «
          {preview.targetProductTypeName}».
          {preview.removedCharacteristics.length > 0 &&
            ` Будут удалены значения характеристик: ${preview.removedCharacteristics.length}.`}
        </span>
      </label>

      {validationError && (
        <div className="mt-4 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutation.error)}
        </div>
      )}

      <button
        type="button"
        disabled={mutation.isPending}
        onClick={handleApply}
        className="mt-5 rounded-2xl bg-red-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-red-400 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {mutation.isPending
          ? "Применяем миграцию..."
          : "Подтвердить и сменить тип"}
      </button>
    </section>
  );
}

function RequiredValueField({
  characteristic,
  value,
  onChange,
}: {
  characteristic: ProductTypeMigrationMissingRequiredCharacteristic;

  value: string;
  onChange: (value: string) => void;
}) {
  if (characteristic.dataType === "Boolean") {
    return (
      <div className="grid gap-2">
        <span className="text-sm text-slate-300">{characteristic.name}</span>

        <AppSelect
          ariaLabel={characteristic.name}
          value={value}
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
    <label className="grid gap-2">
      <span className="text-sm text-slate-300">
        {characteristic.name}
        {characteristic.unit ? `, ${characteristic.unit}` : ""}
      </span>

      <input
        type={characteristic.dataType === "Number" ? "number" : "text"}
        step={characteristic.dataType === "Number" ? "any" : undefined}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400"
      />
    </label>
  );
}
