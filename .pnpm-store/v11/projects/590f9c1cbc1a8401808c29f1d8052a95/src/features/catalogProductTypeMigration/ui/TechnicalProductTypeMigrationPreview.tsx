"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type { CatalogProductDetails } from "@/features/catalogProducts/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";
import { previewProductTypeMigration } from "../api/previewProductTypeMigration";
import type {
  ProductTypeMigrationCharacteristicValue,
  ProductTypeMigrationMissingRequiredCharacteristic,
} from "../model/types";
import { ApplyProductTypeMigrationForm } from "./ApplyProductTypeMigrationForm";
import { AppButton } from "@/shared/ui/AppButton";

interface TechnicalProductTypeMigrationPreviewProps {
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

  return "Не удалось построить план смены типа.";
}

function formatDataType(dataType: string): string {
  if (dataType === "Text") {
    return "Текст";
  }

  if (dataType === "Number") {
    return "Число";
  }

  if (dataType === "Boolean") {
    return "Да / Нет";
  }

  return dataType;
}

function formatValue(dataType: string, value: string): string {
  if (dataType === "Boolean") {
    if (value.toLowerCase() === "true") {
      return "Да";
    }

    if (value.toLowerCase() === "false") {
      return "Нет";
    }
  }

  return value;
}

export function TechnicalProductTypeMigrationPreview({
  product,
}: TechnicalProductTypeMigrationPreviewProps) {
  const [targetProductTypeId, setTargetProductTypeId] = useState("");

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const availableProductTypes = (productTypesQuery.data ?? []).filter(
    (productType) => productType.id !== product.productTypeId,
  );

  const selectedTargetType =
    availableProductTypes.find(
      (productType) => productType.id === targetProductTypeId,
    ) ?? null;

  const previewMutation = useMutation({
    mutationFn: async (selectedProductTypeId: string) => {
      return previewProductTypeMigration(product.id, {
        targetProductTypeId: selectedProductTypeId,
      });
    },
  });

  function handlePreview(): void {
    if (!selectedTargetType) {
      return;
    }

    previewMutation.reset();

    previewMutation.mutate(selectedTargetType.id);
  }

  const preview = previewMutation.data;

  return (
    <section className="min-w-0 rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-5">
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
          className="mt-0.5 h-5 w-5 shrink-0 text-[var(--app-warning)]"
        >
          <path d="M10.3 3.9 1.8 18.1a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
          <path d="M12 9v4" />
          <path d="M12 17h.01" />
        </svg>

        <div className="min-w-0">
          <h3 className="font-semibold text-[var(--app-warning)]">
            Смена типа товара
          </h3>

          <p className="mt-2 text-sm text-[var(--app-text)]">
            Смена типа может удалить несовместимые характеристики. Сначала
            проверьте последствия — предпросмотр не изменяет данные товара.
          </p>
        </div>
      </div>

      <div className="mt-5 min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
        <p className="text-xs text-[var(--app-muted)]">Текущий тип</p>

        <p className="mt-2 font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
          {product.productTypeName}
        </p>

        <p className="mt-1 font-mono text-xs text-[var(--app-muted)] [overflow-wrap:anywhere]">
          {product.productTypeCode}
        </p>
      </div>

      {productTypesQuery.isError && (
        <div
          role="alert"
          className="mt-4 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(productTypesQuery.error)}
        </div>
      )}

      <div className="mt-5 grid min-w-0 gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
        <div className="grid min-w-0 gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Новый тип товара
          </span>

          <AppSelect
            ariaLabel="Новый тип товара"
            value={targetProductTypeId}
            disabled={productTypesQuery.isLoading || previewMutation.isPending}
            onChange={(value) => {
              setTargetProductTypeId(value);
              previewMutation.reset();
            }}
            options={[
              {
                value: "",
                label: "Выберите новый тип",
              },

              ...availableProductTypes.map((productType) => ({
                value: productType.id,
                label: productType.name,
              })),
            ]}
          />
        </div>

        <AppButton
          type="button"
          variant="warning"
          disabled={!selectedTargetType || previewMutation.isPending}
          loading={previewMutation.isPending}
          onClick={handlePreview}
          className="w-full lg:w-auto"
        >
          {previewMutation.isPending
            ? "Анализируем..."
            : "Показать последствия"}
        </AppButton>
      </div>

      {previewMutation.isError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(previewMutation.error)}
        </div>
      )}

      {preview && (
        <div className="mt-6 grid min-w-0 gap-5">
          <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5">
            <p className="text-sm text-[var(--app-muted)]">
              Планируемое изменение
            </p>

            <div className="mt-3 flex min-w-0 flex-wrap items-center gap-3">
              <span className="min-w-0 font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                {preview.currentProductTypeName}
              </span>

              <span className="shrink-0 text-[var(--app-muted)]">→</span>

              <span className="min-w-0 font-medium text-[var(--app-accent)] [overflow-wrap:anywhere]">
                {preview.targetProductTypeName}
              </span>
            </div>
          </div>

          <div className="grid min-w-0 gap-3 sm:grid-cols-3">
            <SummaryCard
              label="Сохранятся"
              value={preview.preservedCharacteristics.length}
            />

            <SummaryCard
              label="Будут удалены"
              value={preview.removedCharacteristics.length}
            />

            <SummaryCard
              label="Нужно заполнить"
              value={preview.missingRequiredCharacteristics.length}
            />
          </div>

          <div
            role="status"
            className={[
              "rounded-2xl border p-4 text-sm",
              preview.canApplyWithoutAdditionalValues
                ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
                : "border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] text-[var(--app-accent)]",
            ].join(" ")}
          >
            {preview.canApplyWithoutAdditionalValues
              ? "Все обязательные характеристики нового типа уже заполнены."
              : "Перед применением миграции необходимо заполнить недостающие обязательные характеристики."}
          </div>

          <MigrationValueGroup
            title="Сохранятся"
            description="Эти определения разрешены новым типом. Их текущие значения будут сохранены."
            items={preview.preservedCharacteristics}
            emptyText="Совместимых заполненных характеристик нет."
            variant="preserved"
          />

          <MigrationValueGroup
            title="Будут удалены"
            description="Новый тип не разрешает эти характеристики. При применении миграции их значения будут удалены."
            items={preview.removedCharacteristics}
            emptyText="Несовместимых значений нет."
            variant="removed"
          />

          <MissingRequiredGroup
            items={preview.missingRequiredCharacteristics}
          />

          <ApplyProductTypeMigrationForm
            key={[
              preview.productVersion,
              preview.currentProductTypeId,
              preview.targetProductTypeId,

              ...preview.removedCharacteristics.map(
                (characteristic) => characteristic.definitionId,
              ),

              ...preview.missingRequiredCharacteristics.map(
                (characteristic) => characteristic.definitionId,
              ),
            ].join(":")}
            product={product}
            preview={preview}
          />
        </div>
      )}
    </section>
  );
}

function SummaryCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
      <p className="text-xs text-[var(--app-muted)]">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-[var(--app-text)]">
        {value}
      </p>
    </div>
  );
}

function MigrationValueGroup({
  title,
  description,
  items,
  emptyText,
  variant,
}: {
  title: string;
  description: string;
  items: ProductTypeMigrationCharacteristicValue[];
  emptyText: string;
  variant: "preserved" | "removed";
}) {
  const borderClass =
    variant === "preserved"
      ? "border-[var(--app-success-border)]"
      : "border-[var(--app-danger-border)]";

  const titleClass =
    variant === "preserved"
      ? "text-[var(--app-success)]"
      : "text-[var(--app-danger)]";

  return (
    <section
      className={`min-w-0 rounded-2xl border ${borderClass} bg-[var(--app-panel)] p-5`}
    >
      <h4 className={`font-semibold ${titleClass}`}>{title}</h4>

      <p className="mt-2 text-sm text-[var(--app-muted)]">{description}</p>

      {items.length === 0 ? (
        <p className="mt-4 text-sm text-[var(--app-muted)]">{emptyText}</p>
      ) : (
        <div className="mt-4 grid min-w-0 gap-3">
          {items.map((item) => (
            <div
              key={item.definitionId}
              className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
            >
              <div className="grid min-w-0 gap-3 sm:grid-cols-2 sm:items-start">
                <div className="min-w-0">
                  <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                    {item.name}
                    {item.unit ? `, ${item.unit}` : ""}
                  </p>

                  <p className="mt-1 font-mono text-xs text-[var(--app-muted)] [overflow-wrap:anywhere]">
                    {item.code} · {formatDataType(item.dataType)}
                  </p>
                </div>

                <p className="min-w-0 whitespace-pre-wrap text-sm font-medium text-[var(--app-text)] [overflow-wrap:anywhere] sm:text-right">
                  {formatValue(item.dataType, item.value)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function MissingRequiredGroup({
  items,
}: {
  items: ProductTypeMigrationMissingRequiredCharacteristic[];
}) {
  return (
    <section className="min-w-0 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-5">
      <h4 className="font-semibold text-[var(--app-text)]">
        Недостающие обязательные характеристики
      </h4>

      <p className="mt-2 text-sm text-[var(--app-muted)]">
        Эти значения потребуются перед применением нового типа.
      </p>

      {items.length === 0 ? (
        <p className="mt-4 text-sm text-[var(--app-success)]">
          Недостающих обязательных значений нет.
        </p>
      ) : (
        <div className="mt-4 grid min-w-0 gap-3">
          {items.map((item) => (
            <div
              key={item.definitionId}
              className="min-w-0 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4"
            >
              <p className="font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                {item.name}
                {item.unit ? `, ${item.unit}` : ""}
              </p>

              <p className="mt-1 font-mono text-xs text-[var(--app-muted)] [overflow-wrap:anywhere]">
                {item.code} · {formatDataType(item.dataType)}
              </p>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
