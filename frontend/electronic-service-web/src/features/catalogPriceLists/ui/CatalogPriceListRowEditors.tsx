"use client";

import { useQuery } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { searchCatalogPriceListProducts } from "@/features/catalogPriceLists/api/catalogPriceListDetailsApi";
import { catalogPriceListQueryKeys } from "@/features/catalogPriceLists/model/queryKeys";
import type {
  BulkUpdateCatalogPriceListRowsRequest,
  CatalogPriceListProductSearchItem,
  CatalogPriceListRow,
  UpdateCatalogPriceListRowRequest,
} from "@/features/catalogPriceLists/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";

const productPageSize = 10;

interface NullableAmountResult {
  isValid: boolean;
  value: number | null;
}

function parseNullableAmount(value: string): NullableAmountResult {
  const normalizedValue = value.trim().replace(",", ".");

  if (!normalizedValue) {
    return {
      isValid: true,
      value: null,
    };
  }

  const parsedValue = Number(normalizedValue);

  if (!Number.isFinite(parsedValue) || parsedValue < 0) {
    return {
      isValid: false,
      value: null,
    };
  }

  return {
    isValid: true,
    value: parsedValue,
  };
}

export function ProductPicker({
  priceListId,
  selectedProductId,
  disabled,
  onSelect,
  onClear,
}: {
  priceListId: string;
  selectedProductId?: string | null;
  disabled: boolean;
  onSelect: (product: CatalogPriceListProductSearchItem) => void;
  onClear: () => void;
}) {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [searchStarted, setSearchStarted] = useState(false);

  const productsQuery = useQuery({
    queryKey: catalogPriceListQueryKeys.products(
      priceListId,
      search,
      page,
      productPageSize,
    ),
    queryFn: () =>
      searchCatalogPriceListProducts({
        priceListId,
        search,
        page,
        pageSize: productPageSize,
      }),
    enabled: searchStarted && priceListId.length > 0,
    placeholderData: (previousData) => previousData,
  });

  const products = productsQuery.data?.items ?? [];
  const totalPages = Math.max(1, productsQuery.data?.totalPages ?? 0);

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    setSearch(searchInput.trim());
    setPage(1);
    setSearchStarted(true);
  }

  return (
    <section className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h3 className="font-semibold text-[var(--app-text)]">
            Товар каталога
          </h3>

          <p className="mt-1 text-xs text-[var(--app-muted)]">
            Поиск выполняется только среди товаров производителя этого прайса.
          </p>
        </div>

        {selectedProductId && (
          <AppButton
            type="button"
            variant="ghost"
            size="sm"
            disabled={disabled}
            onClick={onClear}
          >
            Очистить ProductId
          </AppButton>
        )}
      </div>

      <p className="mt-3 break-all text-sm text-[var(--app-muted)]">
        Выбранный ProductId: {selectedProductId ?? "не выбран"}
      </p>

      <form
        onSubmit={handleSearch}
        className="mt-4 flex flex-col gap-2 sm:flex-row"
      >
        <AppInput
          value={searchInput}
          disabled={disabled}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="Артикул или наименование"
        />

        <AppButton
          type="submit"
          variant="secondary"
          loading={productsQuery.isFetching}
          disabled={disabled}
        >
          Найти
        </AppButton>
      </form>

      {productsQuery.isError && (
        <div
          role="alert"
          className="mt-3 rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            productsQuery.error,
            "Не удалось выполнить поиск товаров.",
          )}
        </div>
      )}

      {searchStarted && productsQuery.isSuccess && products.length === 0 && (
        <p className="mt-3 text-sm text-[var(--app-muted)]">
          Подходящие товары производителя не найдены.
        </p>
      )}

      {products.length > 0 && (
        <div className="mt-3 grid gap-2">
          {products.map((product) => (
            <button
              key={product.productId}
              type="button"
              disabled={disabled}
              onClick={() => onSelect(product)}
              className={[
                "rounded-xl border p-3 text-left transition-colors",
                "disabled:cursor-not-allowed disabled:opacity-60",
                product.productId === selectedProductId
                  ? "border-[var(--app-accent)] bg-[var(--app-accent-soft)]"
                  : "border-[var(--app-border)] bg-[var(--app-panel)] hover:border-[var(--app-border-strong)]",
              ].join(" ")}
            >
              <span className="block font-semibold text-[var(--app-text)]">
                {product.article}
              </span>

              <span className="mt-1 block text-sm text-[var(--app-text)]">
                {product.name}
              </span>

              <span className="mt-1 block text-xs text-[var(--app-muted)]">
                {product.productTypeName} · {product.productTypeCode}
              </span>
            </button>
          ))}
        </div>
      )}

      {productsQuery.data && productsQuery.data.totalPages > 1 && (
        <div className="mt-3 flex items-center justify-between">
          <AppButton
            type="button"
            variant="secondary"
            size="sm"
            disabled={disabled || page <= 1 || productsQuery.isFetching}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
          >
            Назад
          </AppButton>

          <span className="text-xs text-[var(--app-muted)]">
            Страница {page} из {totalPages}
          </span>

          <AppButton
            type="button"
            variant="secondary"
            size="sm"
            disabled={
              disabled || page >= totalPages || productsQuery.isFetching
            }
            onClick={() =>
              setPage((current) => Math.min(totalPages, current + 1))
            }
          >
            Вперёд
          </AppButton>
        </div>
      )}
    </section>
  );
}

