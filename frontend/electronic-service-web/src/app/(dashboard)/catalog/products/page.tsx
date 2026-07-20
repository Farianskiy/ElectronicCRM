"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import type { FormEvent, ReactNode } from "react";
import { useState } from "react";
import { searchCatalogProducts } from "@/features/catalogProducts/api/searchCatalogProducts";
import { formatPrice } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import type {
  CatalogProductListItem,
  SearchProductCharacteristicRequest,
} from "@/features/catalogProducts/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";

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

  return "Произошла неизвестная ошибка.";
}

function ProductAvailabilityBadge({
  stockQuantity,
}: {
  stockQuantity: number;
}) {
  const isAvailable = stockQuantity > 0;

  return (
    <span
      className={
        isAvailable
          ? "rounded-full bg-green-500/15 px-3 py-1 text-xs font-medium text-green-300"
          : "rounded-full bg-red-500/15 px-3 py-1 text-xs font-medium text-red-300"
      }
    >
      {stockQuantity}
    </span>
  );
}

function ProductRow({ product }: { product: CatalogProductListItem }) {
  return (
    <tr className="bg-white/[0.02] transition hover:bg-white/[0.05]">
      <td className="px-4 py-4">
        <p className="font-medium text-white">{product.name}</p>
      </td>

      <td className="px-4 py-4 text-slate-300">{product.article}</td>

      <td className="px-4 py-4 text-slate-300">{product.manufacturerName}</td>

      <td className="px-4 py-4 text-slate-300">
        <p>{product.productTypeName}</p>
        <p className="mt-1 text-xs text-slate-500">{product.productTypeCode}</p>
      </td>

      <td className="px-4 py-4 text-slate-300">
        {formatPrice(product.priceAmount, product.priceCurrency)}
      </td>

      <td className="px-4 py-4">
        <ProductAvailabilityBadge stockQuantity={product.stockQuantity} />
      </td>

      <td className="px-4 py-4">
        <Link
          href={`/catalog/products/${product.id}`}
          className="rounded-xl bg-white/[0.06] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-teal-500 hover:text-white"
        >
          Открыть
        </Link>
      </td>
    </tr>
  );
}

interface AppliedCatalogFilters {
  search: string | null;
  productTypeCode: string | null;
  manufacturer: string | null;
  characteristics: SearchProductCharacteristicRequest[];
  onlyInStock: boolean | null;
}

const initialAppliedFilters: AppliedCatalogFilters = {
  search: null,
  productTypeCode: null,
  manufacturer: null,
  characteristics: [],
  onlyInStock: null,
};

interface SelectContainerProps {
  children: ReactNode;
}

function SelectContainer({ children }: SelectContainerProps) {
  return (
    <div className="relative">
      {children}

      <svg
        aria-hidden="true"
        viewBox="0 0 20 20"
        fill="none"
        className="pointer-events-none absolute right-4 top-1/2 size-4 -translate-y-1/2 text-slate-400"
      >
        <path
          d="m6 8 4 4 4-4"
          stroke="currentColor"
          strokeWidth="1.75"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </div>
  );
}

interface CharacteristicFilterFieldProps {
  characteristic: CatalogProductTypeCharacteristicMetadata;
  value: string;
  onChange: (value: string) => void;
}

