"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import type { FormEvent } from "react";
import { useState } from "react";
import { getCatalogProducts } from "@/features/catalogProducts/api/getCatalogProducts";
import type { CatalogProductListItem } from "@/features/catalogProducts/model/types";
import { formatPrice } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";

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

      <td className="px-4 py-4 text-slate-300">
        {product.manufacturerName}
      </td>

      <td className="px-4 py-4 text-slate-300">
        <p>{product.productTypeName}</p>
        <p className="mt-1 text-xs text-slate-500">
          {product.productTypeCode}
        </p>
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

export default function CatalogProductsPage() {
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [onlyInStock, setOnlyInStock] = useState(false);
  const [appliedOnlyInStock, setAppliedOnlyInStock] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  const productsQuery = useQuery({
    queryKey: ["catalog-products", appliedSearch, page, pageSize],
    queryFn: () =>
      getCatalogProducts({
        search: appliedSearch,
        page,
        pageSize,
      }),
  });

  const rawProducts = productsQuery.data?.items ?? [];

  const products = appliedOnlyInStock
    ? rawProducts.filter((product) => product.stockQuantity > 0)
    : rawProducts;

  const totalCount = productsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setPage(1);
    setAppliedSearch(search);
    setAppliedOnlyInStock(onlyInStock);
  }

  function handleReset() {
    setSearch("");
    setAppliedSearch("");
    setOnlyInStock(false);
    setAppliedOnlyInStock(false);
    setPage(1);
  }

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Каталог товаров"
        description="Поиск и просмотр товаров, цен, остатков и характеристик."
      />

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-5">
        <form
          onSubmit={handleSearch}
          className="grid gap-4 lg:grid-cols-[1fr_auto_auto]"
        >
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">
              Поиск товара
            </span>
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Например: автомат иэк армат 1п 16а"
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
            />
          </label>

          <label className="flex items-center gap-3 self-end rounded-2xl border border-white/10 bg-black/20 px-4 py-3">
            <input
              type="checkbox"
              checked={onlyInStock}
              onChange={(event) => setOnlyInStock(event.target.checked)}
            />
            <span className="text-sm text-slate-300">Только в наличии</span>
          </label>

          <div className="flex items-end gap-3">
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