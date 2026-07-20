"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getCatalogProductDetails } from "@/features/catalogProducts/api/getCatalogProductDetails";
import type { CatalogProductCharacteristic } from "@/features/catalogProducts/model/types";
import { TechnicalProductEditor } from "@/features/catalogProducts/ui/TechnicalProductEditor";
import { isTechnicalUser } from "@/shared/api/authToken";
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

function formatCharacteristicValue(
  characteristic: CatalogProductCharacteristic,
): string {
  if (characteristic.unit) {
    return `${characteristic.value} ${characteristic.unit}`;
  }

  return characteristic.value;
}

function getProductIdFromParams(params: ReturnType<typeof useParams>): string {
  const id = params.id;

  if (typeof id === "string") {
    return id;
  }

  if (Array.isArray(id)) {
    return id[0] ?? "";
  }

  return "";
}

export default function CatalogProductDetailsPage() {
  const params = useParams();
  const productId = getProductIdFromParams(params);

  const session = useAuthSession();
  const canEditProduct = isTechnicalUser(session);

  const [isEditorOpen, setIsEditorOpen] = useState(false);

  const productQuery = useQuery({
    queryKey: ["catalog-product-details", productId],
    queryFn: () => getCatalogProductDetails(productId),
    enabled: productId.length > 0,
  });

  const product = productQuery.data;

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Карточка товара"
        description="Полная информация о товаре, цене, остатке и характеристиках."
      />

      <div>
        <Link
          href="/catalog/products"
          className="inline-flex rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1]"
        >
          ← Назад к каталогу
        </Link>
      </div>

      {productQuery.isLoading && (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем карточку товара...
        </section>
      )}

      {productQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getErrorMessage(productQuery.error)}
        </section>
      )}

      {product && (
        <>
          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
              <div>
                <h1 className="text-3xl font-bold text-white">
                  {product.name}
                </h1>

                <p className="mt-2 text-sm text-slate-400">
                  Артикул: {product.article}
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <span
                  className={
                    product.stockQuantity > 0
                      ? "w-fit rounded-full bg-green-500/15 px-4 py-2 text-sm font-medium text-green-300"
                      : "w-fit rounded-full bg-red-500/15 px-4 py-2 text-sm font-medium text-red-300"
                  }
                >
                  {product.stockQuantity > 0 ? "В наличии" : "Нет в наличии"}
                </span>

                {canEditProduct && (
                  <button
                    type="button"
                    onClick={() => setIsEditorOpen((current) => !current)}
                    className="rounded-2xl border border-teal-500/30 bg-teal-500/10 px-4 py-2 text-sm font-medium text-teal-200 transition hover:bg-teal-500/20"
                  >
                    {isEditorOpen ? "Закрыть редактор" : "Редактировать товар"}
                  </button>
                )}
              </div>
            </div>

            <div className="mt-6 grid gap-4 md:grid-cols-4">
              <InfoCard
                label="Производитель"
                value={product.manufacturerName}
              />
              <InfoCard label="Тип товара" value={product.productTypeName} />
              <InfoCard label="Код типа" value={product.productTypeCode} />
              <InfoCard
                label="Цена"
                value={formatPrice(product.priceAmount, product.priceCurrency)}
              />
              <InfoCard label="Остаток" value={`${product.stockQuantity}`} />
            </div>
          </section>

          {canEditProduct && isEditorOpen && (
            <TechnicalProductEditor product={product} />
          )}

          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">Характеристики</h2>

            {product.characteristics.length === 0 ? (
              <p className="mt-4 text-sm text-slate-400">
                Характеристики не указаны.
              </p>
            ) : (
              <div className="mt-5 grid gap-3 md:grid-cols-2">
                {product.characteristics.map((characteristic) => (
                  <div
                    key={`${characteristic.code}-${characteristic.value}`}
                    className="flex justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 px-4 py-3"
                  >
                    <div>
                      <p className="text-sm font-medium text-slate-200">
                        {characteristic.name}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        {characteristic.code} · {characteristic.dataType}
                      </p>
                    </div>

                    <p className="text-right text-sm font-semibold text-white">
                      {formatCharacteristicValue(characteristic)}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </section>

          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Альтернативные названия
            </h2>

            {product.aliases.length === 0 ? (
              <p className="mt-4 text-sm text-slate-400">Alias-ы не указаны.</p>
            ) : (
              <div className="mt-5 flex flex-wrap gap-2">
                {product.aliases.map((alias) => (
                  <span
                    key={alias}
                    className="rounded-full bg-teal-500/15 px-3 py-1 text-sm font-medium text-teal-300"
                  >
                    {alias}
                  </span>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>
      <p className="mt-1 text-lg font-semibold text-white">{value}</p>
    </div>
  );
}
