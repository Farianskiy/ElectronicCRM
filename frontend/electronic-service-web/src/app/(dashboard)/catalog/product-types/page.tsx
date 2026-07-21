"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { RequireTechnicalUser } from "@/features/auth/ui/RequireTechnicalUser";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { getCatalogProductTypeCharacteristicSchema } from "@/features/catalogProductTypes/api/getCatalogProductTypeCharacteristicSchema";
import type { CatalogProductTypeCharacteristicSchemaItem } from "@/features/catalogProductTypes/model/types";
import { AddOptionalCharacteristicToProductType } from "@/features/catalogProductTypes/ui/AddOptionalCharacteristicToProductType";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";
import { ProductTypeCharacteristicRequirednessControl } from "@/features/catalogProductTypes/ui/ProductTypeCharacteristicRequirednessControl";
import { ProductTypeCharacteristicRemovalControl } from "@/features/catalogProductTypes/ui/ProductTypeCharacteristicRemovalControl";

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

  return "Не удалось загрузить схему типа товара.";
}

function calculateCoveragePercent(
  productsWithValueCount: number,
  productsCount: number,
): number {
  if (productsCount <= 0) {
    return 0;
  }

  const coverage = (productsWithValueCount / productsCount) * 100;

  return Math.round(Math.min(Math.max(coverage, 0), 100));
}

function getCoverageBarClass(
  characteristic: CatalogProductTypeCharacteristicSchemaItem,
): string {
  if (characteristic.productsWithoutValueCount === 0) {
    return "bg-teal-400";
  }

  const coverage = calculateCoveragePercent(
    characteristic.productsWithValueCount,
    characteristic.productsWithValueCount +
      characteristic.productsWithoutValueCount,
  );

  if (coverage >= 75) {
    return "bg-amber-400";
  }

  return "bg-red-400";
}

function formatDataType(dataType: string): string {
  if (dataType === "Number") {
    return "Число";
  }

  if (dataType === "Boolean") {
    return "Да / Нет";
  }

  if (dataType === "Text") {
    return "Текст";
  }

  return dataType;
}

export default function CatalogProductTypesPage() {
  return (
    <RequireTechnicalUser>
      <CatalogProductTypesContent />
    </RequireTechnicalUser>
  );
}

