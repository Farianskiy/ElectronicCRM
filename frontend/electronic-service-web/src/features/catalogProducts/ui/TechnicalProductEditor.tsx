"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { updateCatalogProductPrice } from "../api/updateCatalogProductPrice";
import { updateCatalogProductStock } from "../api/updateCatalogProductStock";
import type { CatalogProductDetails } from "../model/types";
import { TechnicalProductAliasesEditor } from "./TechnicalProductAliasesEditor";
import { TechnicalProductCharacteristicsEditor } from "./TechnicalProductCharacteristicsEditor";
import { TechnicalProductGeneralInformationEditor } from "./TechnicalProductGeneralInformationEditor";
import { TechnicalProductTypeMigrationPreview } from "@/features/catalogProductTypeMigration/ui/TechnicalProductTypeMigrationPreview";
import { TechnicalProductAuditHistory } from "@/features/catalogProductAuditHistory/ui/TechnicalProductAuditHistory";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";

interface TechnicalProductEditorProps {
  product: CatalogProductDetails;
}

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

  return "Не удалось сохранить изменения.";
}

export function TechnicalProductEditor({
  product,
}: TechnicalProductEditorProps) {
  const queryClient = useQueryClient();

  const [priceAmountDraft, setPriceAmountDraft] = useState<string | null>(null);

  const [priceCurrencyDraft, setPriceCurrencyDraft] = useState<string | null>(
    null,
  );

  const [stockQuantityDraft, setStockQuantityDraft] = useState<string | null>(
    null,
  );

  const priceAmount = priceAmountDraft ?? product.priceAmount.toString();

  const priceCurrency = priceCurrencyDraft ?? product.priceCurrency;

  const stockQuantity = stockQuantityDraft ?? product.stockQuantity.toString();

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [validationError, setValidationError] = useState<string | null>(null);

  async function refreshProduct(): Promise<void> {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ["catalog-product-details", product.id],
      }),

      queryClient.invalidateQueries({
        queryKey: ["catalog-products"],
      }),

      queryClient.invalidateQueries({
        queryKey: catalogProductAuditHistoryQueryKey(product.id),
      }),
    ]);
  }

  const priceMutation = useMutation({
    mutationFn: async (request: { amount: number; currency: string }) => {
      await updateCatalogProductPrice(product.id, request);
    },

    onSuccess: async () => {
      await refreshProduct();

      setPriceAmountDraft(null);
      setPriceCurrencyDraft(null);
      setValidationError(null);
      setSuccessMessage("Цена успешно обновлена.");
    },
  });

  const stockMutation = useMutation({
    mutationFn: async (request: { quantity: number }) => {
      await updateCatalogProductStock(product.id, request);
    },

    onSuccess: async () => {
      await refreshProduct();

      setStockQuantityDraft(null);
      setValidationError(null);
      setSuccessMessage("Остаток успешно обновлён.");
    },
  });

  function handlePriceSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setValidationError(null);
    setSuccessMessage(null);

    const normalizedPrice = priceAmount.trim().replace(",", ".");

    const amount = Number(normalizedPrice);

    if (!Number.isFinite(amount) || amount < 0) {
      setValidationError("Цена должна быть числом, не меньшим нуля.");

      return;
    }

    const currency = priceCurrency.trim().toUpperCase();

    if (currency.length === 0) {
      setValidationError("Укажите валюту товара.");

      return;
    }

    priceMutation.mutate({
      amount,
      currency,
    });
  }

  function handleStockSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setValidationError(null);
    setSuccessMessage(null);

    const normalizedQuantity = stockQuantity.trim().replace(",", ".");

    const quantity = Number(normalizedQuantity);

    if (!Number.isFinite(quantity) || quantity < 0) {
      setValidationError("Остаток должен быть числом, не меньшим нуля.");

      return;
    }

    stockMutation.mutate({
      quantity,
    });
  }

  const mutationError = priceMutation.error ?? stockMutation.error;

  return (
    <section className="min-w-0 rounded-3xl border border-[var(--app-accent-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
      <div>
        <h2 className="text-xl font-semibold text-[var(--app-text)]">
          Редактирование товара
        </h2>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Изменения доступны только техническому пользователю.
        </p>
      </div>

      {validationError && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {validationError}
        </div>
      )}

      {mutationError && (
        <div
          role="alert"
          className="mt-5 whitespace-pre-wrap rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm leading-6 text-[var(--app-danger)] [overflow-wrap:anywhere]"
        >
          {getErrorMessage(mutationError)}
        </div>
      )}

      {successMessage && (
        <div
          role="status"
          className="mt-5 rounded-2xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] p-4 text-sm leading-6 text-[var(--app-success)] [overflow-wrap:anywhere]"
        >
          {successMessage}
        </div>
      )}

      <div className="mt-6 min-w-0">
        <TechnicalProductGeneralInformationEditor product={product} />
      </div>

      <div className="mt-6 min-w-0">
        <TechnicalProductTypeMigrationPreview
          key={`${product.id}:${product.productTypeId}`}
          product={product}
        />
      </div>

      <div className="mt-6 grid min-w-0 gap-5 lg:grid-cols-2">
        <form
          onSubmit={handlePriceSubmit}
          className="flex min-w-0 flex-col rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5"
        >
          <h3 className="text-base font-semibold text-[var(--app-text)]">
            Цена
          </h3>

          <div className="mt-4 grid min-w-0 gap-4 sm:grid-cols-[minmax(0,1fr)_120px]">
            <label className="grid min-w-0 gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
                Значение
              </span>

              <AppInput
                type="number"
                min="0"
                step="0.01"
                required
                value={priceAmount}
                onChange={(event) => setPriceAmountDraft(event.target.value)}
              />
            </label>

            <label className="grid min-w-0 gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
                Валюта
              </span>

              <AppInput
                value={priceCurrency}
                onChange={(event) => setPriceCurrencyDraft(event.target.value)}
                maxLength={3}
                required
                className="uppercase"
              />
            </label>
          </div>

          <AppButton
            type="submit"
            variant="primary"
            loading={priceMutation.isPending}
            className="mt-5 w-full sm:w-auto sm:self-start"
          >
            {priceMutation.isPending ? "Сохраняем..." : "Сохранить цену"}
          </AppButton>
        </form>

        <form
          onSubmit={handleStockSubmit}
          className="flex min-w-0 flex-col rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5"
        >
          <h3 className="text-base font-semibold text-[var(--app-text)]">
            Остаток
          </h3>

          <label className="mt-4 grid min-w-0 gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Количество на складе
            </span>

            <AppInput
              type="number"
              min="0"
              step="any"
              required
              value={stockQuantity}
              onChange={(event) => setStockQuantityDraft(event.target.value)}
            />
          </label>

          <AppButton
            type="submit"
            variant="primary"
            loading={stockMutation.isPending}
            className="mt-5 w-full sm:w-auto sm:self-start"
          >
            {stockMutation.isPending ? "Сохраняем..." : "Сохранить остаток"}
          </AppButton>
        </form>
      </div>

      <div className="mt-6 grid min-w-0 gap-5">
        <TechnicalProductCharacteristicsEditor product={product} />

        <TechnicalProductAliasesEditor product={product} />

        <div className="mt-6 min-w-0">
          <TechnicalProductAuditHistory
            key={product.id}
            productId={product.id}
          />
        </div>
      </div>
    </section>
  );
}