export function CatalogPriceListSingleRowEditor({
  priceListId,
  row,
  saving,
  errorMessage,
  onSave,
  onCancel,
}: {
  priceListId: string;
  row: CatalogPriceListRow;
  saving: boolean;
  errorMessage?: string | null;
  onSave: (request: UpdateCatalogPriceListRowRequest) => void;
  onCancel: () => void;
}) {
  const [article, setArticle] = useState(row.article);
  const [name, setName] = useState(row.name);
  const [basePriceAmount, setBasePriceAmount] = useState(
    row.basePriceAmount?.toString() ?? "",
  );
  const [mrcPriceAmount, setMrcPriceAmount] = useState(
    row.mrcPriceAmount?.toString() ?? "",
  );
  const [productUrl, setProductUrl] = useState(row.productUrl ?? "");
  const [unit, setUnit] = useState(row.unit ?? "");
  const [productId, setProductId] = useState<string | null>(
    row.productId ?? null,
  );
  const [selectedProduct, setSelectedProduct] =
    useState<CatalogPriceListProductSearchItem | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    const normalizedArticle = article.trim();
    const normalizedName = name.trim();
    const basePriceResult = parseNullableAmount(basePriceAmount);
    const mrcPriceResult = parseNullableAmount(mrcPriceAmount);

    if (!normalizedArticle) {
      setValidationError("Укажите артикул.");
      return;
    }

    if (!normalizedName) {
      setValidationError("Укажите наименование.");
      return;
    }

    if (!basePriceResult.isValid) {
      setValidationError(
        "Базовая цена должна быть положительным числом или пустым значением.",
      );
      return;
    }

    if (!mrcPriceResult.isValid) {
      setValidationError(
        "МРЦ должна быть положительным числом или пустым значением.",
      );
      return;
    }

    setValidationError(null);

    onSave({
      article: normalizedArticle,
      name: normalizedName,
      basePriceAmount: basePriceResult.value,
      mrcPriceAmount: mrcPriceResult.value,
      productUrl: productUrl.trim() || null,
      unit: unit.trim() || null,
      productId,
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-5 rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-5"
    >
      <div>
        <h3 className="text-lg font-semibold text-[var(--app-text)]">
          Редактирование строки {row.rowNumber}
        </h3>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          После сохранения backend повторно проверит строку и пересчитает
          статистику прайса.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Артикул
          </span>

          <AppInput
            value={article}
            disabled={saving}
            onChange={(event) => setArticle(event.target.value)}
          />
        </label>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Единица измерения
          </span>

          <AppInput
            value={unit}
            disabled={saving}
            onChange={(event) => setUnit(event.target.value)}
            placeholder="шт"
          />
        </label>

        <label className="grid gap-2 lg:col-span-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Наименование
          </span>

          <textarea
            value={name}
            disabled={saving}
            onChange={(event) => setName(event.target.value)}
            rows={3}
            className="min-h-24 w-full rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 text-sm text-[var(--app-text)] focus:border-[var(--app-accent)] focus:outline-none focus:ring-2 focus:ring-[var(--app-accent)] disabled:cursor-not-allowed disabled:opacity-60"
          />
        </label>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Прайс 100%
          </span>

          <AppInput
            value={basePriceAmount}
            disabled={saving}
            inputMode="decimal"
            onChange={(event) => setBasePriceAmount(event.target.value)}
          />
        </label>

        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            МРЦ
          </span>

          <AppInput
            value={mrcPriceAmount}
            disabled={saving}
            inputMode="decimal"
            onChange={(event) => setMrcPriceAmount(event.target.value)}
          />
        </label>

        <label className="grid gap-2 lg:col-span-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Ссылка на товар
          </span>

          <AppInput
            type="url"
            value={productUrl}
            disabled={saving}
            onChange={(event) => setProductUrl(event.target.value)}
            placeholder="https://..."
          />
        </label>
      </div>

      <ProductPicker
        priceListId={priceListId}
        selectedProductId={productId}
        disabled={saving}
        onSelect={(product) => {
          setSelectedProduct(product);
          setProductId(product.productId);
        }}
        onClear={() => {
          setSelectedProduct(null);
          setProductId(null);
        }}
      />

      {selectedProduct && (
        <p className="text-sm text-[var(--app-success)]">
          Выбран товар: {selectedProduct.article} — {selectedProduct.name}
        </p>
      )}

      {(validationError || errorMessage) && (
        <div
          role="alert"
          className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]"
        >
          {validationError ?? errorMessage}
        </div>
      )}

      <div className="flex flex-wrap justify-end gap-2">
        <AppButton
          type="button"
          variant="secondary"
          disabled={saving}
          onClick={onCancel}
        >
          Отмена
        </AppButton>

        <AppButton type="submit" variant="primary" loading={saving}>
          Сохранить строку
        </AppButton>
      </div>
    </form>
  );
}

