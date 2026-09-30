"use client";

import { useMemo, useState } from "react";
import type {
  CatalogProductTypeKind,
  CatalogProductTypeMetadata,
} from "@/features/catalogMetadata/model/types";
import { AppSelect } from "@/shared/ui/AppSelect";

type ProductTypeScope = "Automatic" | CatalogProductTypeKind;

interface CatalogImportProductTypePickerProps {
  ariaLabel: string;
  value: string;
  productTypes: CatalogProductTypeMetadata[];
  onChange: (value: string) => void;
  disabled?: boolean;
  allowAutomatic?: boolean;
  emptyLabel?: string;
}

function getInitialScope(
  value: string,
  productTypes: CatalogProductTypeMetadata[],
  allowAutomatic: boolean,
): ProductTypeScope {
  const selectedProductType = productTypes.find(
    (productType) => productType.id === value,
  );

  if (selectedProductType) {
    return selectedProductType.kind;
  }

  return allowAutomatic ? "Automatic" : "MainProduct";
}

export function CatalogImportProductTypePicker({
  ariaLabel,
  value,
  productTypes,
  onChange,
  disabled = false,
  allowAutomatic = false,
  emptyLabel = "Выберите тип товара",
}: CatalogImportProductTypePickerProps) {
  const [scope, setScope] = useState<ProductTypeScope>(() =>
    getInitialScope(value, productTypes, allowAutomatic),
  );

  const selectedProductType = productTypes.find(
    (productType) => productType.id === value,
  );

  const effectiveScope = selectedProductType?.kind ?? scope;

  const filteredProductTypes = useMemo(() => {
    if (effectiveScope === "Automatic") {
      return [];
    }

    return productTypes.filter(
      (productType) => productType.kind === effectiveScope,
    );
  }, [effectiveScope, productTypes]);

  function handleScopeChange(nextScope: ProductTypeScope): void {
    if (nextScope === effectiveScope) {
      return;
    }

    setScope(nextScope);

    if (nextScope === "Automatic" || selectedProductType?.kind !== nextScope) {
      onChange("");
    }
  }

  const scopes: Array<{
    value: ProductTypeScope;
    label: string;
    description: string;
  }> = [
    ...(allowAutomatic
      ? [
          {
            value: "Automatic" as const,
            label: "Определять по строкам",
            description: "Для файлов, где встречаются разные типы товаров.",
          },
        ]
      : []),
    {
      value: "MainProduct",
      label: "Основные товары",
      description: "Самостоятельные изделия каталога.",
    },
    {
      value: "Component",
      label: "Комплектующие",
      description: "Дополнительные позиции для основных товаров.",
    },
  ];

  return (
    <div className="grid gap-3">
      <div
        className={[
          "grid gap-2",
          allowAutomatic ? "lg:grid-cols-3" : "sm:grid-cols-2",
        ].join(" ")}
      >
        {scopes.map((scopeOption) => {
          const isSelected = effectiveScope === scopeOption.value;

          return (
            <button
              key={scopeOption.value}
              type="button"
              aria-pressed={isSelected}
              disabled={disabled}
              onClick={() => handleScopeChange(scopeOption.value)}
              className={[
                "rounded-2xl border px-4 py-3 text-left transition",
                "disabled:cursor-not-allowed disabled:opacity-60",
                isSelected
                  ? "border-[var(--app-accent)] bg-[var(--app-accent-soft)]"
                  : "border-[var(--app-border)] bg-[var(--app-surface)] hover:border-[var(--app-border-strong)]",
              ].join(" ")}
            >
              <span
                className={[
                  "block text-sm font-semibold",
                  isSelected
                    ? "text-[var(--app-accent)]"
                    : "text-[var(--app-text)]",
                ].join(" ")}
              >
                {scopeOption.label}
              </span>

              <span className="mt-1 block text-xs leading-5 text-[var(--app-muted)]">
                {scopeOption.description}
              </span>
            </button>
          );
        })}
      </div>

      {effectiveScope === "Automatic" ? (
        <p className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-3 text-sm text-[var(--app-muted)]">
          Тип товара будет определяться отдельно для каждой строки файла.
        </p>
      ) : (
        <AppSelect
          ariaLabel={ariaLabel}
          value={value}
          disabled={disabled || filteredProductTypes.length === 0}
          onChange={onChange}
          options={[
            {
              value: "",
              label:
                filteredProductTypes.length === 0
                  ? "В этой категории пока нет типов"
                  : emptyLabel,
            },
            ...filteredProductTypes.map((productType) => ({
              value: productType.id,
              label: `${productType.name} · ${productType.code}`,
            })),
          ]}
        />
      )}
    </div>
  );
}
