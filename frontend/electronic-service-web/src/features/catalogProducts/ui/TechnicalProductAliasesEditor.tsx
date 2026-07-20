"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { addCatalogProductAlias } from "../api/addCatalogProductAlias";
import type { CatalogProductDetails } from "../model/types";

interface TechnicalProductAliasesEditorProps {
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

  return "Не удалось добавить альтернативное название.";
}

export function TechnicalProductAliasesEditor({
  product,
}: TechnicalProductAliasesEditorProps) {
  const queryClient = useQueryClient();

  const [alias, setAlias] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: async (value: string) => {
      await addCatalogProductAlias(product.id, {
        alias: value,
      });
    },

    onSuccess: async (_data, savedAlias) => {
      setAlias("");
      setValidationError(null);
      setSuccessMessage(`Альтернативное название «${savedAlias}» добавлено.`);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-details", product.id],
        }),

        queryClient.invalidateQueries({
          queryKey: ["catalog-products"],
        }),
      ]);
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    mutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    const normalizedAlias = alias.trim();

    if (normalizedAlias.length === 0) {
      setValidationError("Введите альтернативное название.");

      return;
    }

    mutation.mutate(normalizedAlias);
  }

  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <div>
        <h3 className="font-semibold text-white">Альтернативные названия</h3>

        <p className="mt-2 text-sm text-slate-400">
          Alias участвует в поиске товара.
        </p>
      </div>

      {product.aliases.length > 0 && (
        <div className="mt-4 flex flex-wrap gap-2">
          {product.aliases.map((productAlias) => (
            <span
              key={productAlias}
              className="rounded-full bg-teal-500/15 px-3 py-1 text-sm font-medium text-teal-300"
            >
              {productAlias}
            </span>
          ))}
        </div>
      )}

      {validationError && (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-4 text-sm text-amber-200">
          {validationError}
        </div>
      )}

      {mutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutation.error)}
        </div>
      )}

      {successMessage && (
        <div className="mt-5 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}

      <form
        onSubmit={handleSubmit}
        className="mt-5 flex flex-col gap-3 sm:flex-row"
      >
        <input
          value={alias}
          onChange={(event) => {
            setAlias(event.target.value);
            setValidationError(null);
            setSuccessMessage(null);
          }}
          placeholder="Например: NSX400F 4P 400A"
          className="min-w-0 flex-1 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
        />

        <button
          type="submit"
          disabled={mutation.isPending}
          className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {mutation.isPending ? "Добавляем..." : "Добавить alias"}
        </button>
      </form>
    </div>
  );
}
