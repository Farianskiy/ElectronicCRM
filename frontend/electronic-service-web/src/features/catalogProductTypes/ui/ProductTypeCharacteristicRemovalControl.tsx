"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import { removeCharacteristicFromProductType } from "../api/removeCharacteristicFromProductType";
import type { CatalogProductTypeCharacteristicSchemaItem } from "../model/types";

interface ProductTypeCharacteristicRemovalControlProps {
  productTypeCode: string;
  productTypeName: string;

  characteristic: CatalogProductTypeCharacteristicSchemaItem;

  onRemoved: (characteristicName: string) => void;
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

  return "Не удалось удалить характеристику из схемы.";
}

export function ProductTypeCharacteristicRemovalControl({
  productTypeCode,
  productTypeName,
  characteristic,
  onRemoved,
}: ProductTypeCharacteristicRemovalControlProps) {
  const queryClient = useQueryClient();

  const [isConfirmationOpen, setIsConfirmationOpen] = useState(false);

  const mutation = useMutation({
    mutationFn: async () => {
      await removeCharacteristicFromProductType(
        productTypeCode,
        characteristic.definitionId,
      );
    },

    onSuccess: async () => {
      /*
       * Сообщение сохраняется на уровне страницы,
       * потому что после обновления schema текущая
       * карточка исчезнет из списка.
       */
      onRemoved(characteristic.name);

      setIsConfirmationOpen(false);

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: [
            "catalog-product-type-characteristic-schema",
            productTypeCode,
          ],
        }),

        /*
         * Удалённая definition снова должна
         * появиться среди доступных.
         *
         * Prefix-инвалидация обновит запросы
         * с любыми поисковыми строками.
         */
        queryClient.invalidateQueries({
          queryKey: [
            "catalog-product-type-available-definitions",
            productTypeCode,
          ],
        }),

        /*
         * Редактор товара больше не должен
         * показывать удалённую характеристику.
         */
        queryClient.invalidateQueries({
          queryKey: ["catalog-product-type-characteristics", productTypeCode],
        }),
      ]);
    },
  });

  function openConfirmation(): void {
    mutation.reset();
    setIsConfirmationOpen(true);
  }

  function cancelConfirmation(): void {
    mutation.reset();
    setIsConfirmationOpen(false);
  }

  return (
    <div
      className={
        characteristic.canRemoveFromType
          ? "rounded-2xl border border-amber-500/20 bg-amber-500/[0.06] p-4"
          : "rounded-2xl border border-red-500/20 bg-red-500/[0.06] p-4"
      }
    >
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div>
          <p
            className={
              characteristic.canRemoveFromType
                ? "font-medium text-amber-200"
                : "font-medium text-red-200"
            }
          >
            Удаление из схемы
          </p>

          <p className="mt-2 text-sm text-slate-400">
            {characteristic.canRemoveFromType
              ? "Характеристика не заполнена ни у одного товара. Связь с выбранным типом можно удалить."
              : `Удаление заблокировано: характеристика заполнена у ${characteristic.productsWithValueCount} товаров.`}
          </p>
        </div>

        {!isConfirmationOpen && (
          <button
            type="button"
            disabled={!characteristic.canRemoveFromType || mutation.isPending}
            onClick={openConfirmation}
            className="shrink-0 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-2 text-sm font-medium text-red-300 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Удалить из схемы
          </button>
        )}
      </div>

      {isConfirmationOpen && (
        <div className="mt-4 rounded-2xl border border-red-500/20 bg-black/20 p-4">
          <p className="font-medium text-white">Подтвердите удаление</p>

          <p className="mt-2 text-sm text-slate-400">
            Характеристика «{characteristic.name}» больше не будет разрешена для
            типа «{productTypeName}».
          </p>

          <p className="mt-2 text-sm text-slate-500">
            Глобальное определение характеристики сохранится и сможет
            использоваться другими типами товаров.
          </p>

          <div className="mt-4 flex flex-wrap gap-2">
            <button
              type="button"
              disabled={mutation.isPending}
              onClick={() => mutation.mutate()}
              className="rounded-xl bg-red-500 px-4 py-2 text-sm font-medium text-white transition hover:bg-red-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {mutation.isPending ? "Удаляем..." : "Подтвердить удаление"}
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
    </div>
  );
}