export function CatalogPriceListBulkRowEditor({
  priceListId,
  rows,
  groupIssueCode,
  groupField,
  groupSourceValue,
  saving,
  errorMessage,
  onSave,
  onCancel,
}: {
  priceListId: string;
  rows: CatalogPriceListRow[];
  groupIssueCode: string;
  groupField: string;
  groupSourceValue: string;
  saving: boolean;
  errorMessage?: string | null;
  onSave: (request: BulkUpdateCatalogPriceListRowsRequest) => void;
  onCancel: () => void;
}) {
  const normalizedGroupField = groupField.trim().toLowerCase();
  const canApplyUnit = normalizedGroupField === "unit";
  const canApplyProduct = normalizedGroupField === "productid";
  const [applyUnit, setApplyUnit] = useState(canApplyUnit);
  const [unit, setUnit] = useState("");
  const [applyProduct, setApplyProduct] = useState(canApplyProduct);
  const [selectedProduct, setSelectedProduct] =
    useState<CatalogPriceListProductSearchItem | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (!applyUnit && !applyProduct) {
      setValidationError(
        "Выберите хотя бы одно групповое изменение: единицу измерения или товар.",
      );
      return;
    }

    if (applyProduct && !selectedProduct) {
      setValidationError("Найдите и выберите товар для группового применения.");
      return;
    }

    setValidationError(null);

    onSave({
      rows: rows.map((row) => ({
        rowId: row.rowId,
        article: row.article,
        name: row.name,
        basePriceAmount: row.basePriceAmount ?? null,
        mrcPriceAmount: row.mrcPriceAmount ?? null,
        productUrl: row.productUrl ?? null,
        unit: applyUnit ? unit.trim() || null : (row.unit ?? null),
        productId:
          applyProduct && selectedProduct
            ? selectedProduct.productId
            : (row.productId ?? null),
      })),
    });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="grid gap-5 rounded-2xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-5"
    >
      <div>
        <h3 className="text-lg font-semibold text-[var(--app-text)]">
          Групповое редактирование
        </h3>

        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Выбрано строк: {rows.length}. В один запрос можно отправить не более
          200 строк.
        </p>

        <div className="mt-3 grid gap-1 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3 text-sm">
          <p className="text-[var(--app-muted)]">
            Код ошибки:{" "}
            <span className="font-semibold text-[var(--app-text)]">
              {groupIssueCode}
            </span>
          </p>

          <p className="text-[var(--app-muted)]">
            Поле:{" "}
            <span className="font-semibold text-[var(--app-text)]">
              {groupField}
            </span>
          </p>

          <p className="break-all text-[var(--app-muted)]">
            Исходное значение:{" "}
            <span className="font-semibold text-[var(--app-text)]">
              {groupSourceValue || "пустое значение"}
            </span>
          </p>
        </div>
      </div>

      <label className="flex items-start gap-3">
        <input
          type="checkbox"
          checked={applyUnit}
          disabled={saving || !canApplyUnit}
          onChange={(event) => setApplyUnit(event.target.checked)}
          className="mt-1 h-4 w-4 rounded border-[var(--app-border)] accent-[var(--app-accent)]"
        />

        <span className="grid flex-1 gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Применить одну единицу измерения
          </span>

          <AppInput
            value={unit}
            disabled={saving || !applyUnit}
            onChange={(event) => setUnit(event.target.value)}
            placeholder="Например: шт"
          />
        </span>
      </label>

      <label className="flex items-start gap-3">
        <input
          type="checkbox"
          checked={applyProduct}
          disabled={saving || !canApplyProduct}
          onChange={(event) => setApplyProduct(event.target.checked)}
          className="mt-1 h-4 w-4 rounded border-[var(--app-border)] accent-[var(--app-accent)]"
        />

        <span className="text-sm font-medium text-[var(--app-text)]">
          Применить один товар ко всем выбранным строкам
        </span>
      </label>

      {!canApplyUnit && !canApplyProduct && (
        <div className="rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-3 text-sm text-[var(--app-warning)]">
          Для поля {groupField} безопасное групповое действие пока не
          предусмотрено. Исправьте такие строки по отдельности.
        </div>
      )}

      {applyProduct && (
        <>
          <div className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]">
            Используйте общий ProductId только для строк, которые действительно
            относятся к одному товару. Артикулы и наименования строк при этом не
            изменяются.
          </div>

          <ProductPicker
            priceListId={priceListId}
            selectedProductId={selectedProduct?.productId}
            disabled={saving}
            onSelect={setSelectedProduct}
            onClear={() => setSelectedProduct(null)}
          />
        </>
      )}

      {(validationError || errorMessage) && (
        <div
          role="alert"
          className="rounded-xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-3 text-sm text-[var(--app-danger)]"
        >
          {validationError ?? errorMessage}
        </div>
      )}

      <div className="flex flex-wrap justify-end gap-2">
        <AppButton
          type="button"
          variant="secondary"
          disabled={saving}
          onClick={onCancel}
        >
          Отмена
        </AppButton>

        <AppButton
          type="submit"
          variant="warning"
          loading={saving}
          disabled={!canApplyUnit && !canApplyProduct}
        >
          Применить к {rows.length} строкам
        </AppButton>
      </div>
    </form>
  );
}
