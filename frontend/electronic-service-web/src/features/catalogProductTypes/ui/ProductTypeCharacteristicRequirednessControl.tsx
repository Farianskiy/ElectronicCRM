"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { setProductTypeCharacteristicRequired } from "../api/setProductTypeCharacteristicRequired";
import type { CatalogProductTypeCharacteristicSchemaItem } from "../model/types";

interface ProductTypeCharacteristicRequirednessControlProps {
  productTypeCode: string;
  productTypeName: string;

  characteristic: CatalogProductTypeCharacteristicSchemaItem;
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

  return "Не удалось изменить обязательность характеристики.";
}

export function ProductTypeCharacteristicRequirednessControl({
  productTypeCode,
  productTypeName,
  characteristic,
}: ProductTypeCharacteristicRequirednessControlProps) {
  const queryClient = useQueryClient();

  const [isConfirmationOpen, setIsConfirmationOpen] = useState(false);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const targetIsRequired = !characteristic.isRequired;

  const canApplyChange =
    characteristic.isRequired || characteristic.canMakeRequired;

  const mutation = useMutation({
    mutationFn: async (isRequired: boolean) => {
      await setProductTypeCharacteristicRequired(
        productTypeCode,
        characteristic.definitionId,
        {
          isRequired,
        },
      );
    },

    onSuccess: async (_data, isRequired) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: [
            "catalog-product-type-characteristic-schema",
            productTypeCode,
          ],
        }),

        /*
         * Этот query используется редактором товара.
         * После обновления он должен показать новый
         * badge обязательности.
         */
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristics", productTypeCode],
        }),
      ]);

      setIsConfirmationOpen(false);

      setSuccessMessage(
        isRequired
          ? `Характеристика «${characteristic.name}» стала обязательной.`
          : `Характеристика «${characteristic.name}» стала необязательной.`,
      );
    },
  });

  function openConfirmation(): void {
    mutation.reset();
    setSuccessMessage(null);
    setIsConfirmationOpen(true);
  }

  function cancelConfirmation(): void {
    mutation.reset();
    setSuccessMessage(null);
    setIsConfirmationOpen(false);
  }

  return (
    <div
      className={
        characteristic.isRequired || characteristic.canMakeRequired
          ? "rounded-2xl border border-green-500/20 bg-green-500/[0.06] p-4"
          : "rounded-2xl border border-red-500/20 bg-red-500/[0.06] p-4"
      }
    >
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <p
            className={
              characteristic.isRequired || characteristic.canMakeRequired
                ? "font-medium text-green-200"
                : "font-medium text-red-200"
            }
          >
            Управление обязательностью
          </p>

          <p className="mt-2 text-sm text-slate-400">
            {characteristic.isRequired
              ? "Характеристика обязательна для всех товаров этого типа."
              : characteristic.canMakeRequired
                ? "Все существующие товары имеют значение. Характеристику можно сделать обязательной."
                : `У ${characteristic.productsWithoutValueCount} товаров отсутствует значение. Включение обязательности заблокировано.`}
          </p>
        </div>

        {!isConfirmationOpen && (
          <button
            type="button"
            disabled={!canApplyChange || mutation.isPending}
            onClick={openConfirmation}
            className={
              characteristic.isRequired
                ? "shrink-0 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-2 text-sm font-medium text-amber-200 transition hover:bg-amber-500/20 disabled:cursor-not-allowed disabled:opacity-50"
                : "shrink-0 rounded-xl bg-green-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-400 disabled:cursor-not-allowed disabled:opacity-50"
            }
          >
            {characteristic.isRequired
              ? "Сделать необязательной"
              : "Сделать обязательной"}
          </button>
        )}
      </div>

      {isConfirmationOpen && (
        <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-4">
          <p className="font-medium text-white">Подтвердите изменение</p>

          <p className="mt-2 text-sm text-slate-400">
            {targetIsRequired
              ? `После изменения все новые и редактируемые товары типа «${productTypeName}» должны содержать характеристику «${characteristic.name}».`
              : `Существующие значения сохранятся, но характеристику «${characteristic.name}» можно будет удалять у отдельных товаров типа «${productTypeName}».`}
          </p>

          <div className="mt-4 flex flex-wrap gap-2">
            <button
              type="button"
              disabled={mutation.isPending}
              onClick={() => mutation.mutate(targetIsRequired)}
              className="rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {mutation.isPending ? "Сохраняем..." : "Подтвердить"}
            </button>

            <button
              type="button"
              disabled={mutation.isPending}
              onClick={cancelConfirmation}
              className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-2 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08] disabled:opacity-50"
            >
              Отмена
            </button>
          </div>
        </div>
      )}

      {mutation.isError && (
        <div className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
          {getErrorMessage(mutation.error)}
        </div>
      )}

      {successMessage && (
        <div className="mt-4 rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
          {successMessage}
        </div>
      )}
    </div>
  );
}
