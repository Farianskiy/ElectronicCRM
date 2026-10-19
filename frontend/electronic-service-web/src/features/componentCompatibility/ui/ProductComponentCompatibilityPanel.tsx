"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import { formatPrice } from "@/shared/lib/formatters";
import { getProductComponentCompatibility } from "../api/getProductComponentCompatibility";
import { setProductNeedStatus } from "../api/setProductNeedStatus";
import {
  changeSelectedComponentQuantity,
  removeSelectedComponent,
  selectProductComponent,
} from "../api/selectedComponents";
import type { ProductNeedStatus } from "../model/types";

const statusOptions: Array<{ value: ProductNeedStatus; label: string }> = [
  { value: "Unknown", label: "Неизвестно" },
  { value: "Missing", label: "Отсутствует" },
  { value: "Included", label: "В комплекте" },
  { value: "NotApplicable", label: "Не применяется" },
];

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data;

    if (typeof responseData === "string") {
      return responseData;
    }

    if (typeof responseData?.detail === "string") {
      return responseData.detail;
    }
  }

  return "Не удалось загрузить совместимые комплектующие.";
}

export function ProductComponentCompatibilityPanel({
  productId,
  canEdit,
}: {
  productId: string;
  canEdit: boolean;
}) {
  const queryClient = useQueryClient();
  const queryKey = ["product-component-compatibility", productId];
  const compatibilityQuery = useQuery({
    queryKey,
    queryFn: () => getProductComponentCompatibility(productId),
  });
  const statusMutation = useMutation({
    mutationFn: ({
      needDefinitionId,
      status,
    }: {
      needDefinitionId: string;
      status: ProductNeedStatus;
    }) => setProductNeedStatus(productId, needDefinitionId, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
  const selectionMutation = useMutation({
    mutationFn: ({
      needDefinitionId,
      componentProductId,
      quantity,
    }: {
      needDefinitionId: string;
      componentProductId: string;
      quantity: number;
    }) =>
      selectProductComponent(
        productId,
        needDefinitionId,
        componentProductId,
        quantity,
      ),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
  const quantityMutation = useMutation({
    mutationFn: ({
      selectionId,
      quantity,
    }: {
      selectionId: string;
      quantity: number;
    }) => changeSelectedComponentQuantity(productId, selectionId, quantity),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
  const removalMutation = useMutation({
    mutationFn: (selectionId: string) =>
      removeSelectedComponent(productId, selectionId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
  const isSelectionPending =
    selectionMutation.isPending ||
    quantityMutation.isPending ||
    removalMutation.isPending;

  if (compatibilityQuery.isLoading) {
    return (
      <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 text-sm text-[var(--app-muted)] sm:p-6">
        Загружаем комплектность...
      </section>
    );
  }

  if (compatibilityQuery.isError) {
    return (
      <section className="rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)] sm:p-6">
        {getErrorMessage(compatibilityQuery.error)}
      </section>
    );
  }

  const needs = compatibilityQuery.data?.needs ?? [];
  const accessories = compatibilityQuery.data?.accessories ?? [];

  if (needs.length === 0 && accessories.length === 0) {
    return null;
  }

  return (
    <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
      <h2 className="text-xl font-semibold text-[var(--app-text)]">
        Совместимые комплектующие
      </h2>

      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        Подходящие позиции определяются по характеристикам товара и показываются
        независимо от состава заводской поставки. Добавление здесь задаёт типовую
        комплектацию карточки; фактический выбор для продажи выполняется в проекте.
      </p>

      <div className="mt-5 grid gap-4">
        {accessories.length > 0 && (
          <article className="rounded-2xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] p-4">
            <h3 className="font-semibold text-[var(--app-text)]">
              Совместимые аксессуары
            </h3>
            <p className="mt-1 text-sm text-[var(--app-muted)]">
              Не входят в обязательную комплектацию и приобретаются по выбору.
            </p>

            <ul className="mt-3 grid gap-2">
              {accessories.map((component) => (
                <li
                  key={component.productId}
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-4 py-3"
                >
                  <Link
                    href={`/catalog/products/${component.productId}`}
                    className="block min-w-0 text-sm"
                  >
                    <span className="block font-medium text-[var(--app-text)]">
                      {component.name}
                    </span>
                    <span className="mt-1 block text-xs text-[var(--app-muted)]">
                      {component.article} ·{" "}
                      {formatPrice(
                        component.priceAmount,
                        component.priceCurrency,
                      )}
                    </span>
                    <span className="mt-2 block text-xs leading-5 text-[var(--app-muted)]">
                      {component.matchConditions.length === 0
                        ? "Подходит ко всем товарам этого типа."
                        : `Подходит: ${component.matchConditions
                            .map(
                              (condition) =>
                                `${condition.characteristicName} — ${condition.actualValue}`,
                            )
                            .join("; ")}.`}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </article>
        )}

        {needs.map((need) => (
          <article
            key={need.needDefinitionId}
            className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
          >
            <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
              <div>
                <h3 className="font-semibold text-[var(--app-text)]">
                  {need.name}
                </h3>

                <p className="mt-1 font-mono text-xs text-[var(--app-muted)]">
                  {need.code}
                </p>

                <span
                  className={`mt-2 inline-flex rounded-full border px-2.5 py-1 text-xs font-medium ${
                    need.requirementMode === "RequiredForAssembly"
                      ? "border-amber-500/30 bg-amber-500/10 text-amber-300"
                      : "border-[var(--app-border)] text-[var(--app-muted)]"
                  }`}
                >
                  {need.requirementMode === "RequiredForAssembly"
                    ? "Требуется для монтажа"
                    : "Необязательное"}
                  {` · обычно ${need.defaultQuantityPerUnit} шт./ед.`}
                </span>

                {need.selectedComponents.length > 0 && (
                  <span className="mt-2 inline-flex rounded-full border border-emerald-500/30 bg-emerald-500/10 px-2.5 py-1 text-xs font-medium text-emerald-300">
                    Задана типовая комплектация
                  </span>
                )}
              </div>

              {canEdit ? (
                <label className="grid gap-1 text-xs text-[var(--app-muted)]">
                  Состав заводской поставки
                  <select
                    value={need.status}
                    disabled={statusMutation.isPending}
                    onChange={(event) =>
                      statusMutation.mutate({
                        needDefinitionId: need.needDefinitionId,
                        status: event.target.value as ProductNeedStatus,
                      })
                    }
                    className="min-h-10 rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 text-sm text-[var(--app-text)]"
                  >
                    {statusOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
              ) : (
                <div className="grid gap-1 text-xs text-[var(--app-muted)]">
                  <span>Состав заводской поставки</span>
                  <span className="rounded-full border border-[var(--app-border)] px-3 py-1">
                    {statusOptions.find((option) => option.value === need.status)
                      ?.label ?? need.status}
                  </span>
                </div>
              )}
            </div>

            {need.selectedComponents.length > 0 && (
              <div className="mt-4">
                <h4 className="text-sm font-semibold text-[var(--app-text)]">
                  Типовая комплектация товара
                </h4>

                <ul className="mt-2 grid gap-2">
                  {need.selectedComponents.map((selection) => (
                    <li
                      key={selection.id}
                      className="flex flex-col gap-3 rounded-xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
                    >
                      <Link
                        href={`/catalog/products/${selection.componentProductId}`}
                        className="min-w-0 text-sm"
                      >
                        <span className="block truncate font-medium text-[var(--app-text)]">
                          {selection.name}
                        </span>
                        <span className="mt-1 block text-xs text-[var(--app-muted)]">
                          {selection.article} · {formatPrice(
                            selection.lineTotalAmount,
                            selection.priceCurrency,
                          )}
                        </span>
                      </Link>

                      {canEdit && (
                        <div className="flex items-center gap-2">
                          <label className="flex items-center gap-2 text-xs text-[var(--app-muted)]">
                            Количество
                            <input
                              key={`${selection.id}-${selection.quantity}`}
                              type="number"
                              min={1}
                              max={1000000}
                              defaultValue={selection.quantity}
                              disabled={isSelectionPending}
                              onBlur={(event) => {
                                const quantity = Number(event.target.value);

                                if (
                                  Number.isInteger(quantity) &&
                                  quantity >= 1 &&
                                  quantity !== selection.quantity
                                ) {
                                  quantityMutation.mutate({
                                    selectionId: selection.id,
                                    quantity,
                                  });
                                }
                              }}
                              className="w-24 rounded-lg border border-[var(--app-border)] bg-[var(--app-panel)] px-2 py-1.5 text-sm text-[var(--app-text)]"
                            />
                          </label>

                          <button
                            type="button"
                            disabled={isSelectionPending}
                            onClick={() => removalMutation.mutate(selection.id)}
                            className="rounded-lg border border-[var(--app-danger-border)] px-3 py-1.5 text-xs font-medium text-[var(--app-danger)] disabled:opacity-50"
                          >
                            Убрать
                          </button>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <div className="mt-4">
              <h4 className="text-sm font-semibold text-[var(--app-text)]">
                Подходящие варианты
              </h4>

              {need.compatibleComponents.length === 0 ? (
                <p className="mt-2 text-sm text-[var(--app-muted)]">
                  По действующим правилам подходящих комплектующих не найдено.
                </p>
              ) : (
                <ul className="mt-2 grid gap-2">
                  {need.compatibleComponents.map((component) => (
                    <li
                      key={component.productId}
                      className="flex flex-col justify-between gap-3 rounded-xl border border-[var(--app-accent-border)] bg-[var(--app-accent-soft)] px-4 py-3 sm:flex-row sm:items-center"
                    >
                      <Link
                        href={`/catalog/products/${component.productId}`}
                        className="min-w-0 text-sm"
                      >
                        <span className="block font-medium text-[var(--app-text)]">
                          {component.name}
                        </span>
                        <span className="mt-1 block text-xs text-[var(--app-muted)]">
                          {component.article} · {" "}
                          {formatPrice(
                            component.priceAmount,
                            component.priceCurrency,
                          )}
                        </span>
                        <span className="mt-2 block text-xs leading-5 text-[var(--app-muted)]">
                          {component.matchConditions.length === 0
                            ? "Подходит ко всем товарам этого типа."
                            : `Подходит: ${component.matchConditions
                                .map(
                                  (condition) =>
                                    `${condition.characteristicName} — ${condition.actualValue}`,
                                )
                                .join("; ")}.`}
                        </span>
                      </Link>

                      {canEdit && (
                        <button
                          type="button"
                          disabled={
                            isSelectionPending ||
                            need.selectedComponents.some(
                              (selection) =>
                                selection.componentProductId ===
                                component.productId,
                            )
                          }
                          onClick={() =>
                              selectionMutation.mutate({
                                needDefinitionId: need.needDefinitionId,
                                componentProductId: component.productId,
                                quantity: need.defaultQuantityPerUnit,
                              })
                          }
                          className="rounded-xl bg-[var(--app-accent)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
                        >
                          {need.selectedComponents.some(
                            (selection) =>
                              selection.componentProductId ===
                              component.productId,
                          )
                            ? "В типовой комплектации"
                            : "Добавить в типовую"}
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </article>
        ))}
      </div>

      {statusMutation.isError && (
        <p className="mt-4 text-sm text-[var(--app-danger)]">
          {getErrorMessage(statusMutation.error)}
        </p>
      )}

      {(selectionMutation.isError ||
        quantityMutation.isError ||
        removalMutation.isError) && (
        <p className="mt-4 text-sm text-[var(--app-danger)]">
          {getErrorMessage(
            selectionMutation.error ??
              quantityMutation.error ??
              removalMutation.error,
          )}
        </p>
      )}
    </section>
  );
}
