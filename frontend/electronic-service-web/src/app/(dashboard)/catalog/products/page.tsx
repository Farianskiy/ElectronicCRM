"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import type { FormEvent } from "react";
import { useState } from "react";
import { searchCatalogProducts } from "@/features/catalogProducts/api/searchCatalogProducts";
import { formatPrice } from "@/shared/lib/formatters";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import type {
  CatalogProductListItem,
  SearchProductCharacteristicRequest,
} from "@/features/catalogProducts/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";
import { AppInput } from "@/shared/ui/AppInput";
import { AppButton } from "@/shared/ui/AppButton";
import { VoiceInputButton } from "@/shared/ui/VoiceInputButton";
import { importCatalogStock } from "@/features/catalogStockImport/api/importCatalogStock";

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
      title={isAvailable ? "В наличии" : "Нет в наличии"}
      className={[
        "inline-flex items-center rounded-full border px-3 py-1",
        "text-xs font-semibold tabular-nums",
        isAvailable
          ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
          : "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]",
      ].join(" ")}
    >
      {stockQuantity}
    </span>
  );
}

function ProductRow({ product }: { product: CatalogProductListItem }) {
  return (
    <tr className="bg-[var(--app-panel)] transition-colors hover:bg-[var(--app-panel-hover)] motion-reduce:transition-none">
      <td className="min-w-[240px] max-w-[420px] px-4 py-4">
        <p className="break-words font-medium text-[var(--app-text)]">
          {product.name}
        </p>
      </td>

      <td className="px-4 py-4 text-[var(--app-muted)]">{product.article}</td>

      <td className="px-4 py-4 text-[var(--app-muted)]">
        {product.manufacturerName}
      </td>

      <td className="px-4 py-4">
        <p className="text-[var(--app-text)]">{product.productTypeName}</p>

        <p className="mt-1 break-words text-xs text-[var(--app-muted)]">
          {product.productTypeCode}
        </p>
      </td>

      <td className="whitespace-nowrap px-4 py-4 tabular-nums text-[var(--app-text)]">
        {formatPrice(product.priceAmount, product.priceCurrency)}
      </td>

      <td className="px-4 py-4">
        <ProductAvailabilityBadge stockQuantity={product.stockQuantity} />
      </td>

      <td className="px-4 py-4">
        <Link
          href={`/catalog/products/${product.id}`}
          aria-label={`Открыть товар: ${product.name}`}
          className="inline-flex min-h-10 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-xs font-semibold text-[var(--app-text)] transition-colors hover:border-[var(--app-accent-border)] hover:bg-[var(--app-accent-soft)] hover:text-[var(--app-accent)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none"
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
      <label className="grid min-w-0 content-start gap-2">
        <span className="text-sm font-medium text-[var(--app-text)]">
          {label}
        </span>

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
    <label className="grid min-w-0 content-start gap-2">
      <span className="text-sm font-medium text-[var(--app-text)]">
        {label}
      </span>

      <AppInput
        type={characteristic.dataType === "Number" ? "number" : "text"}
        step={characteristic.dataType === "Number" ? "any" : undefined}
        value={value}
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

export default function CatalogProductsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [productTypeCode, setProductTypeCode] = useState("");
  const [manufacturer, setManufacturer] = useState("");
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [stockManufacturerId, setStockManufacturerId] = useState("");
  const [stockFile, setStockFile] = useState<File | null>(null);

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

  const stockImportMutation = useMutation({
    mutationFn: importCatalogStock,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["catalog-products"],
      });
    },
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

  function handleStockImport(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!stockManufacturerId || !stockFile) {
      return;
    }

    stockImportMutation.mutate({
      manufacturerId: stockManufacturerId,
      file: stockFile,
    });
  }

  return (
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Каталог товаров"
      description="Поиск и просмотр товаров, цен, остатков и характеристик."
      contentClassName="grid min-w-0 gap-6"
    >
      <section
        aria-labelledby="catalog-stock-import-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div>
          <h2
            id="catalog-stock-import-title"
            className="text-lg font-semibold text-[var(--app-text)]"
          >
            Загрузка остатков
          </h2>

          <p className="mt-1 text-sm leading-6 text-[var(--app-muted)]">
            Выберите производителя и XLSX-файл с колонками «Артикул» и
            «Остаток». Обновятся только найденные товары; остальные строки и
            товары будут пропущены.
          </p>
        </div>

        <form
          onSubmit={handleStockImport}
          className="mt-5 grid min-w-0 gap-4 lg:grid-cols-[minmax(220px,1fr)_minmax(280px,2fr)_auto] lg:items-end"
        >
          <label className="grid min-w-0 content-start gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Производитель
            </span>

            <AppSelect
              ariaLabel="Производитель для загрузки остатков"
              value={stockManufacturerId}
              disabled={manufacturersQuery.isLoading}
              onChange={setStockManufacturerId}
              options={[
                {
                  value: "",
                  label: manufacturersQuery.isLoading
                    ? "Загружаем производителей..."
                    : "Выберите производителя",
                  disabled: manufacturersQuery.isLoading,
                },
                ...(manufacturersQuery.data ?? []).map((manufacturerItem) => ({
                  value: manufacturerItem.id,
                  label: manufacturerItem.name,
                })),
              ]}
            />
          </label>

          <label className="grid min-w-0 content-start gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Файл остатков
            </span>

            <input
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              onChange={(event) => {
                setStockFile(event.target.files?.[0] ?? null);
                stockImportMutation.reset();
              }}
              className="min-h-11 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-sm text-[var(--app-text)] file:mr-3 file:rounded-lg file:border-0 file:bg-[var(--app-accent-soft)] file:px-3 file:py-1.5 file:font-semibold file:text-[var(--app-accent)]"
            />
          </label>

          <AppButton
            type="submit"
            variant="primary"
            loading={stockImportMutation.isPending}
            disabled={!stockManufacturerId || !stockFile}
          >
            Загрузить остатки
          </AppButton>
        </form>

        {stockImportMutation.isError && (
          <div
            role="alert"
            className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
          >
            {getErrorMessage(stockImportMutation.error)}
          </div>
        )}

        {stockImportMutation.data && (
          <div
            role="status"
            className="mt-4 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4"
          >
            <p className="font-semibold text-[var(--app-success)]">
              Остатки загружены
            </p>

            <p className="mt-2 text-sm leading-6 text-[var(--app-text)]">
              Прочитано строк: {stockImportMutation.data.readRowsCount}. Совпало
              по артикулу: {stockImportMutation.data.matchedRowsCount}. Изменено
              товаров: {stockImportMutation.data.updatedProductsCount}.
              Пропущено: {stockImportMutation.data.skippedRowsCount}.
            </p>

            {stockImportMutation.data.issues.length > 0 && (
              <details className="mt-3 text-sm text-[var(--app-muted)]">
                <summary className="cursor-pointer font-medium text-[var(--app-text)]">
                  Показать примеры пропущенных строк
                </summary>

                <ul className="mt-2 grid gap-1 pl-5">
                  {stockImportMutation.data.issues.map((issue) => (
                    <li
                      key={`${issue.rowNumber}-${issue.article}-${issue.message}`}
                    >
                      Строка {issue.rowNumber}
                      {issue.article ? `, артикул ${issue.article}` : ""}:{" "}
                      {issue.message}
                    </li>
                  ))}
                </ul>
              </details>
            )}
          </div>
        )}
      </section>

      <section
        aria-labelledby="catalog-filters-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="mb-5">
          <h2
            id="catalog-filters-title"
            className="text-lg font-semibold text-[var(--app-text)]"
          >
            Поиск и фильтры
          </h2>

          <p className="mt-1 text-sm leading-6 text-[var(--app-muted)]">
            Задайте условия и нажмите «Найти», чтобы обновить список товаров.
          </p>
        </div>

        <form onSubmit={handleSearch} className="grid min-w-0 gap-5">
          <div className="grid min-w-0 gap-4 md:grid-cols-2 xl:grid-cols-4">
            <div className="grid min-w-0 content-start gap-2 md:col-span-2">
              <label
                htmlFor="catalog-product-search"
                className="text-sm font-medium text-[var(--app-text)]"
              >
                Поиск товара
              </label>

              <div className="grid min-w-0 gap-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-start">
                <AppInput
                  id="catalog-product-search"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Название, артикул, тип или производитель"
                />

                <VoiceInputButton
                  disabled={productsQuery.isFetching}
                  onTranscript={(transcript) => {
                    setSearch(transcript);
                  }}
                />
              </div>
            </div>

            <label className="grid min-w-0 content-start gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
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

            <label className="grid min-w-0 content-start gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
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
            <section
              aria-labelledby="catalog-characteristic-filters-title"
              className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-4 sm:p-5"
            >
              <h3
                id="catalog-characteristic-filters-title"
                className="text-sm font-semibold text-[var(--app-text)]"
              >
                Характеристики
              </h3>

              {characteristicsQuery.isLoading ? (
                <p
                  role="status"
                  className="mt-3 text-sm text-[var(--app-muted)]"
                >
                  Загружаем характеристики...
                </p>
              ) : characteristicsQuery.isError ? (
                <p
                  role="alert"
                  className="mt-3 rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]"
                >
                  {getErrorMessage(characteristicsQuery.error)}
                </p>
              ) : filterableCharacteristics.length === 0 ? (
                <p className="mt-3 text-sm leading-6 text-[var(--app-muted)]">
                  Для этого типа нет характеристик, разрешённых для фильтрации.
                </p>
              ) : (
                <div className="mt-4 grid min-w-0 gap-4 md:grid-cols-2 xl:grid-cols-3">
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

          <div className="flex flex-col gap-4 border-t border-[var(--app-border)] pt-4 sm:flex-row sm:items-center sm:justify-between">
            <label className="flex min-h-11 cursor-pointer items-center gap-3 self-start rounded-xl px-2 py-2">
              <input
                type="checkbox"
                checked={onlyInStock}
                onChange={(event) => setOnlyInStock(event.target.checked)}
                className="h-4 w-4 shrink-0 cursor-pointer accent-[var(--app-accent-strong)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--app-panel)]"
              />

              <span className="text-sm font-medium text-[var(--app-text)]">
                Только в наличии
              </span>
            </label>

            <div className="grid grid-cols-2 gap-3 sm:flex sm:items-center">
              <AppButton type="submit" variant="primary">
                Найти
              </AppButton>

              <AppButton
                type="button"
                variant="secondary"
                onClick={handleReset}
              >
                Сбросить
              </AppButton>
            </div>
          </div>
        </form>
      </section>

      <section
        aria-labelledby="catalog-results-title"
        className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
      >
        <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
          <div>
            <h2
              id="catalog-results-title"
              className="text-xl font-semibold text-[var(--app-text)]"
            >
              Товары
            </h2>

            {productsQuery.data && (
              <p className="mt-1 text-sm tabular-nums text-[var(--app-muted)]">
                Найдено: {totalCount}. На странице: {products.length}.
              </p>
            )}
          </div>

          {productsQuery.isFetching && productsQuery.data && (
            <p role="status" className="text-sm text-[var(--app-muted)]">
              Обновляем список...
            </p>
          )}
        </div>

        {productsQuery.isError && (
          <div
            role="alert"
            className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4"
          >
            <h3 className="text-sm font-semibold text-[var(--app-danger)]">
              Не удалось загрузить товары
            </h3>

            <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-[var(--app-danger)]">
              {getErrorMessage(productsQuery.error)}
            </p>

            {products.length > 0 && (
              <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
                Ниже показаны ранее загруженные данные. Они могут быть
                неактуальны.
              </p>
            )}
          </div>
        )}

        {productsQuery.isLoading ? (
          <div
            role="status"
            className="mt-5 flex min-h-40 items-center justify-center gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-6"
          >
            <span
              aria-hidden="true"
              className="h-5 w-5 shrink-0 animate-spin rounded-full border-2 border-[var(--app-accent-border)] border-t-[var(--app-accent)] motion-reduce:animate-none"
            />

            <p className="text-sm text-[var(--app-muted)]">
              Загружаем товары...
            </p>
          </div>
        ) : products.length > 0 ? (
          <div
            role="region"
            aria-label="Таблица товаров с горизонтальной прокруткой"
            tabIndex={0}
            className="mt-5 min-w-0 max-w-full overflow-x-auto rounded-2xl border border-[var(--app-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--app-accent)]"
          >
            <table className="w-full min-w-[980px] border-collapse text-left text-sm">
              <caption className="sr-only">Результаты поиска товаров</caption>

              <thead className="bg-[var(--app-surface)] text-[var(--app-muted)]">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Наименование
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Артикул
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Производитель
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Тип
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Цена
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Остаток
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Действие
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-[var(--app-border)]">
                {products.map((product) => (
                  <ProductRow key={product.id} product={product} />
                ))}
              </tbody>
            </table>
          </div>
        ) : productsQuery.isSuccess ? (
          <div
            role="status"
            className="mt-5 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] px-5 py-10 text-center"
          >
            <h3 className="text-base font-semibold text-[var(--app-text)]">
              Ничего не найдено
            </h3>

            <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-[var(--app-muted)]">
              Попробуйте изменить поисковый запрос, выбрать другой тип товара
              или производителя либо отключить фильтр наличия.
            </p>
          </div>
        ) : null}

        <nav
          aria-label="Страницы каталога товаров"
          className="mt-5 grid grid-cols-2 items-center gap-3 border-t border-[var(--app-border)] pt-4 sm:flex sm:justify-between"
        >
          <AppButton
            type="button"
            variant="secondary"
            disabled={page <= 1}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
          >
            Назад
          </AppButton>

          <p className="order-first col-span-2 text-center text-sm tabular-nums text-[var(--app-muted)] sm:order-none">
            {productsQuery.data
              ? `Страница ${page} из ${totalPages}`
              : `Страница ${page}`}
          </p>

          <AppButton
            type="button"
            variant="secondary"
            disabled={page >= totalPages}
            onClick={() =>
              setPage((current) => Math.min(totalPages, current + 1))
            }
          >
            Вперёд
          </AppButton>
        </nav>
      </section>
    </PageWorkspace>
  );
}
