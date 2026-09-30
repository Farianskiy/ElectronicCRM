"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useMemo, useState } from "react";
import { getCatalogProductTypeCharacteristics } from "@/features/catalogMetadata/api/getCatalogProductTypeCharacteristics";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import {
  createComponentOffer,
  getComponentNeeds,
  getComponentOffers,
} from "../api/componentCompatibilityManagement";

function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && typeof error.response?.data === "string") {
    return error.response.data;
  }

  return "Не удалось сохранить совместимость комплектующего.";
}

export function ComponentOfferManager({
  productId,
  productTypeCode,
}: {
  productId: string;
  productTypeCode: string;
}) {
  const queryClient = useQueryClient();
  const offersQueryKey = ["component-offers", productId];
  const [needId, setNeedId] = useState("");
  const [values, setValues] = useState<Record<string, string>>({});
  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
  });
  const needsQuery = useQuery({
    queryKey: ["component-needs", "all"],
    queryFn: () => getComponentNeeds(),
  });
  const offersQuery = useQuery({
    queryKey: offersQueryKey,
    queryFn: () => getComponentOffers(productId),
  });
  const selectedNeed = useMemo(
    () => needsQuery.data?.find((need) => need.id === needId),
    [needId, needsQuery.data],
  );
  const characteristicsQuery = useQuery({
    queryKey: [
      "catalog-product-type-characteristics",
      selectedNeed?.mainProductTypeCode,
    ],
    queryFn: () =>
      getCatalogProductTypeCharacteristics(
        selectedNeed?.mainProductTypeCode ?? "",
      ),
    enabled: Boolean(selectedNeed),
  });
  const createMutation = useMutation({
    mutationFn: () =>
      createComponentOffer(
        productId,
        needId,
        Object.entries(values)
          .filter(([, value]) => value.trim().length > 0)
          .flatMap(([characteristicDefinitionId, value]) =>
            value
              .split(";")
              .map((item) => item.trim())
              .filter(Boolean)
              .map((item) => ({
                characteristicDefinitionId,
                value: item,
              })),
          ),
      ),
    onSuccess: async () => {
      setValues({});
      await queryClient.invalidateQueries({ queryKey: offersQueryKey });
    },
  });

  const currentProductType = productTypesQuery.data?.find(
    (productType) => productType.code === productTypeCode,
  );

  if (!currentProductType || currentProductType.kind !== "Component") {
    return null;
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (needId) {
      createMutation.mutate();
    }
  }

  return (
    <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 shadow-sm shadow-[var(--app-shadow)] sm:p-6">
      <h2 className="text-xl font-semibold text-[var(--app-text)]">
        Совместимость комплектующего
      </h2>

      <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
        Выберите потребность, которую закрывает комплектующее. Несколько
        допустимых значений одной характеристики разделяйте точкой с запятой:
        например, 1800; 2000.
      </p>

      <form onSubmit={handleSubmit} className="mt-5 grid gap-4">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-[var(--app-text)]">
            Потребность основного товара
          </span>

          <select
            value={needId}
            onChange={(event) => {
              setNeedId(event.target.value);
              setValues({});
            }}
            className="min-h-11 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 text-sm text-[var(--app-text)]"
          >
            <option value="">Выберите потребность</option>
            {(needsQuery.data ?? []).map((need) => (
              <option key={need.id} value={need.id}>
                {need.mainProductTypeName} · {need.name}
              </option>
            ))}
          </select>
        </label>

        {selectedNeed && (
          <div className="grid gap-3 md:grid-cols-2">
            {(characteristicsQuery.data ?? []).map((characteristic) => (
              <label
                key={characteristic.id}
                className="grid gap-2 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4"
              >
                <span className="text-sm font-medium text-[var(--app-text)]">
                  {characteristic.name}
                  {characteristic.unit ? `, ${characteristic.unit}` : ""}
                </span>

                <input
                  value={values[characteristic.id] ?? ""}
                  onChange={(event) =>
                    setValues((current) => ({
                      ...current,
                      [characteristic.id]: event.target.value,
                    }))
                  }
                  placeholder={`Значение (${characteristic.dataType}); варианты через ;`}
                  className="rounded-xl border border-[var(--app-border)] bg-[var(--app-panel)] px-3 py-2 text-sm text-[var(--app-text)]"
                />
              </label>
            ))}
          </div>
        )}

        <button
          type="submit"
          disabled={!needId || createMutation.isPending}
          className="justify-self-start rounded-xl bg-[var(--app-accent)] px-5 py-3 text-sm font-semibold text-white disabled:opacity-50"
        >
          Сохранить совместимость
        </button>
      </form>

      {createMutation.isError && (
        <p className="mt-4 text-sm text-[var(--app-danger)]">
          {getErrorMessage(createMutation.error)}
        </p>
      )}

      <div className="mt-6">
        <h3 className="text-sm font-semibold text-[var(--app-text)]">
          Настроенные предложения
        </h3>

        <ul className="mt-3 grid gap-2">
          {(offersQuery.data ?? []).map((offer) => (
            <li
              key={offer.id}
              className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 text-sm text-[var(--app-text)]"
            >
              {offer.needName} · условий: {offer.constraintsCount}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
