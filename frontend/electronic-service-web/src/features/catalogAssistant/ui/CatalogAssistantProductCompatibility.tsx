"use client";

import { useQuery } from "@tanstack/react-query";
import axios from "axios";
import Link from "next/link";
import { useState } from "react";
import { getProductComponentCompatibility } from "@/features/componentCompatibility/api/getProductComponentCompatibility";
import type {
  CompatibleComponent,
  ProductNeedCompatibility,
} from "@/features/componentCompatibility/model/types";
import { formatPrice } from "@/shared/lib/formatters";

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

  return "Не удалось загрузить комплектующие. Попробуйте ещё раз.";
}

function ComponentCard({ component }: { component: CompatibleComponent }) {
  return (
    <li className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
        <div className="min-w-0">
          <Link
            href={`/catalog/products/${component.productId}`}
            className="font-semibold text-white transition hover:text-teal-300"
          >
            {component.name}
          </Link>
          <p className="mt-1 text-xs text-slate-400">
            Артикул: {component.article}
          </p>
        </div>

        <div className="shrink-0 text-left sm:text-right">
          <p className="text-sm font-semibold text-white">
            {formatPrice(component.priceAmount, component.priceCurrency)}
          </p>
          <p
            className={`mt-1 text-xs ${
              component.stockQuantity > 0 ? "text-emerald-300" : "text-amber-300"
            }`}
          >
            {component.stockQuantity > 0
              ? `В наличии: ${component.stockQuantity}`
              : "Нет в наличии"}
          </p>
        </div>
      </div>

      <p className="mt-3 text-xs leading-5 text-slate-400">
        {component.matchConditions.length === 0
          ? "Подходит ко всем товарам этого типа."
          : `Почему подходит: ${component.matchConditions
              .map((condition) => {
                const expected = condition.expectedValues.join(" или ");
                return expected.length > 0
                  ? `${condition.characteristicName} — ${condition.actualValue} (условие: ${expected})`
                  : `${condition.characteristicName} — ${condition.actualValue}`;
              })
              .join("; ")}.`}
      </p>

      <Link
        href={`/catalog/products/${component.productId}`}
        className="mt-3 inline-flex rounded-xl border border-white/10 px-3 py-2 text-xs font-medium text-slate-200 transition hover:border-teal-400/50 hover:text-teal-200"
      >
        Открыть комплектующее
      </Link>
    </li>
  );
}

function NeedBlock({ need }: { need: ProductNeedCompatibility }) {
  const required = need.requirementMode === "RequiredForAssembly";

  return (
    <article
      className={`rounded-2xl border p-4 ${
        required
          ? "border-amber-500/30 bg-amber-500/[0.07]"
          : "border-white/10 bg-white/[0.03]"
      }`}
    >
      <div className="flex flex-col justify-between gap-2 sm:flex-row sm:items-start">
        <div>
          <h4 className="font-semibold text-white">{need.name}</h4>
          <p className="mt-1 text-xs text-slate-400">
            {required ? "Требуется для монтажа" : "Дополнительная комплектация"}
          </p>
        </div>

        <span
          className={`w-fit rounded-full px-2.5 py-1 text-xs font-medium ${
            required
              ? "bg-amber-500/15 text-amber-200"
              : "bg-slate-500/15 text-slate-300"
          }`}
        >
          Обычно {need.defaultQuantityPerUnit} шт./ед.
        </span>
      </div>

      {need.compatibleComponents.length === 0 ? (
        <p className="mt-4 rounded-xl border border-amber-500/20 bg-black/20 p-3 text-sm text-amber-200">
          Для этой потребности подходящих комплектующих по действующим правилам
          не найдено.
        </p>
      ) : (
        <ul className="mt-4 grid gap-3 lg:grid-cols-2">
          {need.compatibleComponents.map((component) => (
            <ComponentCard key={component.productId} component={component} />
          ))}
        </ul>
      )}
    </article>
  );
}

