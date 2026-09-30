"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getCatalogManufacturers } from "@/features/catalogMetadata/api/getCatalogManufacturers";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";
import { AppSelect } from "@/shared/ui/AppSelect";
import { bulkUpdateCatalogProducts } from "../api/bulkUpdateCatalogProducts";
import { searchCatalogProducts } from "../api/searchCatalogProducts";
import type {
  BulkUpdateCatalogProductRequest,
  CatalogProductListItem,
} from "../model/types";
import type { BulkEditorFilters } from "../model/bulkEditorFilters";

interface ProductDraft {
  name: string;
  article: string;
  manufacturerId: string;
  priceAmount: string;
  priceCurrency: string;
  stockQuantity: string;
}

function createDraft(product: CatalogProductListItem): ProductDraft {
  return {
    name: product.name,
    article: product.article,
    manufacturerId: product.manufacturerId,
    priceAmount: product.basePriceAmount.toString(),
    priceCurrency: product.basePriceCurrency,
    stockQuantity: product.stockQuantity.toString(),
  };
}

function parseNonNegativeNumber(value: string): number | null {
  const parsed = Number(value.trim().replace(",", "."));
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
}

function isDraftChanged(
  product: CatalogProductListItem,
  draft: ProductDraft,
): boolean {
  return (
    draft.name !== product.name ||
    draft.article !== product.article ||
    draft.manufacturerId !== product.manufacturerId ||
    draft.priceAmount !== product.basePriceAmount.toString() ||
    draft.priceCurrency !== product.basePriceCurrency ||
    draft.stockQuantity !== product.stockQuantity.toString()
  );
}

