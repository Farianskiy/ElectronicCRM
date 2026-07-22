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
    <section className="rounded-2xl border border-amber-500/25 bg-amber-500/[0.05] p-5">
      <div>
        <h3 className="font-semibold text-white">Смена типа товара</h3>

        <p className="mt-2 text-sm text-slate-400">
          Сначала будет построен план миграции. На этом шаге данные товара не
          изменяются.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">Текущий тип</p>

        <p className="mt-2 font-medium text-white">{product.productTypeName}</p>

        <p className="mt-1 font-mono text-xs text-slate-500">
          {product.productTypeCode}
        </p>
      </div>

      {productTypesQuery.isError && (
        <div className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(productTypesQuery.error)}
        </div>
      )}

      <div className="mt-5 grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
        <div className="grid gap-2">
          <span className="text-sm text-slate-300">Новый тип товара</span>

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

        <button
          type="button"
          disabled={!selectedTargetType || previewMutation.isPending}
          onClick={handlePreview}
          className="rounded-2xl bg-amber-500 px-5 py-3 text-sm font-medium text-slate-950 transition hover:bg-amber-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {previewMutation.isPending
            ? "Анализируем..."
            : "Показать последствия"}
        </button>
      </div>

      {previewMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(previewMutation.error)}
        </div>
      )}

      {preview && (
        <div className="mt-6 grid gap-5">
          <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
            <p className="text-sm text-slate-400">Планируемое изменение</p>

            <div className="mt-3 flex flex-wrap items-center gap-3">
              <span className="font-medium text-white">
                {preview.currentProductTypeName}
              </span>

              <span className="text-slate-500">→</span>

              <span className="font-medium text-amber-200">
                {preview.targetProductTypeName}
              </span>
            </div>
          </div>

          <div className="grid gap-3 sm:grid-cols-3">
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
            className={
              preview.canApplyWithoutAdditionalValues
                ? "rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200"
                : "rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200"
            }
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
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
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
    variant === "preserved" ? "border-green-500/20" : "border-red-500/20";

  return (
    <section className={`rounded-2xl border ${borderClass} bg-black/20 p-5`}>
      <h4 className="font-semibold text-white">{title}</h4>

      <p className="mt-2 text-sm text-slate-400">{description}</p>

      {items.length === 0 ? (
        <p className="mt-4 text-sm text-slate-500">{emptyText}</p>
      ) : (
        <div className="mt-4 grid gap-3">
          {items.map((item) => (
            <div
              key={item.definitionId}
              className="rounded-2xl border border-white/10 bg-white/[0.03] p-4"
            >
              <div className="flex flex-wrap justify-between gap-3">
                <div>
                  <p className="font-medium text-slate-100">
                    {item.name}
                    {item.unit ? `, ${item.unit}` : ""}
                  </p>

                  <p className="mt-1 font-mono text-xs text-slate-500">
                    {item.code} · {formatDataType(item.dataType)}
                  </p>
                </div>

                <p className="text-sm font-medium text-slate-200">
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
    <section className="rounded-2xl border border-amber-500/20 bg-black/20 p-5">
      <h4 className="font-semibold text-white">
        Недостающие обязательные характеристики
      </h4>

      <p className="mt-2 text-sm text-slate-400">
        Эти значения потребуются перед применением нового типа.
      </p>

      {items.length === 0 ? (
        <p className="mt-4 text-sm text-green-300">
          Недостающих обязательных значений нет.
        </p>
      ) : (
        <div className="mt-4 grid gap-3">
          {items.map((item) => (
            <div
              key={item.definitionId}
              className="rounded-2xl border border-amber-500/20 bg-amber-500/[0.05] p-4"
            >
              <p className="font-medium text-amber-100">
                {item.name}
                {item.unit ? `, ${item.unit}` : ""}
              </p>

              <p className="mt-1 font-mono text-xs text-slate-500">
                {item.code} · {formatDataType(item.dataType)}
              </p>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