function CharacteristicFilterField({
  characteristic,
  value,
  onChange,
}: CharacteristicFilterFieldProps) {
  const label = characteristic.unit
    ? `${characteristic.name}, ${characteristic.unit}`
    : characteristic.name;

  if (characteristic.dataType === "Boolean") {
    return (
      <label className="grid gap-2">
        <span className="text-sm font-medium text-slate-300">{label}</span>

        <AppSelect
          ariaLabel={label}
          value={value}
          onChange={onChange}
          options={[
            {
              value: "",
              label: "Любое значение",
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
      </label>
    );
  }

  return (
    <label className="grid gap-2">
      <span className="text-sm font-medium text-slate-300">{label}</span>

      <input
        type={characteristic.dataType === "Number" ? "number" : "text"}
        step={characteristic.dataType === "Number" ? "any" : undefined}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={
          characteristic.dataType === "Number"
            ? "Введите число"
            : "Введите значение"
        }
        className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
      />
    </label>
  );
}

export default function CatalogProductsPage() {
  const [search, setSearch] = useState("");
  const [productTypeCode, setProductTypeCode] = useState("");
  const [manufacturer, setManufacturer] = useState("");
  const [onlyInStock, setOnlyInStock] = useState(false);

  const [characteristicValues, setCharacteristicValues] = useState<
    Record<string, string>
  >({});

  const [appliedFilters, setAppliedFilters] = useState<AppliedCatalogFilters>(
    initialAppliedFilters,
  );

  const [page, setPage] = useState(1);
  const pageSize = 20;

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

  const characteristicsQuery = useQuery({
    queryKey: ["catalog-product-type-characteristics", productTypeCode],
    queryFn: () => getCatalogProductTypeCharacteristics(productTypeCode),
    enabled: productTypeCode.length > 0,
    staleTime: 5 * 60 * 1000,
  });

  const filterableCharacteristics =
    characteristicsQuery.data?.filter(
      (characteristic) => characteristic.isFilterable,
    ) ?? [];

  const productsQuery = useQuery({
    queryKey: ["catalog-products", appliedFilters, page, pageSize],
    queryFn: () =>
      searchCatalogProducts({
        ...appliedFilters,
        page,
        pageSize,
      }),
  });

  const products = productsQuery.data?.items ?? [];

  const totalCount = productsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const characteristics = filterableCharacteristics
      .map((characteristic) => ({
        code: characteristic.code,
        value: characteristicValues[characteristic.code]?.trim() ?? "",
      }))
      .filter((characteristic) => characteristic.value.length > 0);

    setAppliedFilters({
      search: search.trim() || null,
      productTypeCode: productTypeCode || null,
      manufacturer: manufacturer || null,
      characteristics,
      onlyInStock: onlyInStock ? true : null,
    });

    setPage(1);
  }

  function handleReset() {
    setSearch("");
    setProductTypeCode("");
    setManufacturer("");
    setOnlyInStock(false);
    setCharacteristicValues({});
    setAppliedFilters(initialAppliedFilters);
    setPage(1);
  }

  function handleCharacteristicChange(
    characteristicCode: string,
    value: string,
  ) {
    setCharacteristicValues((currentValues) => ({
      ...currentValues,
      [characteristicCode]: value,
    }));
  }

  function handleProductTypeChange(nextProductTypeCode: string) {
    setProductTypeCode(nextProductTypeCode);
    setCharacteristicValues({});
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Каталог товаров"
        description="Поиск и просмотр товаров, цен, остатков и характеристик."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <form onSubmit={handleSearch} className="grid gap-5">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Поиск товара
            </span>

            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Название, артикул, тип или производитель"
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
            />
          </label>

          <div className="grid gap-4 md:grid-cols-2">
            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Тип товара
              </span>

              <AppSelect
                ariaLabel="Тип товара"
                value={productTypeCode}
                disabled={productTypesQuery.isLoading}
                onChange={handleProductTypeChange}
                options={[
                  {
                    value: "",
                    label: productTypesQuery.isLoading
                      ? "Загружаем типы..."
                      : "Все типы",
                    disabled: productTypesQuery.isLoading,
                  },
                  ...(productTypesQuery.data ?? []).map((productType) => ({
                    value: productType.code,
                    label: productType.name,
                  })),
                ]}
              />
            </label>

            <label className="grid gap-2">
              <span className="text-sm font-medium text-slate-300">
                Производитель
              </span>

              <AppSelect
                ariaLabel="Производитель"
                value={manufacturer}
                disabled={manufacturersQuery.isLoading}
                onChange={setManufacturer}
                options={[
                  {
                    value: "",
                    label: manufacturersQuery.isLoading
                      ? "Загружаем производителей..."
                      : "Все производители",
                    disabled: manufacturersQuery.isLoading,
                  },
                  ...(manufacturersQuery.data ?? []).map(
                    (manufacturerItem) => ({
                      value: manufacturerItem.name,
                      label: manufacturerItem.name,
                    }),
                  ),
                ]}
              />
            </label>
          </div>

          {productTypeCode && (
            <section className="rounded-2xl border border-white/10 bg-black/20 p-4">
              <h3 className="text-sm font-semibold text-white">
                Характеристики
              </h3>

              {characteristicsQuery.isLoading ? (
                <p className="mt-3 text-sm text-slate-400">
                  Загружаем характеристики...
                </p>
              ) : characteristicsQuery.isError ? (
                <p className="mt-3 rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
                  {getErrorMessage(characteristicsQuery.error)}
                </p>
              ) : filterableCharacteristics.length === 0 ? (
                <p className="mt-3 text-sm text-slate-400">
                  Для этого типа нет характеристик, разрешённых для фильтрации.
                </p>
              ) : (
                <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                  {filterableCharacteristics.map((characteristic) => (
                    <CharacteristicFilterField
                      key={characteristic.id}
                      characteristic={characteristic}
                      value={characteristicValues[characteristic.code] ?? ""}
                      onChange={(value) =>
                        handleCharacteristicChange(characteristic.code, value)
                      }
                    />
                  ))}
                </div>
              )}
            </section>
          )}

          <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
            <label className="flex items-center gap-3 rounded-2xl border border-white/10 bg-black/20 px-4 py-3">
              <input
                type="checkbox"
                checked={onlyInStock}
                onChange={(event) => setOnlyInStock(event.target.checked)}
              />

              <span className="text-sm text-slate-300">Только в наличии</span>
            </label>

            <div className="flex gap-3">
              <button
                type="submit"
                className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400"
              >
                Найти
              </button>

              <button
                type="button"
                onClick={handleReset}
                className="rounded-2xl bg-white/[0.06] px-5 py-3 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1]"
              >
                Сбросить
              </button>
            </div>
          </div>
        </form>
      </section>

      {productsQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-5 text-red-200">
          {getErrorMessage(productsQuery.error)}
        </section>
      )}

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
          <div>
            <h2 className="text-xl font-semibold text-white">Товары</h2>
            <p className="mt-1 text-sm text-slate-400">
              Найдено backend-ом: {totalCount}. Показано на странице:{" "}
              {products.length}
            </p>
          </div>

          <p className="text-sm text-slate-400">
            Страница {page} из {totalPages}
          </p>
        </div>

        {productsQuery.isLoading ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5 text-slate-300">
            Загружаем товары...
          </div>
        ) : products.length === 0 ? (
          <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5">
            <h3 className="text-lg font-semibold text-white">
              Ничего не найдено
            </h3>
            <p className="mt-2 text-sm text-slate-400">
              Попробуй изменить запрос или отключить фильтр наличия.
            </p>
          </div>
        ) : (
          <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10">
            <table className="w-full min-w-[980px] border-collapse text-left text-sm">
              <thead className="bg-black/30 text-slate-400">
                <tr>
                  <th className="px-4 py-3 font-medium">Наименование</th>
                  <th className="px-4 py-3 font-medium">Артикул</th>
                  <th className="px-4 py-3 font-medium">Производитель</th>
                  <th className="px-4 py-3 font-medium">Тип</th>
                  <th className="px-4 py-3 font-medium">Цена</th>
                  <th className="px-4 py-3 font-medium">Остаток</th>
                  <th className="px-4 py-3 font-medium">Действие</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-white/10">
                {products.map((product) => (
                  <ProductRow key={product.id} product={product} />
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="mt-5 flex items-center justify-between">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 disabled:opacity-40"
          >
            Назад
          </button>

          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() =>
              setPage((current) => Math.min(totalPages, current + 1))
            }
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 disabled:opacity-40"
          >
            Вперёд
          </button>
        </div>
      </section>
    </div>
  );
}
