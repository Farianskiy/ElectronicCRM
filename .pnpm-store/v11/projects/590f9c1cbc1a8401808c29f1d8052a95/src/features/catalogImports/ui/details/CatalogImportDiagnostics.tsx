"use client";

import { useId, useState, type ReactNode } from "react";
import { AppSelect } from "@/shared/ui/AppSelect";
import { WorkspaceTabs } from "@/shared/ui/WorkspaceTabs";
import type { AnalyzeCatalogImportBatchResponse } from "../../model/types";
import { CatalogImportRecognitionShadowPanel } from "../recognition/CatalogImportRecognitionShadowPanel";
import { CatalogImportManufacturerResolutionPanel } from "../manufacturers/CatalogImportManufacturerResolutionPanel";
import { CatalogImportManufacturerRecognitionShadowPanel } from "../manufacturers/CatalogImportManufacturerRecognitionShadowPanel";
import { CatalogImportProductTypeSuggestionShadowPanel } from "../productTypes/CatalogImportProductTypeSuggestionShadowPanel";
import { CatalogImportProductNameExplanationPanel } from "../explanation/CatalogImportProductNameExplanationPanel";

interface CatalogImportDiagnosticsProps {
  batchId: string;
  productTypeId?: string | null;
  analysis: AnalyzeCatalogImportBatchResponse | null;
  onAnalysisChange: (
    analysis: AnalyzeCatalogImportBatchResponse | null,
  ) => void;
}

type DiagnosticViewId =
  | "manufacturer-column"
  | "manufacturer-name"
  | "product-type"
  | "product-name"
  | "characteristics";

interface DiagnosticView {
  id: DiagnosticViewId;
  label: string;
  description: string;
  content: ReactNode;
}

export function CatalogImportDiagnostics({
  batchId,
  productTypeId,
  analysis,
  onAnalysisChange,
}: CatalogImportDiagnosticsProps) {
  const instanceId = useId();

  const [navigation, setNavigation] = useState<{
    active: DiagnosticViewId;
    visited: DiagnosticViewId[];
  }>({
    active: "manufacturer-column",
    visited: ["manufacturer-column"],
  });

  const views: DiagnosticView[] = [
    {
      id: "manufacturer-column",
      label: "Производители: колонка",
      description:
        "Сопоставление значений из колонки производителя со справочником.",
      content: (
        <CatalogImportManufacturerResolutionPanel
          batchId={batchId}
          productTypeId={analysis?.productTypeId ?? productTypeId}
          summary={analysis?.manufacturerResolutionSummary ?? null}
          onAnalysisChange={onAnalysisChange}
        />
      ),
    },
    {
      id: "manufacturer-name",
      label: "Производители: название",
      description:
        "Поиск производителя в наименовании товара и сравнение с результатом сопоставления.",
      content: (
        <CatalogImportManufacturerRecognitionShadowPanel
          shadow={analysis?.manufacturerRecognitionShadow ?? null}
        />
      ),
    },
    {
      id: "product-type",
      label: "Тип товара",
      description:
        "Предложения по определению типа товара на основании распознавания.",
      content: (
        <CatalogImportProductTypeSuggestionShadowPanel
          shadow={analysis?.productTypeSuggestionShadow ?? null}
        />
      ),
    },
    {
      id: "product-name",
      label: "Наименования",
      description: "Подробный разбор исходных наименований товаров.",
      content: (
        <CatalogImportProductNameExplanationPanel
          explanation={analysis?.productNameExplanation ?? null}
        />
      ),
    },
    {
      id: "characteristics",
      label: "Характеристики",
      description:
        "Распознанные характеристики и результаты дополнения данных.",
      content: (
        <CatalogImportRecognitionShadowPanel
          recognitionShadow={analysis?.recognitionShadow ?? null}
          recognitionEnrichment={analysis?.recognitionEnrichment ?? null}
        />
      ),
    },
  ];

  const activeView =
    views.find((view) => view.id === navigation.active) ?? views[0];

  function handleViewChange(value: string): void {
    const view = views.find((item) => item.id === value);

    if (!view) {
      return;
    }

    setNavigation((current) => {
      if (current.active === view.id) {
        return current;
      }

      return {
        active: view.id,
        visited: current.visited.includes(view.id)
          ? current.visited
          : [...current.visited, view.id],
      };
    });
  }

  function getTabId(viewId: DiagnosticViewId): string {
    return `diagnostic-${instanceId}-${viewId}`;
  }

  return (
    <div className="grid min-w-0 gap-4">
      <div className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
        <p className="mb-3 text-sm font-semibold text-[var(--app-text)]">
          Вид разбора
        </p>

        <div className="hidden 2xl:block">
          <WorkspaceTabs
            ariaLabel="Виды диагностики импорта"
            tabs={views.map((view) => ({
              id: getTabId(view.id),
              label: view.label,
              panelId: `${getTabId(view.id)}-panel`,
            }))}
            activeTab={getTabId(activeView.id)}
            onTabChange={(tabId) => {
              const view = views.find((item) => getTabId(item.id) === tabId);

              if (view) {
                handleViewChange(view.id);
              }
            }}
          />
        </div>

        <div className="2xl:hidden">
          <AppSelect
            ariaLabel="Вид разбора"
            value={activeView.id}
            options={views.map((view) => ({
              value: view.id,
              label: view.label,
            }))}
            onChange={handleViewChange}
          />
        </div>

        <p className="mt-3 text-sm leading-6 text-[var(--app-muted)]">
          {activeView.description}
        </p>
      </div>

      {analysis === null && (
        <p role="status" className="text-sm leading-6 text-[var(--app-muted)]">
          Результат анализа пока недоступен. Запуск анализа и сообщения о его
          выполнении находятся на экране «Настройка файла». Открытие
          технического разбора само по себе не запускает повторный анализ.
        </p>
      )}

      {views.map((view) => (
        <div
          key={view.id}
          id={`${getTabId(view.id)}-panel`}
          role="tabpanel"
          aria-label={view.label}
          hidden={view.id !== activeView.id}
          tabIndex={0}
          className={
            view.id === activeView.id
              ? "grid min-w-0 gap-4 rounded-2xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
              : "hidden"
          }
        >
          {navigation.visited.includes(view.id) && view.content}
        </div>
      ))}
    </div>
  );
}
