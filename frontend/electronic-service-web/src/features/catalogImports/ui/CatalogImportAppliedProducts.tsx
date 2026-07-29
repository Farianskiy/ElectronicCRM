"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatPrice } from "@/shared/lib/formatters";
import { getCatalogImportAppliedProducts } from "../api/getCatalogImportAppliedProducts";
import { catalogImportQueryKeys } from "../model/queryKeys";

interface CatalogImportAppliedProductsProps {
  batchId: string;
}

const pageSize = 25;

export function CatalogImportAppliedProducts({
  batchId,
}: CatalogImportAppliedProductsProps) {
  const [page, setPage] = useState(1);

  const productsQuery = useQuery({
    queryKey: catalogImportQueryKeys.appliedProducts(batchId, page, pageSize),
    queryFn: () =>
      getCatalogImportAppliedProducts({
        batchId,
        page,
        pageSize,
      }),
    placeholderData: (previousData) => previousData,
  });

  const products = productsQuery.data?.items ?? [];
  const totalCount = productsQuery.data?.totalCount ?? 0;
  const backendTotalPages = productsQuery.data?.totalPages ?? 0;
  const totalPages = Math.max(1, backendTotalPages);

  return (
    <section className="rounded-3xl border border-green-500/20 bg-green-500/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div>
          <h2 className="text-xl font-semibold text-white">Созданные товары</h2>

          <p className="mt-2 text-sm leading-6 text-slate-400">
            Товары, добавленные в каталог при применении этого пакета.
          </p>

          <p className="mt-2 text-sm text-green-200">
            Всего создано: {totalCount}
          </p>
        </div>

        <Link
          href="/catalog/products"
          className="rounded-xl border border-green-500/30 bg-green-500/10 px-4 py-2 text-sm font-medium text-green-200 transition hover:bg-green-500/20"
        >
          Открыть весь каталог
        </Link>
      </div>

      {productsQuery.isError && (
        <div className="mt-6 rounded-2xl border border-red-500/30 bg-red-500/10 p-5 text-sm text-red-200">
          {getApiErrorMessage(
            productsQuery.error,
            "Не удалось загрузить созданные товары.",
          )}
        </div>
      )}

      {productsQuery.isLoading ? (
        <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5 text-slate-300">
          Загружаем созданные товары...
        </div>
      ) : products.length === 0 ? (
        <div className="mt-6 rounded-2xl border border-white/10 bg-black/20 p-5">
          <h3 className="font-semibold text-white">Товары не найдены</h3>

          <p className="mt-2 text-sm text-slate-400">
            Пакет имеет статус Applied, но связанные записи аудита отсутствуют.
          </p>
        </div>
      ) : (
        <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10">
          <table className="w-full min-w-[1150px] border-collapse text-left text-sm">
            <thead className="bg-black/30 text-slate-400">
              <tr>
                <th className="px-4 py-3 font-medium">Наименование</th>

                <th className="px-4 py-3 font-medium">Артикул</th>

                <th className="px-4 py-3 font-medium">Производитель</th>

                <th className="px-4 py-3 font-medium">Тип</th>

                <th className="px-4 py-3 font-medium">Цена</th>

                <th className="px-4 py-3 font-medium">Остаток</th>

                <th className="px-4 py-3 font-medium">Применён</th>

                <th className="px-4 py-3 font-medium">Действие</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-white/10">
              {products.map((product) => (
                <tr
                  key={product.productId}
                  className="bg-white/[0.01] transition hover:bg-white/[0.04]"
                >
                  <td className="max-w-80 px-4 py-4">
                    <p className="break-words font-medium text-white">
                      {product.name}
                    </p>

                    <p className="mt-1 break-all text-xs text-slate-600">
                      {product.productId}
                    </p>
                  </td>

                  <td className="px-4 py-4 text-slate-300">
                    {product.article}
                  </td>

                  <td className="px-4 py-4 text-slate-300">
                    {product.manufacturerName}
                  </td>

                  <td className="px-4 py-4">
                    <p className="text-slate-200">{product.productTypeName}</p>

                    <p className="mt-1 text-xs text-slate-500">
                      {product.productTypeCode}
                    </p>
                  </td>

                  <td className="whitespace-nowrap px-4 py-4 text-slate-300">
                    {formatPrice(product.priceAmount, product.priceCurrency)}
                  </td>

                  <td className="px-4 py-4">
                    <span
                      className={[
                        "rounded-full px-3 py-1",
                        "text-xs font-medium",
                        product.stockQuantity > 0
                          ? "bg-green-500/15 text-green-300"
                          : "bg-red-500/15 text-red-300",
                      ].join(" ")}
                    >
                      {product.stockQuantity}
                    </span>
                  </td>

                  <td className="whitespace-nowrap px-4 py-4 text-slate-400">
                    {formatDate(product.appliedAtUtc)}
                  </td>

                  <td className="px-4 py-4">
                    <Link
                      href={`/catalog/products/${product.productId}`}
                      className="rounded-xl bg-white/[0.06] px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-teal-500 hover:text-white"
                    >
                      Открыть товар
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {totalCount > 0 && (
        <div className="mt-5 flex items-center justify-between">
          <button
            type="button"
            disabled={page <= 1 || productsQuery.isFetching}
            onClick={() =>
              setPage((currentPage) => Math.max(1, currentPage - 1))
            }
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Назад
          </button>

          <p className="text-sm text-slate-400">
            Страница {page} из {totalPages}
          </p>

          <button
            type="button"
            disabled={
              page >= totalPages ||
              backendTotalPages === 0 ||
              productsQuery.isFetching
            }
            onClick={() =>
              setPage((currentPage) => Math.min(totalPages, currentPage + 1))
            }
            className="rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1] disabled:cursor-not-allowed disabled:opacity-40"
          >
            Вперёд
          </button>
        </div>
      )}
    </section>
  );
}
