"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import { addCatalogProductAlias } from "../api/addCatalogProductAlias";
import { removeCatalogProductAlias } from "../api/removeCatalogProductAlias";
import type { CatalogProductDetails } from "../model/types";
import { catalogProductAuditHistoryQueryKey } from "@/features/catalogProductAuditHistory/model/queryKeys";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";

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
    <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel-strong)] p-5">
      <div>
        <h3 className="text-base font-semibold text-[var(--app-text)]">
          Альтернативные названия
        </h3>

        <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
          Альтернативные названия участвуют в поиске товара. Удаление начинает
          действовать сразу после подтверждения.
        </p>
      </div>

      {product.aliases.length === 0 ? (
        <p className="mt-4 rounded-2xl border border-dashed border-[var(--app-border-strong)] bg-[var(--app-surface)] p-4 text-sm leading-6 text-[var(--app-muted)]">
          Альтернативные названия пока не добавлены.
        </p>
      ) : (
        <ul className="mt-4 grid min-w-0 gap-3">
          {product.aliases.map((productAlias) => {
            const isAwaitingConfirmation =
              aliasIdPendingRemoval === productAlias.id;

            const isRemoving =
              removeMutation.isPending &&
              removeMutation.variables?.aliasId === productAlias.id;

            return (
              <li
                key={productAlias.id}
                className="grid min-w-0 gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] px-4 py-3 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center"
              >
                <span className="min-w-0 text-sm font-medium leading-6 text-[var(--app-text)] [overflow-wrap:anywhere]">
                  {productAlias.value}
                </span>

                {isAwaitingConfirmation ? (
                  <div className="flex min-w-0 flex-wrap gap-2 lg:justify-end">
                    <AppButton
                      type="button"
                      variant="danger"
                      size="sm"
                      disabled={isRemoving}
                      loading={isRemoving}
                      onClick={() =>
                        removeMutation.mutate({
                          aliasId: productAlias.id,
                          aliasValue: productAlias.value,
                        })
                      }
                      className="max-w-full"
                    >
                      {isRemoving ? "Удаляем..." : "Подтвердить удаление"}
                    </AppButton>

                    <AppButton
                      type="button"
                      variant="secondary"
                      size="sm"
                      disabled={isRemoving}
                      onClick={() => setAliasIdPendingRemoval(null)}
                    >
                      Отмена
                    </AppButton>
                  </div>
                ) : (
                  <AppButton
                    type="button"
                    variant="danger"
                    size="sm"
                    disabled={addMutation.isPending || removeMutation.isPending}
                    onClick={() => {
                      addMutation.reset();
                      removeMutation.reset();
                      setValidationError(null);
                      setSuccessMessage(null);
                      setAliasIdPendingRemoval(productAlias.id);
                    }}
                    className="justify-self-start lg:justify-self-end"
                  >
                    Удалить
                  </AppButton>
                )}
              </li>
            );
          })}
        </ul>
      )}

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

      <form
        onSubmit={handleSubmit}
        className="mt-5 flex min-w-0 flex-col gap-3 sm:flex-row sm:items-end"
      >
        <label className="grid min-w-0 flex-1 gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Новое альтернативное название
          </span>

          <AppInput
            value={alias}
            aria-invalid={Boolean(validationError)}
            onChange={(event) => {
              setAlias(event.target.value);
              setValidationError(null);
              setSuccessMessage(null);
              setAliasIdPendingRemoval(null);
            }}
            placeholder="Например: NSX400F 4P 400A"
          />
        </label>

        <AppButton
          type="submit"
          variant="primary"
          disabled={addMutation.isPending || removeMutation.isPending}
          loading={addMutation.isPending}
          className="w-full sm:w-auto"
        >
          {addMutation.isPending ? "Добавляем..." : "Добавить название"}
        </AppButton>
      </form>
    </div>
  );
}
