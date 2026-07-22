"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { addCatalogProductAlias } from "../api/addCatalogProductAlias";
import { removeCatalogProductAlias } from "../api/removeCatalogProductAlias";
import type { CatalogProductDetails } from "../model/types";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";

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

  return "Не удалось изменить альтернативные названия.";
}

export function TechnicalProductAliasesEditor({
  product,
}: TechnicalProductAliasesEditorProps) {
  const queryClient = useQueryClient();

  const [alias, setAlias] = useState("");

  const [aliasIdPendingRemoval, setAliasIdPendingRemoval] = useState<
    string | null
  >(null);

  const [validationError, setValidationError] = useState<string | null>(null);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

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

  const addMutation = useMutation({
    mutationFn: async (value: string) => {
      await addCatalogProductAlias(product.id, {
        alias: value,
      });
    },

    onSuccess: async (_data, savedAlias) => {
      await refreshProduct();

      setAlias("");
      setAliasIdPendingRemoval(null);
      setValidationError(null);
      setSuccessMessage(`Альтернативное название «${savedAlias}» добавлено.`);
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (request: { aliasId: string; aliasValue: string }) => {
      await removeCatalogProductAlias(product.id, request.aliasId);
    },

    onSuccess: async (_data, removedAlias) => {
      await refreshProduct();

      setAliasIdPendingRemoval(null);
      setValidationError(null);
      setSuccessMessage(
        `Альтернативное название «${removedAlias.aliasValue}» удалено.`,
      );
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    addMutation.reset();
    removeMutation.reset();
    setValidationError(null);
    setSuccessMessage(null);

    const normalizedAlias = alias.trim();

    if (normalizedAlias.length === 0) {
      setValidationError("Введите альтернативное название.");

      return;
    }

    addMutation.mutate(normalizedAlias);
  }

  const mutationError = addMutation.error ?? removeMutation.error;

  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <div>
        <h3 className="font-semibold text-white">Альтернативные названия</h3>

        <p className="mt-2 text-sm text-slate-400">
          Alias участвует в поиске товара. Удаление начинает действовать сразу
          после подтверждения.
        </p>
      </div>

      {product.aliases.length === 0 ? (
        <p className="mt-4 text-sm text-slate-500">
          Альтернативные названия пока не добавлены.
        </p>
      ) : (
        <div className="mt-4 grid gap-2">
          {product.aliases.map((productAlias) => {
            const isAwaitingConfirmation =
              aliasIdPendingRemoval === productAlias.id;

            const isRemoving =
              removeMutation.isPending &&
              removeMutation.variables?.aliasId === productAlias.id;

            return (
              <div
                key={productAlias.id}
                className="flex flex-col justify-between gap-3 rounded-2xl border border-white/10 bg-white/[0.02] px-4 py-3 sm:flex-row sm:items-center"
              >
                <span className="min-w-0 break-words text-sm font-medium text-teal-300">
                  {productAlias.value}
                </span>

                {isAwaitingConfirmation ? (
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      disabled={isRemoving}
                      onClick={() =>
                        removeMutation.mutate({
                          aliasId: productAlias.id,
                          aliasValue: productAlias.value,
                        })
                      }
                      className="rounded-xl bg-red-500 px-3 py-2 text-xs font-medium text-white transition hover:bg-red-400 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                      {isRemoving ? "Удаляем..." : "Подтвердить удаление"}
                    </button>

                    <button
                      type="button"
                      disabled={isRemoving}
                      onClick={() => setAliasIdPendingRemoval(null)}
                      className="rounded-xl border border-white/10 bg-white/[0.04] px-3 py-2 text-xs font-medium text-slate-300 transition hover:bg-white/[0.08]"
                    >
                      Отмена
                    </button>
                  </div>
                ) : (
                  <button
                    type="button"
                    disabled={addMutation.isPending || removeMutation.isPending}
                    onClick={() => {
                      addMutation.reset();
                      removeMutation.reset();
                      setValidationError(null);
                      setSuccessMessage(null);
                      setAliasIdPendingRemoval(productAlias.id);
                    }}
                    className="rounded-xl border border-red-500/30 bg-red-500/10 px-3 py-2 text-xs font-medium text-red-300 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    Удалить
                  </button>
                )}
              </div>
            );
          })}
        </div>
      )}

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
            setAliasIdPendingRemoval(null);
          }}
          placeholder="Например: NSX400F 4P 400A"
          className="min-w-0 flex-1 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400 focus:ring-2 focus:ring-teal-400/20"
        />

        <button
          type="submit"
          disabled={addMutation.isPending || removeMutation.isPending}
          className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {addMutation.isPending ? "Добавляем..." : "Добавить alias"}
        </button>
      </form>
    </div>
  );
}