export function CatalogProductBulkEditor({
  filters,
}: {
  filters: BulkEditorFilters;
}) {
  const queryClient = useQueryClient();
  const { hasPermission } = useCurrentUserAccess();
  const canEditProducts = hasPermission("ProductsEdit");
  const canManagePrices = hasPermission("PricesManage");
  const canManageStock = hasPermission("StockManage");

  const [drafts, setDrafts] = useState<Record<string, ProductDraft>>({});
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [commonManufacturerId, setCommonManufacturerId] = useState("");
  const [commonPrice, setCommonPrice] = useState("");
  const [commonCurrency, setCommonCurrency] = useState("");
  const [commonStock, setCommonStock] = useState("");
  const [batchPage, setBatchPage] = useState(1);

  const productsQuery = useQuery({
    queryKey: ["catalog-products-bulk-editor", filters, batchPage],
    queryFn: () =>
      searchCatalogProducts({
        ...filters,
        page: batchPage,
        pageSize: 100,
      }),
  });

  const manufacturersQuery = useQuery({
    queryKey: ["catalog-manufacturers"],
    queryFn: getCatalogManufacturers,
    staleTime: 5 * 60 * 1000,
    enabled: canEditProducts,
  });

  const products = useMemo(
    () => productsQuery.data?.items ?? [],
    [productsQuery.data],
  );
  const totalBatches = Math.max(
    1,
    Math.ceil((productsQuery.data?.totalCount ?? 0) / 100),
  );

  const changedProducts = useMemo(
    () =>
      products.filter((product) => {
        const draft = drafts[product.id] ?? createDraft(product);
        return isDraftChanged(product, draft);
      }),
    [drafts, products],
  );

  const filterDescriptions = useMemo(() => {
    const descriptions: string[] = [];
    if (filters.search) descriptions.push(`Поиск: ${filters.search}`);
    if (filters.productTypeCode) {
      descriptions.push(`Тип: ${filters.productTypeCode}`);
    }
    if (filters.manufacturer) {
      descriptions.push(`Производитель: ${filters.manufacturer}`);
    }
    if (filters.onlyInStock === true) descriptions.push("Только в наличии");
    if (filters.onlyInStock === false) descriptions.push("Только без остатка");
    for (const characteristic of filters.characteristics ?? []) {
      descriptions.push(`${characteristic.code}: ${characteristic.value}`);
    }
    return descriptions;
  }, [filters]);

  useEffect(() => {
    if (changedProducts.length === 0) return;

    const preventUnsavedExit = (event: BeforeUnloadEvent) => {
      event.preventDefault();
    };
    const preventUnsavedNavigation = (event: MouseEvent) => {
      const target = event.target;
      if (!(target instanceof Element)) return;

      const anchor = target.closest("a");
      if (!anchor || anchor.target === "_blank") return;
      if (
        window.confirm(
          "Есть несохранённые изменения. Покинуть массовый редактор?",
        )
      ) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();
    };

    window.addEventListener("beforeunload", preventUnsavedExit);
    document.addEventListener("click", preventUnsavedNavigation, true);
    return () => {
      window.removeEventListener("beforeunload", preventUnsavedExit);
      document.removeEventListener("click", preventUnsavedNavigation, true);
    };
  }, [changedProducts.length]);

  const saveMutation = useMutation({
    mutationFn: bulkUpdateCatalogProducts,
    onError: (error) => {
      const message = getApiErrorMessage(
        error,
        "Не удалось сохранить изменения.",
      );
      const productId = products
        .map((product) => product.id)
        .find((id) => message.includes(id));

      if (productId) {
        setRowErrors((current) => ({ ...current, [productId]: message }));
      }
    },
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog-products"] });
      const refreshed = await productsQuery.refetch();

      if (refreshed.data) setDrafts({});

      setRowErrors({});
      setGeneralError(null);
      setSuccessMessage(
        `Сохранено товаров: ${result.updatedProductsCount}.`,
      );
    },
  });

  function updateDraft(
    productId: string,
    update: (draft: ProductDraft) => ProductDraft,
  ): void {
    setDrafts((current) => {
      const product = products.find((item) => item.id === productId);
      if (!product) return current;
      const draft = current[productId] ?? createDraft(product);

      return { ...current, [productId]: update(draft) };
    });
    setRowErrors((current) => {
      if (!current[productId]) return current;
      const next = { ...current };
      delete next[productId];
      return next;
    });
    setGeneralError(null);
    setSuccessMessage(null);
    saveMutation.reset();
  }

  function updateAllDrafts(update: (draft: ProductDraft) => ProductDraft) {
    setDrafts((current) =>
      Object.fromEntries(
        products.map((product) => {
          const draft = current[product.id] ?? createDraft(product);
          return [product.id, update(draft)];
        }),
      ),
    );
    setRowErrors({});
    setGeneralError(null);
    setSuccessMessage(null);
    saveMutation.reset();
  }

  function resetDrafts(): void {
    setDrafts({});
    setRowErrors({});
    setGeneralError(null);
    setSuccessMessage(null);
    saveMutation.reset();
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const nextErrors: Record<string, string> = {};
    const requestRows: BulkUpdateCatalogProductRequest[] = [];

    for (const product of changedProducts) {
      const draft = drafts[product.id];
      if (!draft) continue;

      const name = draft.name.trim();
      const article = draft.article.trim();
      const priceAmount = parseNonNegativeNumber(draft.priceAmount);
      const priceCurrency = draft.priceCurrency.trim().toUpperCase();
      const stockQuantity = parseNonNegativeNumber(draft.stockQuantity);

      if (canEditProducts && (name.length < 2 || name.length > 500)) {
        nextErrors[product.id] =
          "Наименование должно содержать от 2 до 500 символов.";
        continue;
      }

      if (canEditProducts && (!article || article.length > 100)) {
        nextErrors[product.id] =
          "Артикул обязателен и не должен превышать 100 символов.";
        continue;
      }

      if (canEditProducts && !draft.manufacturerId) {
        nextErrors[product.id] = "Выберите производителя.";
        continue;
      }

      if (canManagePrices && priceAmount === null) {
        nextErrors[product.id] = "Цена должна быть числом, не меньшим нуля.";
        continue;
      }

      if (canManagePrices && priceCurrency.length !== 3) {
        nextErrors[product.id] = "Валюта должна содержать три символа.";
        continue;
      }

      if (canManageStock && stockQuantity === null) {
        nextErrors[product.id] = "Остаток должен быть числом, не меньшим нуля.";
        continue;
      }

      const request: BulkUpdateCatalogProductRequest = {
        productId: product.id,
      };

      if (canEditProducts && draft.name !== product.name) request.name = name;
      if (canEditProducts && draft.article !== product.article) {
        request.article = article;
      }
      if (
        canEditProducts &&
        draft.manufacturerId !== product.manufacturerId
      ) {
        request.manufacturerId = draft.manufacturerId;
      }
      if (
        canManagePrices &&
        draft.priceAmount !== product.basePriceAmount.toString()
      ) {
        request.priceAmount = priceAmount;
      }
      if (
        canManagePrices &&
        draft.priceCurrency !== product.basePriceCurrency
      ) {
        request.priceCurrency = priceCurrency;
      }
      if (
        canManageStock &&
        draft.stockQuantity !== product.stockQuantity.toString()
      ) {
        request.stockQuantity = stockQuantity;
      }

      requestRows.push(request);
    }

    if (Object.keys(nextErrors).length > 0) {
      setRowErrors(nextErrors);
      setGeneralError(
        "Некоторые строки содержат ошибки. Исправьте отмеченные значения.",
      );
      return;
    }

    if (requestRows.length === 0) {
      setGeneralError("Нет изменений для сохранения.");
      return;
    }

    setRowErrors({});
    setGeneralError(null);
    setSuccessMessage(null);
    saveMutation.mutate(requestRows);
  }

  if (productsQuery.isLoading) {
    return (
      <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6 text-sm text-[var(--app-muted)]">
        Загружаем до 100 товаров по выбранным фильтрам...
      </section>
    );
  }

  if (productsQuery.isError) {
    return (
      <section role="alert" className="rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-6 text-sm text-[var(--app-danger)]">
        {getApiErrorMessage(
          productsQuery.error,
          "Не удалось загрузить выборку товаров.",
        )}
      </section>
    );
  }

  if (products.length === 0) {
    return (
      <section className="rounded-3xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-panel)] p-8 text-center">
        <p className="font-semibold text-[var(--app-text)]">Товары не найдены</p>
        <Link href="/catalog/products" className="mt-3 inline-block text-sm font-semibold text-[var(--app-accent)]">
          Изменить фильтры
        </Link>
      </section>
    );
  }

  const isBusy = saveMutation.isPending || productsQuery.isFetching;

  return (
    <form onSubmit={handleSubmit} className="grid min-w-0 gap-6">
      <section className="rounded-3xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-5 sm:p-6">
        <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
          <div>
            <h2 className="text-xl font-semibold text-[var(--app-text)]">
              Выборка для редактирования
            </h2>
            <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
              Найдено: {productsQuery.data?.totalCount ?? 0}. Загружено в
              редактор: {products.length}. Пакет: {batchPage} из {totalBatches}.
              Изменено строк: {changedProducts.length}.
            </p>
            {(productsQuery.data?.totalCount ?? 0) > 100 && (
              <p className="mt-1 text-sm text-[var(--app-warning)]">
                Товары разделены на пакеты по 100 строк. Сохраните или отмените
                изменения перед переходом к следующему пакету.
              </p>
            )}

            <ul className="mt-3 flex flex-wrap gap-2" aria-label="Применённые фильтры">
              {filterDescriptions.map((description) => (
                <li key={description} className="rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-1 text-xs text-[var(--app-muted)]">
                  {description}
                </li>
              ))}
            </ul>
          </div>

          <div className="flex flex-wrap gap-2">
            {totalBatches > 1 && (
              <>
                <AppButton
                  type="button"
                  variant="secondary"
                  disabled={isBusy || changedProducts.length > 0 || batchPage <= 1}
                  onClick={() => {
                    setDrafts({});
                    setBatchPage((current) => Math.max(1, current - 1));
                  }}
                >
                  Предыдущие 100
                </AppButton>
                <AppButton
                  type="button"
                  variant="secondary"
                  disabled={isBusy || changedProducts.length > 0 || batchPage >= totalBatches}
                  onClick={() => {
                    setDrafts({});
                    setBatchPage((current) => Math.min(totalBatches, current + 1));
                  }}
                >
                  Следующие 100
                </AppButton>
              </>
            )}
            <AppButton
              type="button"
              variant="secondary"
              disabled={isBusy || changedProducts.length === 0}
              onClick={resetDrafts}
            >
              Отменить изменения
            </AppButton>
            <AppButton
              type="submit"
              variant="primary"
              loading={saveMutation.isPending}
              disabled={isBusy || changedProducts.length === 0}
            >
              Сохранить {changedProducts.length || ""}
            </AppButton>
          </div>
        </div>

        {(generalError || saveMutation.isError) && (
          <div role="alert" className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
            {generalError ??
              getApiErrorMessage(
                saveMutation.error,
                "Не удалось сохранить изменения.",
              )}
          </div>
        )}

        {successMessage && (
          <div role="status" className="mt-4 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm text-[var(--app-success)]">
            {successMessage}
          </div>
        )}
      </section>

      {(canEditProducts || canManagePrices || canManageStock) && (
        <section className="grid gap-4 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6">
          <div>
            <h2 className="text-lg font-semibold text-[var(--app-text)]">
              Применить ко всей выборке
            </h2>
            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Значения заполнят все загруженные строки. До сохранения каждую
              строку можно поправить отдельно.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {canEditProducts && (
              <div className="grid content-start gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">Производитель</span>
                <AppSelect
                  ariaLabel="Общий производитель"
                  value={commonManufacturerId}
                  disabled={isBusy || manufacturersQuery.isLoading}
                  onChange={setCommonManufacturerId}
                  options={[
                    { value: "", label: "Выберите производителя" },
                    ...(manufacturersQuery.data ?? []).map((manufacturer) => ({
                      value: manufacturer.id,
                      label: manufacturer.name,
                    })),
                  ]}
                />
                <AppButton
                  size="sm"
                  disabled={!commonManufacturerId || isBusy}
                  onClick={() =>
                    updateAllDrafts((draft) => ({
                      ...draft,
                      manufacturerId: commonManufacturerId,
                    }))
                  }
                >
                  Применить
                </AppButton>
              </div>
            )}

            {canManagePrices && (
              <div className="grid content-start gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">Базовая цена</span>
                <AppInput value={commonPrice} inputMode="decimal" disabled={isBusy} onChange={(event) => setCommonPrice(event.target.value)} />
                <AppButton size="sm" disabled={parseNonNegativeNumber(commonPrice) === null || isBusy} onClick={() => updateAllDrafts((draft) => ({ ...draft, priceAmount: commonPrice }))}>
                  Применить
                </AppButton>
              </div>
            )}

            {canManagePrices && (
              <div className="grid content-start gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">Валюта</span>
                <AppInput value={commonCurrency} maxLength={3} disabled={isBusy} className="uppercase" onChange={(event) => setCommonCurrency(event.target.value)} />
                <AppButton size="sm" disabled={commonCurrency.trim().length !== 3 || isBusy} onClick={() => updateAllDrafts((draft) => ({ ...draft, priceCurrency: commonCurrency.trim().toUpperCase() }))}>
                  Применить
                </AppButton>
              </div>
            )}

            {canManageStock && (
              <div className="grid content-start gap-2">
                <span className="text-sm font-medium text-[var(--app-text)]">Остаток</span>
                <AppInput value={commonStock} inputMode="decimal" disabled={isBusy} onChange={(event) => setCommonStock(event.target.value)} />
                <AppButton size="sm" disabled={parseNonNegativeNumber(commonStock) === null || isBusy} onClick={() => updateAllDrafts((draft) => ({ ...draft, stockQuantity: commonStock }))}>
                  Применить
                </AppButton>
              </div>
            )}
          </div>
        </section>
      )}

      <section className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4 sm:p-5">
        <div role="region" aria-label="Массовое редактирование товаров" tabIndex={0} className="max-w-full overflow-x-auto rounded-2xl border border-[var(--app-border)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]">
          <table className="w-full min-w-[1320px] border-collapse text-left text-sm">
            <thead className="sticky top-0 z-10 bg-[var(--app-surface)] text-[var(--app-muted)]">
              <tr>
                <th className="px-3 py-3 font-medium">Наименование</th>
                <th className="px-3 py-3 font-medium">Артикул</th>
                <th className="px-3 py-3 font-medium">Производитель</th>
                <th className="px-3 py-3 font-medium">Тип</th>
                <th className="px-3 py-3 font-medium">Базовая цена</th>
                <th className="px-3 py-3 font-medium">Валюта</th>
                <th className="px-3 py-3 font-medium">Остаток</th>
                <th className="px-3 py-3 font-medium">Карточка</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--app-border)]">
              {products.map((product) => {
                const draft = drafts[product.id] ?? createDraft(product);
                const changed = isDraftChanged(product, draft);
                const error = rowErrors[product.id];

                return (
                  <tr key={product.id} className={error ? "bg-[var(--app-danger-soft)]" : changed ? "bg-[var(--app-accent-soft)]" : "bg-[var(--app-panel)]"}>
                    <td className="min-w-[300px] px-3 py-3 align-top">
                      <AppInput value={draft.name} disabled={!canEditProducts || isBusy} aria-invalid={Boolean(error)} onChange={(event) => updateDraft(product.id, (current) => ({ ...current, name: event.target.value }))} />
                      {error && <p className="mt-2 text-xs leading-5 text-[var(--app-danger)]">{error}</p>}
                    </td>
                    <td className="min-w-[190px] px-3 py-3 align-top">
                      <AppInput value={draft.article} disabled={!canEditProducts || isBusy} onChange={(event) => updateDraft(product.id, (current) => ({ ...current, article: event.target.value }))} />
                    </td>
                    <td className="min-w-[220px] px-3 py-3 align-top">
                      <AppSelect ariaLabel={`Производитель: ${product.name}`} value={draft.manufacturerId} disabled={!canEditProducts || isBusy || manufacturersQuery.isLoading} onChange={(value) => updateDraft(product.id, (current) => ({ ...current, manufacturerId: value }))} options={(manufacturersQuery.data ?? [{ id: product.manufacturerId, name: product.manufacturerName }]).map((manufacturer) => ({ value: manufacturer.id, label: manufacturer.name }))} />
                    </td>
                    <td className="min-w-[180px] px-3 py-3 align-top text-[var(--app-muted)]">
                      <p className="font-medium text-[var(--app-text)]">{product.productTypeName}</p>
                      <p className="mt-1 text-xs">{product.productTypeCode}</p>
                    </td>
                    <td className="min-w-[140px] px-3 py-3 align-top">
                      <AppInput value={draft.priceAmount} inputMode="decimal" disabled={!canManagePrices || isBusy} onChange={(event) => updateDraft(product.id, (current) => ({ ...current, priceAmount: event.target.value }))} />
                      {(product.priceAmount !== product.basePriceAmount ||
                        product.priceCurrency !== product.basePriceCurrency) && (
                        <p className="mt-1 text-xs text-[var(--app-muted)]">
                          Активная: {product.priceAmount} {product.priceCurrency}
                        </p>
                      )}
                    </td>
                    <td className="w-[105px] px-3 py-3 align-top">
                      <AppInput value={draft.priceCurrency} maxLength={3} className="uppercase" disabled={!canManagePrices || isBusy} onChange={(event) => updateDraft(product.id, (current) => ({ ...current, priceCurrency: event.target.value }))} />
                    </td>
                    <td className="w-[125px] px-3 py-3 align-top">
                      <AppInput value={draft.stockQuantity} inputMode="decimal" disabled={!canManageStock || isBusy} onChange={(event) => updateDraft(product.id, (current) => ({ ...current, stockQuantity: event.target.value }))} />
                    </td>
                    <td className="px-3 py-3 align-top">
                      <Link href={`/catalog/products/${product.id}`} target="_blank" className="inline-flex min-h-10 items-center rounded-xl border border-[var(--app-border)] px-3 text-xs font-semibold text-[var(--app-text)] hover:border-[var(--app-accent-border)] hover:text-[var(--app-accent)]">
                        Открыть
                      </Link>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </form>
  );
}