function CatalogProductTypesContent() {
  const [selectedProductTypeCode, setSelectedProductTypeCode] = useState("");

  const [schemaSuccessMessage, setSchemaSuccessMessage] = useState<
    string | null
  >(null);

  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });

  const productTypes = productTypesQuery.data ?? [];

  /*
   * При первом открытии state остаётся пустым.
   * После загрузки типов автоматически используем
   * первый элемент, не вызывая setState в useEffect.
   */
  const effectiveProductTypeCode =
    selectedProductTypeCode || productTypes[0]?.code || "";

  const schemaQuery = useQuery({
    queryKey: [
      "catalog-product-type-characteristic-schema",
      effectiveProductTypeCode,
    ],

    queryFn: () =>
      getCatalogProductTypeCharacteristicSchema(effectiveProductTypeCode),

    enabled: effectiveProductTypeCode.length > 0,
    staleTime: 60 * 1000,
  });

  const schema = schemaQuery.data;

  const requiredCharacteristicsCount =
    schema?.characteristics.filter(
      (characteristic) => characteristic.isRequired,
    ).length ?? 0;

  const optionalCharacteristicsCount =
    schema?.characteristics.filter(
      (characteristic) => !characteristic.isRequired,
    ).length ?? 0;

  const incompleteRequiredCharacteristicsCount =
    schema?.characteristics.filter(
      (characteristic) =>
        characteristic.isRequired &&
        characteristic.productsWithoutValueCount > 0,
    ).length ?? 0;

  const removableCharacteristicsCount =
    schema?.characteristics.filter(
      (characteristic) => characteristic.canRemoveFromType,
    ).length ?? 0;

  const queryError = productTypesQuery.error ?? schemaQuery.error;

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Схемы типов товаров"
        description="Анализ характеристик, обязательности и заполненности данных для каждого типа товара."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div className="grid gap-5 lg:grid-cols-[minmax(280px,420px)_1fr] lg:items-end">
          <div className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Тип товара
            </span>

            <AppSelect
              ariaLabel="Тип товара"
              value={effectiveProductTypeCode}
              disabled={
                productTypesQuery.isLoading ||
                productTypesQuery.isError ||
                productTypes.length === 0
              }
              onChange={(value) => {
                setSelectedProductTypeCode(value);
                setSchemaSuccessMessage(null);
              }}
              options={productTypes.map((productType) => ({
                value: productType.code,
                label: productType.name,
              }))}
            />
          </div>

          <div className="rounded-2xl border border-teal-500/20 bg-teal-500/[0.06] px-5 py-4">
            <p className="text-sm font-medium text-teal-200">
              Управление схемой
            </p>

            <p className="mt-1 text-sm text-slate-400">
              Можно добавлять существующие характеристики и управлять их
              обязательностью. Перед включением обязательности backend повторно
              проверяет все товары выбранного типа.
            </p>
          </div>
        </div>
      </section>

      {queryError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getErrorMessage(queryError)}
        </section>
      )}

      {productTypesQuery.isLoading && (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем типы товаров...
        </section>
      )}

      {!productTypesQuery.isLoading &&
        !productTypesQuery.isError &&
        productTypes.length === 0 && (
          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Типы товаров не найдены
            </h2>

            <p className="mt-2 text-sm text-slate-400">
              В каталоге пока нет настроенных типов товаров.
            </p>
          </section>
        )}

      {schemaSuccessMessage && (
        <section className="rounded-3xl border border-green-500/30 bg-green-500/10 p-5 text-green-200">
          {schemaSuccessMessage}
        </section>
      )}

      {schemaQuery.isLoading && (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем схему выбранного типа...
        </section>
      )}

      {schema && (
        <>
          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
              <div>
                <p className="text-sm font-medium text-teal-300">
                  {schema.productTypeCode}
                </p>

                <h2 className="mt-1 text-2xl font-semibold text-white">
                  {schema.productTypeName}
                </h2>

                <p className="mt-2 text-sm text-slate-400">
                  Сводная информация о схеме и заполненности характеристик.
                </p>
              </div>

              {incompleteRequiredCharacteristicsCount > 0 && (
                <div className="rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
                  Обнаружены пропуски в обязательных характеристиках:{" "}
                  {incompleteRequiredCharacteristicsCount}
                </div>
              )}
            </div>

            <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
              <SummaryCard
                label="Товаров"
                value={schema.productsCount.toString()}
              />

              <SummaryCard
                label="Характеристик"
                value={schema.characteristics.length.toString()}
              />

              <SummaryCard
                label="Обязательных"
                value={requiredCharacteristicsCount.toString()}
              />

              <SummaryCard
                label="Необязательных"
                value={optionalCharacteristicsCount.toString()}
              />

              <SummaryCard
                label="Можно удалить из схемы"
                value={removableCharacteristicsCount.toString()}
              />
            </div>
          </section>

          <AddOptionalCharacteristicToProductType
            key={schema.productTypeCode}
            productTypeCode={schema.productTypeCode}
            productTypeName={schema.productTypeName}
          />

          {schema.characteristics.length === 0 ? (
            <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
              <h2 className="text-xl font-semibold text-white">
                Схема не настроена
              </h2>

              <p className="mt-2 text-sm text-slate-400">
                Для выбранного типа товара ещё не добавлены характеристики.
              </p>
            </section>
          ) : (
            <section className="grid gap-4">
              {schema.characteristics.map((characteristic) => (
                <CharacteristicSchemaCard
                  key={characteristic.definitionId}
                  productTypeCode={schema.productTypeCode}
                  productTypeName={schema.productTypeName}
                  characteristic={characteristic}
                  productsCount={schema.productsCount}
                  onCharacteristicRemoved={(characteristicName) => {
                    setSchemaSuccessMessage(
                      `Характеристика «${characteristicName}» удалена из схемы типа «${schema.productTypeName}».`,
                    );
                  }}
                />
              ))}
            </section>
          )}
        </>
      )}
    </div>
  );
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  );
}

