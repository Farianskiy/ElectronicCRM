"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import type { CatalogProductTypeMetadata } from "@/features/catalogMetadata/model/types";
import { AppButton } from "@/shared/ui/AppButton";
import {
  archiveProductType,
  previewProductTypeArchive,
  restoreProductType,
} from "../api/productTypeLifecycle";

interface ProductTypeLifecycleManagementProps {
  productType: CatalogProductTypeMetadata;
  onChanged: (message: string) => void;
}

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && typeof error.response?.data === "string") {
    return error.response.data;
  }

  return error instanceof Error
    ? error.message
    : "Не удалось изменить состояние типа товара.";
}

export function ProductTypeLifecycleManagement({
  productType,
  onChanged,
}: ProductTypeLifecycleManagementProps) {
  const queryClient = useQueryClient();
  const [isConfirmed, setIsConfirmed] = useState(false);

  const refreshTypes = async () => {
    await queryClient.invalidateQueries({ queryKey: ["catalog-product-types"] });
  };

  const previewMutation = useMutation({
    mutationFn: () => previewProductTypeArchive(productType.id),
    onSuccess: () => setIsConfirmed(false),
  });

  const archiveMutation = useMutation({
    mutationFn: () => archiveProductType(productType.id),
    onSuccess: async () => {
      await refreshTypes();
      previewMutation.reset();
      setIsConfirmed(false);
      onChanged(`Тип «${productType.name}» перемещён в архив.`);
    },
  });

  const restoreMutation = useMutation({
    mutationFn: () => restoreProductType(productType.id),
    onSuccess: async () => {
      await refreshTypes();
      onChanged(`Тип «${productType.name}» восстановлен из архива.`);
    },
  });

  if (productType.isArchived) {
    return (
      <section className="rounded-3xl border border-slate-500/30 bg-slate-500/[0.08] p-6">
        <h3 className="text-lg font-semibold text-white">Архивный тип</h3>
        <p className="mt-2 text-sm text-slate-400">
          Этот тип скрыт из новых импортов и операций. Исторические данные
          сохранены.
        </p>

        <AppButton
          className="mt-4"
          loading={restoreMutation.isPending}
          onClick={() => restoreMutation.mutate()}
        >
          Восстановить тип
        </AppButton>

        {restoreMutation.isError && (
          <p className="mt-4 text-sm text-red-200">
            {getErrorMessage(restoreMutation.error)}
          </p>
        )}
      </section>
    );
  }

  const preview = previewMutation.data;
  const error = previewMutation.error ?? archiveMutation.error;

  return (
    <section className="rounded-3xl border border-red-500/20 bg-red-500/[0.04] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-lg font-semibold text-white">
            Архивирование типа
          </h3>
          <p className="mt-2 text-sm text-slate-400">
            Архивный тип исчезнет из новых операций, но останется в истории.
          </p>
        </div>

        <AppButton
          variant="danger"
          loading={previewMutation.isPending}
          onClick={() => previewMutation.mutate()}
        >
          Проверить возможность архивирования
        </AppButton>
      </div>

      {error && <p className="mt-4 text-sm text-red-200">{getErrorMessage(error)}</p>}

      {preview && (
        <div className="mt-5 grid gap-4 rounded-2xl border border-white/10 bg-black/20 p-5">
          {preview.canArchive ? (
            <>
              <p className="text-sm text-green-200">
                Активных товаров, незавершённых импортов и настроек
                комплектующих нет. Тип можно архивировать.
              </p>

              <label className="flex items-start gap-3 text-sm text-slate-200">
                <input
                  type="checkbox"
                  checked={isConfirmed}
                  onChange={(event) => setIsConfirmed(event.target.checked)}
                  className="mt-0.5 h-4 w-4 accent-red-500"
                />
                <span>Подтверждаю архивирование типа товара.</span>
              </label>

              <AppButton
                variant="dangerSolid"
                loading={archiveMutation.isPending}
                disabled={!isConfirmed}
                onClick={() => archiveMutation.mutate()}
              >
                Архивировать тип
              </AppButton>
            </>
          ) : (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
              <p className="font-medium text-red-200">
                Тип пока нельзя архивировать:
              </p>
              <ul className="mt-3 grid gap-2 text-sm text-red-100">
                {preview.blockers.map((blocker) => (
                  <li key={blocker.code}>
                    {blocker.message} Количество: {blocker.count}.
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
