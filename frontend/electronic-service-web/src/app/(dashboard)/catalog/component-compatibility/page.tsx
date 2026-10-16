"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { RequirePermission } from "@/features/auth/ui/RequirePermission";
import { getCatalogProductTypes } from "@/features/catalogMetadata/api/getCatalogProductTypes";
import { ComponentCompatibilityImportPanel } from "@/features/componentCompatibility/ui/ComponentCompatibilityImportPanel";
import { ComponentCompatibilityRulesManager } from "@/features/componentCompatibility/ui/ComponentCompatibilityRulesManager";
import { ComponentNeedsManager } from "@/features/componentCompatibility/ui/ComponentNeedsManager";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";

type SectionId = "needs" | "rules" | "excel" | "check";

const sections: Array<{ id: SectionId; label: string }> = [
  { id: "needs", label: "Потребности" },
  { id: "rules", label: "Правила" },
  { id: "excel", label: "Excel" },
  { id: "check", label: "Проверка" },
];

export default function ComponentCompatibilityPage() {
  return (
    <RequirePermission permission="DictionariesManage">
      <ComponentCompatibilityContent />
    </RequirePermission>
  );
}

function ComponentCompatibilityContent() {
  const [section, setSection] = useState<SectionId>("needs");
  const [selectedTypeCode, setSelectedTypeCode] = useState("");
  const productTypesQuery = useQuery({
    queryKey: ["catalog-product-types"],
    queryFn: getCatalogProductTypes,
    staleTime: 5 * 60 * 1000,
  });
  const mainProductTypes = (productTypesQuery.data ?? []).filter(
    (item) => item.kind === "MainProduct",
  );
  const effectiveTypeCode =
    selectedTypeCode || mainProductTypes[0]?.code || "";
  const selectedType = mainProductTypes.find(
    (item) => item.code === effectiveTypeCode,
  );

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Комплектующие и совместимость"
        description="Настройте, какие комплектующие показывать для каждого типа, серии и сочетания характеристик."
      />

      <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6">
        <div className="grid gap-4 lg:grid-cols-[minmax(280px,440px)_1fr] lg:items-end">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-[var(--app-text)]">
              Основной тип товара
            </span>
            <AppSelect
              ariaLabel="Основной тип товара"
              value={effectiveTypeCode}
              options={mainProductTypes.map((item) => ({
                value: item.code,
                label: item.name,
              }))}
              onChange={setSelectedTypeCode}
              disabled={productTypesQuery.isLoading}
            />
          </label>
          <p className="text-sm leading-6 text-[var(--app-muted)]">
            Тип задаёт общую область. Серия, номинальный ток, реверсивность и
            другие характеристики задаются уже внутри правил и разделяют товары
            одного типа.
          </p>
        </div>

        <div className="mt-5 flex flex-wrap gap-2 border-t border-[var(--app-border)] pt-5">
          {sections.map((item) => (
            <button
              key={item.id}
              type="button"
              onClick={() => setSection(item.id)}
              className={`rounded-xl border px-4 py-2.5 text-sm font-semibold transition-colors ${
                section === item.id
                  ? "border-[var(--app-accent)] bg-[var(--app-accent-soft)] text-[var(--app-accent)]"
                  : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] hover:text-[var(--app-text)]"
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      </section>

      {!selectedType && !productTypesQuery.isLoading && (
        <section className="rounded-3xl border border-dashed border-[var(--app-border)] p-8 text-center text-sm text-[var(--app-muted)]">
          Сначала создайте хотя бы один основной тип товара.
        </section>
      )}

      {selectedType && section === "needs" && (
        <ComponentNeedsManager
          productTypeCode={selectedType.code}
          productTypeName={selectedType.name}
        />
      )}

      {selectedType && section === "rules" && (
        <ComponentCompatibilityRulesManager
          key={selectedType.code}
          productTypeCode={selectedType.code}
        />
      )}

      {section === "excel" && (
        <ComponentCompatibilityImportPanel productTypeCode={effectiveTypeCode} />
      )}

      {section === "check" && (
        <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6">
          <h2 className="text-lg font-semibold text-[var(--app-text)]">
            Проверка результата в проекте
          </h2>
          <ol className="mt-4 grid gap-3 text-sm leading-6 text-[var(--app-muted)]">
            <li>1. Откройте или создайте расчёт цен.</li>
            <li>2. Добавьте основной товар выбранного типа.</li>
            <li>
              3. Откройте его комплектующие: CRM покажет только правила,
              прошедшие по серии и остальным характеристикам.
            </li>
            <li>
              4. Выберите подходящую позицию. Дополнительные комплектующие можно
              оставить невыбранными.
            </li>
          </ol>
          <Link
            href="/catalog/price-calculations"
            className="mt-5 inline-flex rounded-xl border border-[var(--app-accent)] bg-[var(--app-accent-soft)] px-4 py-2.5 text-sm font-semibold text-[var(--app-accent)]"
          >
            Перейти к расчётам цен
          </Link>
        </section>
      )}
    </div>
  );
}