function CharacteristicSchemaCard({
  productTypeCode,
  productTypeName,
  characteristic,
  productsCount,
  onCharacteristicRemoved,
}: {
  productTypeCode: string;
  productTypeName: string;

  characteristic: CatalogProductTypeCharacteristicSchemaItem;

  productsCount: number;

  onCharacteristicRemoved: (characteristicName: string) => void;
}) {
  const coveragePercent = calculateCoveragePercent(
    characteristic.productsWithValueCount,
    productsCount,
  );

  return (
    <article className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div className="flex flex-col justify-between gap-5 xl:flex-row xl:items-start">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-lg font-semibold text-white">
              {characteristic.name}
              {characteristic.unit ? `, ${characteristic.unit}` : ""}
            </h3>

            <span
              className={
                characteristic.isRequired
                  ? "rounded-full border border-amber-500/30 bg-amber-500/15 px-3 py-1 text-xs font-medium text-amber-300"
                  : "rounded-full border border-slate-500/30 bg-slate-500/10 px-3 py-1 text-xs font-medium text-slate-300"
              }
            >
              {characteristic.isRequired ? "Обязательная" : "Необязательная"}
            </span>
          </div>

          <p className="mt-2 break-all text-sm text-slate-500">
            {characteristic.code} · {formatDataType(characteristic.dataType)}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <SettingBadge
            enabled={characteristic.isFilterable}
            enabledLabel="Участвует в фильтрах"
            disabledLabel="Не участвует в фильтрах"
          />

          <SettingBadge
            enabled={characteristic.isUsedForReplacement}
            enabledLabel="Участвует в аналогах"
            disabledLabel="Не участвует в аналогах"
          />
        </div>
      </div>

      <div className="mt-6 grid gap-5 lg:grid-cols-[1.4fr_1fr]">
        <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <p className="text-sm text-slate-400">Заполненность</p>

              <p className="mt-1 text-xl font-semibold text-white">
                {characteristic.productsWithValueCount} из {productsCount}
              </p>
            </div>

            <p className="text-lg font-semibold text-teal-300">
              {coveragePercent}%
            </p>
          </div>

          <div className="mt-4 h-2 overflow-hidden rounded-full bg-white/[0.08]">
            <div
              className={`h-full rounded-full transition-all ${getCoverageBarClass(
                characteristic,
              )}`}
              style={{
                width: `${coveragePercent}%`,
              }}
            />
          </div>

          <div className="mt-4 flex flex-wrap gap-x-6 gap-y-2 text-sm">
            <span className="text-green-300">
              Заполнено: {characteristic.productsWithValueCount}
            </span>

            <span
              className={
                characteristic.productsWithoutValueCount > 0
                  ? "text-red-300"
                  : "text-slate-400"
              }
            >
              Не заполнено: {characteristic.productsWithoutValueCount}
            </span>
          </div>
        </div>

        <div className="grid gap-3">
          <ProductTypeCharacteristicRequirednessControl
            productTypeCode={productTypeCode}
            productTypeName={productTypeName}
            characteristic={characteristic}
          />

          <ProductTypeCharacteristicRemovalControl
            productTypeCode={productTypeCode}
            productTypeName={productTypeName}
            characteristic={characteristic}
            onRemoved={onCharacteristicRemoved}
          />
        </div>
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <DetailItem
          label="Режим подбора аналогов"
          value={characteristic.replacementMatchMode}
        />

        <DetailItem
          label="Вес при подборе"
          value={characteristic.replacementWeight.toString()}
        />

        <DetailItem
          label="Definition ID"
          value={characteristic.definitionId}
          monospace
        />
      </div>
    </article>
  );
}

function SettingBadge({
  enabled,
  enabledLabel,
  disabledLabel,
}: {
  enabled: boolean;
  enabledLabel: string;
  disabledLabel: string;
}) {
  return (
    <span
      className={
        enabled
          ? "rounded-full border border-teal-500/30 bg-teal-500/10 px-3 py-1 text-xs font-medium text-teal-300"
          : "rounded-full border border-white/10 bg-white/[0.03] px-3 py-1 text-xs font-medium text-slate-500"
      }
    >
      {enabled ? enabledLabel : disabledLabel}
    </span>
  );
}

function DetailItem({
  label,
  value,
  monospace = false,
}: {
  label: string;
  value: string;
  monospace?: boolean;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p
        className={[
          "mt-2 break-all text-sm text-slate-200",
          monospace ? "font-mono" : "",
        ].join(" ")}
      >
        {value}
      </p>
    </div>
  );
}
