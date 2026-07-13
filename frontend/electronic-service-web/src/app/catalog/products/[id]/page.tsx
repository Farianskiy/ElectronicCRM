"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import { useParams } from "next/navigation";
import { getCatalogProductDetails } from "@/features/catalogProducts/api/getCatalogProductDetails";
import type {
  CatalogProductAlias,
  CatalogProductCharacteristic,
} from "@/features/catalogProducts/model/types";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { AppShell } from "@/widgets/appShell/AppShell";

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

function formatPrice(value?: number | null): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency: "RUB",
    maximumFractionDigits: 2,
  }).format(value);
}

function formatCharacteristicValue(
  characteristic: CatalogProductCharacteristic,
): string {
  const value = characteristic.value ?? "—";

  if (characteristic.unit) {
    return `${value} ${characteristic.unit}`;
  }

  return value;
}

function getAliasValue(alias: CatalogProductAlias): string {
  return alias.value ?? alias.name ?? alias.alias ?? "—";
}

export default function CatalogProductDetailsPage() {
  const params = useParams<{ id: string }>();
  const productId = params.id;

  const productQuery = useQuery({
    queryKey: ["catalog-product-details", productId],
    queryFn: () => getCatalogProductDetails(productId),
    enabled: Boolean(productId),
  });

  const product = productQuery.data;

  return (
    <RequireAuth>
      <AppShell
        title="Карточка товара"
        description="Полная информация о товаре, цене, остатке и характеристиках."
      >
        <div className="grid gap-6">
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
                      Артикул: {product.article ?? "—"}
                    </p>
                  </div>

                  <span
                    className={
                      product.isAvailable === false
                        ? "w-fit rounded-full bg-red-500/15 px-4 py-2 text-sm font-medium text-red-300"
                        : "w-fit rounded-full bg-green-500/15 px-4 py-2 text-sm font-medium text-green-300"
                    }
                  >
                    {product.isAvailable === false ? "Нет в наличии" : "В наличии"}
                  </span>
                </div>

                <div className="mt-6 grid gap-4 md:grid-cols-4">
                  <InfoCard
                    label="Производитель"
                    value={product.manufacturerName ?? product.manufacturer ?? "—"}
                  />

                  <InfoCard
                    label="Тип товара"
                    value={
                      product.productTypeName ??
                      product.productType ??
                      product.productTypeCode ??
                      "—"
                    }
                  />

                  <InfoCard label="Цена" value={formatPrice(product.price)} />

                  <InfoCard
                    label="Остаток"
                    value={
                      product.stockQuantity === null ||
                      product.stockQuantity === undefined
                        ? "—"
                        : `${product.stockQuantity}`
                    }
                  />
                </div>
              </section>

              <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
                <h2 className="text-xl font-semibold text-white">
                  Характеристики
                </h2>

                {!product.characteristics ||
                product.characteristics.length === 0 ? (
                  <p className="mt-4 text-sm text-slate-400">
                    Характеристики не пришли в ответе backend.
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
                            {characteristic.name ?? characteristic.code}
                          </p>
                          <p className="mt-1 text-xs text-slate-500">
                            {characteristic.code}
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

                {!product.aliases || product.aliases.length === 0 ? (
                  <p className="mt-4 text-sm text-slate-400">
                    Alias-ы не указаны.
                  </p>
                ) : (
                  <div className="mt-5 flex flex-wrap gap-2">
                    {product.aliases.map((alias, index) => (
                      <span
                        key={alias.id ?? `${getAliasValue(alias)}-${index}`}
                        className="rounded-full bg-teal-500/15 px-3 py-1 text-sm font-medium text-teal-300"
                      >
                        {getAliasValue(alias)}
                      </span>
                    ))}
                  </div>
                )}
              </section>
            </>
          )}
        </div>
      </AppShell>
    </RequireAuth>
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