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
    <section className="rounded-3xl border border-teal-500/20 bg-teal-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Редактирование товара
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Изменения доступны только техническому пользователю.
        </p>
      </div>

      {validationError && (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {validationError}
        </div>
      )}

      {mutationError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutationError)}
        </div>
      )}

      {successMessage && (
        <div className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      <div className="mt-6 grid gap-5 lg:grid-cols-2">
        <form
          onSubmit={handlePriceSubmit}
          className="rounded-2xl border border-white/10 bg-black/20 p-5"
        >
          <h3 className="font-semibold text-white">Цена</h3>

          <div className="mt-4 grid gap-4 sm:grid-cols-[1fr_120px]">
            <label className="grid gap-2">
              <span className="text-sm text-slate-300">Значение</span>

              <input
                type="number"
                min="0"
                step="0.01"
                required
                value={priceAmount}
                onChange={(event) => setPriceAmountDraft(event.target.value)}
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
              />
            </label>

            <label className="grid gap-2">
              <span className="text-sm text-slate-300">Валюта</span>

              <input
                value={priceCurrency}
                onChange={(event) => setPriceCurrencyDraft(event.target.value)}
                maxLength={3}
                required
                className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 uppercase text-slate-100 outline-none focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
              />
            </label>
          </div>

          <button
            type="submit"
            disabled={priceMutation.isPending}
            className="mt-5 rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {priceMutation.isPending ? "Сохраняем..." : "Сохранить цену"}
          </button>
        </form>

        <form
          onSubmit={handleStockSubmit}
          className="rounded-2xl border border-white/10 bg-black/20 p-5"
        >
          <h3 className="font-semibold text-white">Остаток</h3>

          <label className="mt-4 grid gap-2">
            <span className="text-sm text-slate-300">Количество на складе</span>

            <input
              type="number"
              min="0"
              step="any"
              required
              value={stockQuantity}
              onChange={(event) => setStockQuantityDraft(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
            />
          </label>

          <button
            type="submit"
            disabled={stockMutation.isPending}
            className="mt-5 rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {stockMutation.isPending ? "Сохраняем..." : "Сохранить остаток"}
          </button>
        </form>
      </div>
      <div className="mt-6 grid gap-5">
        <TechnicalProductCharacteristicsEditor product={product} />

        <TechnicalProductAliasesEditor product={product} />
      </div>
    </section>
  );
}
