"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { useState } from "react";
import type { CatalogProductTypeMetadata } from "@/features/catalogMetadata/model/types";
import { AppButton } from "@/shared/ui/AppButton";
import { changeProductTypeKind } from "../api/changeProductTypeKind";
import { previewProductTypeKindChange } from "../api/previewProductTypeKindChange";
import type { ProductTypeKind } from "../model/types";

interface ProductTypeKindManagementProps {
  productType: CatalogProductTypeMetadata;
  onChanged: (message: string) => void;
}

function formatKind(kind: ProductTypeKind): string {
  return kind === "Component" ? "Комплектующее" : "Основной товар";
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

  return error instanceof Error
    ? error.message
    : "Не удалось изменить назначение типа товара.";
}

export function ProductTypeKindManagement({
  productType,
  onChanged,
}: ProductTypeKindManagementProps) {
  const queryClient = useQueryClient();
  const [isConfirmed, setIsConfirmed] = useState(false);

  const targetKind: ProductTypeKind =
    productType.kind === "Component" ? "MainProduct" : "Component";

  const previewMutation = useMutation({
    mutationFn: () =>
      previewProductTypeKindChange(productType.id, targetKind),
    onSuccess: () => setIsConfirmed(false),
  });

  const changeMutation = useMutation({
    mutationFn: () => changeProductTypeKind(productType.id, targetKind),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["catalog-product-types"],
      });

      previewMutation.reset();
      setIsConfirmed(false);
      onChanged(
        `Назначение типа «${productType.name}» изменено на «${formatKind(targetKind)}». Товары и характеристики сохранены.`,
      );
    },
  });

  const preview = previewMutation.data;
  const error = previewMutation.error ?? changeMutation.error;

  return (
    <section className="rounded-3xl border border-amber-500/25 bg-amber-500/[0.06] p-6">
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <h3 className="text-lg font-semibold text-white">
            Назначение типа
          </h3>

          <p className="mt-2 text-sm text-slate-300">
            Сейчас: <strong>{formatKind(productType.kind)}</strong>. Можно
            безопасно изменить назначение после проверки зависимостей.
          </p>

          <p className="mt-1 text-sm text-slate-400">
            Товары, их значения и схема характеристик при этом сохраняются.
          </p>
        </div>

        <AppButton
          variant="warning"
          loading={previewMutation.isPending}
          disabled={changeMutation.isPending}
          onClick={() => {
            changeMutation.reset();
            previewMutation.mutate();
          }}
        >
          Проверить смену на «{formatKind(targetKind)}»
        </AppButton>
      </div>

      {error && (
        <div
          role="alert"
          className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200"
        >
          {getErrorMessage(error)}
        </div>
      )}

      {preview && (
        <div className="mt-5 grid gap-4 rounded-2xl border border-white/10 bg-black/20 p-5">
          <div className="grid gap-3 sm:grid-cols-2">
            <KindSummary label="Товаров будет сохранено" value={preview.productsCount} />
            <KindSummary
              label="Характеристик будет сохранено"
              value={preview.characteristicsCount}
            />
          </div>

          {preview.canChange ? (
            <div className="rounded-2xl border border-green-500/30 bg-green-500/10 p-4 text-sm text-green-200">
              Блокирующих зависимостей нет. Назначение можно изменить.
            </div>
          ) : (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
              <p className="font-medium text-red-200">
                Сначала устраните следующие зависимости:
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

          {preview.canChange && (
            <>
              <label className="flex items-start gap-3 text-sm text-slate-200">
                <input
                  type="checkbox"
                  checked={isConfirmed}
                  onChange={(event) => setIsConfirmed(event.target.checked)}
                  className="mt-0.5 h-4 w-4 accent-teal-500"
                />

                <span>
                  Подтверждаю смену назначения на «{formatKind(targetKind)}».
                  Товары и характеристики должны остаться без изменений.
                </span>
              </label>

              <AppButton
                variant="warning"
                loading={changeMutation.isPending}
                disabled={!isConfirmed || previewMutation.isPending}
                onClick={() => changeMutation.mutate()}
                className="w-full"
              >
                Изменить назначение типа
              </AppButton>
            </>
          )}
        </div>
      )}
    </section>
  );
}

function KindSummary({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-4">
      <p className="text-sm text-slate-400">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>
    </div>
  );
}