export function CatalogAssistantProductCompatibility({
  productId,
  defaultExpanded = false,
}: {
  productId: string;
  defaultExpanded?: boolean;
}) {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const compatibilityQuery = useQuery({
    queryKey: ["product-component-compatibility", productId],
    queryFn: () => getProductComponentCompatibility(productId),
    enabled: expanded,
  });
  const panelId = `assistant-components-${productId}`;
  const needs = compatibilityQuery.data?.needs ?? [];
  const accessories = compatibilityQuery.data?.accessories ?? [];
  const requiredNeeds = needs.filter(
    (need) => need.requirementMode === "RequiredForAssembly",
  );
  const optionalNeeds = needs.filter(
    (need) => need.requirementMode !== "RequiredForAssembly",
  );

  return (
    <div className="mt-4 border-t border-white/10 pt-4">
      <button
        type="button"
        aria-expanded={expanded}
        aria-controls={panelId}
        onClick={() => setExpanded((current) => !current)}
        className="inline-flex items-center gap-2 rounded-xl border border-teal-500/30 bg-teal-500/10 px-4 py-2 text-sm font-medium text-teal-200 transition hover:bg-teal-500/20"
      >
        {expanded ? "Скрыть комплектующие" : "Показать комплектующие"}
        <span aria-hidden="true">{expanded ? "−" : "+"}</span>
      </button>

      {expanded && (
        <div id={panelId} className="mt-4">
          {compatibilityQuery.isPending && (
            <p className="rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-slate-300">
              Подбираем совместимые комплектующие...
            </p>
          )}

          {compatibilityQuery.isError && (
            <div className="rounded-2xl border border-red-500/30 bg-red-500/10 p-4">
              <p className="text-sm text-red-200">
                {getErrorMessage(compatibilityQuery.error)}
              </p>
              <button
                type="button"
                onClick={() => compatibilityQuery.refetch()}
                className="mt-3 rounded-xl border border-red-400/30 px-3 py-2 text-xs font-medium text-red-100"
              >
                Повторить
              </button>
            </div>
          )}

          {compatibilityQuery.isSuccess &&
            needs.length === 0 &&
            accessories.length === 0 && (
              <p className="rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-slate-400">
                Для этого товара совместимые комплектующие пока не настроены.
              </p>
            )}

          {compatibilityQuery.isSuccess &&
            (needs.length > 0 || accessories.length > 0) && (
              <div className="grid gap-4">
                {requiredNeeds.length > 0 && (
                  <section className="grid gap-3">
                    <div>
                      <h3 className="font-semibold text-amber-100">
                        Обязательно для монтажа
                      </h3>
                      <p className="mt-1 text-xs text-slate-400">
                        Выберите подходящий вариант при формировании проекта.
                      </p>
                    </div>
                    {requiredNeeds.map((need) => (
                      <NeedBlock key={need.needDefinitionId} need={need} />
                    ))}
                  </section>
                )}

                {optionalNeeds.length > 0 && (
                  <section className="grid gap-3">
                    <h3 className="font-semibold text-white">
                      Дополнительная комплектация
                    </h3>
                    {optionalNeeds.map((need) => (
                      <NeedBlock key={need.needDefinitionId} need={need} />
                    ))}
                  </section>
                )}

                {accessories.length > 0 && (
                  <section className="rounded-2xl border border-teal-500/20 bg-teal-500/[0.05] p-4">
                    <h3 className="font-semibold text-teal-100">
                      Совместимые аксессуары
                    </h3>
                    <p className="mt-1 text-xs text-slate-400">
                      Не являются обязательными и приобретаются по выбору.
                    </p>
                    <ul className="mt-4 grid gap-3 lg:grid-cols-2">
                      {accessories.map((component) => (
                        <ComponentCard
                          key={component.productId}
                          component={component}
                        />
                      ))}
                    </ul>
                  </section>
                )}
              </div>
            )}
        </div>
      )}
    </div>
  );
}
