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
import { PageWorkspace } from "@/shared/ui/PageWorkspace";
import { AppButton } from "@/shared/ui/AppButton";

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
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Карточка товара"
      description="Полная информация о товаре, цене, остатке и характеристиках."
      contentClassName="grid min-w-0 gap-6"
      actions={
        <Link
          href="/catalog/products"
          className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)] transition-colors hover:border-[var(--app-border-strong)] hover:bg-[var(--app-surface-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none"
        >
          <span aria-hidden="true">←</span>
          Назад к каталогу
        </Link>
      }
    >
      {productQuery.isLoading && (
        <section
          role="status"
          className="flex min-h-40 items-center justify-center gap-3 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6"
        >
          <span
            aria-hidden="true"
            className="h-5 w-5 shrink-0 animate-spin rounded-full border-2 border-[var(--app-accent-border)] border-t-[var(--app-accent)] motion-reduce:animate-none"
          />

          <p className="text-sm text-[var(--app-muted)]">
            Загружаем карточку товара...
          </p>
        </section>
      )}

      {productQuery.isError && (
        <section
          role="alert"
          className="rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 sm:p-6"
        >
          <h2 className="text-base font-semibold text-[var(--app-danger)]">
            Не удалось загрузить карточку товара
          </h2>

          <p className="mt-2 whitespace-pre-wrap break-words text-sm leading-6 text-[var(--app-danger)]">
            {getErrorMessage(productQuery.error)}
          </p>

          {product && (
            <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
              Ниже показаны ранее загруженные данные. Они могут быть
              неактуальны.
            </p>
          )}
        </section>
      )}

      {product && (
        <>
          <section
            aria-labelledby="product-overview-title"
            className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
          >
            <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
              <div className="min-w-0 flex-1">
                <h2
                  id="product-overview-title"
                  className="text-2xl font-bold leading-tight text-[var(--app-text)] [overflow-wrap:anywhere] sm:text-3xl"
                >
                  {product.name}
                </h2>

                <p className="mt-2 text-sm leading-6 text-[var(--app-muted)] [overflow-wrap:anywhere]">
                  Артикул:{" "}
                  <span className="font-medium text-[var(--app-text)]">
                    {product.article}
                  </span>
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-3 lg:shrink-0">
                <span
                  className={[
                    "inline-flex items-center rounded-full border px-4 py-2",
                    "text-sm font-medium",
                    product.stockQuantity > 0
                      ? "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]"
                      : "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]",
                  ].join(" ")}
                >
                  {product.stockQuantity > 0 ? "В наличии" : "Нет в наличии"}
                </span>

                {canEditProduct && (
                  <AppButton
                    type="button"
                    variant="primary"
                    aria-expanded={isEditorOpen}
                    onClick={() => setIsEditorOpen((current) => !current)}
                  >
                    {isEditorOpen ? "Закрыть редактор" : "Редактировать товар"}
                  </AppButton>
                )}
              </div>
            </div>

            <div className="mt-6 grid min-w-0 gap-4 sm:grid-cols-2 xl:grid-cols-5">
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

          {canEditProduct && isEditorOpen ? (
            <TechnicalProductEditor product={product} />
          ) : (
            <>
              <section
                aria-labelledby="product-characteristics-title"
                className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
              >
                <h2
                  id="product-characteristics-title"
                  className="text-xl font-semibold text-[var(--app-text)]"
                >
                  Характеристики
                </h2>

                {product.characteristics.length === 0 ? (
                  <p className="mt-4 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
                    Характеристики не указаны.
                  </p>
                ) : (
                  <dl className="mt-5 grid min-w-0 gap-3 md:grid-cols-2">
                    {product.characteristics.map((characteristic) => (
                      <div
                        key={`${characteristic.code}-${characteristic.value}`}
                        className="grid min-w-0 gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 sm:grid-cols-2"
                      >
                        <dt className="min-w-0">
                          <p className="text-sm font-medium text-[var(--app-text)] [overflow-wrap:anywhere]">
                            {characteristic.name}
                          </p>

                          <p className="mt-1 text-xs leading-5 text-[var(--app-muted)] [overflow-wrap:anywhere]">
                            {characteristic.code} · {characteristic.dataType}
                          </p>
                        </dt>

                        <dd className="min-w-0 whitespace-pre-wrap text-sm font-semibold leading-6 text-[var(--app-text)] [overflow-wrap:anywhere] sm:text-right">
                          {formatCharacteristicValue(characteristic)}
                        </dd>
                      </div>
                    ))}
                  </dl>
                )}
              </section>

              <section
                aria-labelledby="product-aliases-title"
                className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6"
              >
                <h2
                  id="product-aliases-title"
                  className="text-xl font-semibold text-[var(--app-text)]"
                >
                  Альтернативные названия
                </h2>

                {product.aliases.length === 0 ? (
                  <p className="mt-4 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
                    Альтернативные названия не указаны.
                  </p>
                ) : (
                  <ul className="mt-5 flex min-w-0 flex-wrap gap-2">
                    {product.aliases.map((alias) => (
                      <li
                        key={alias.id}
                        className="min-w-0 max-w-full rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-3 py-2 text-sm font-medium leading-5 text-[var(--app-accent)] [overflow-wrap:anywhere]"
                      >
                        {alias.value}
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            </>
          )}
        </>
      )}
    </PageWorkspace>
  );
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <dl className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <dt className="text-sm text-[var(--app-muted)]">{label}</dt>

      <dd className="mt-2 text-base font-semibold leading-6 tabular-nums text-[var(--app-text)] [overflow-wrap:anywhere]">
        {value}
      </dd>
    </dl>
  );
}
