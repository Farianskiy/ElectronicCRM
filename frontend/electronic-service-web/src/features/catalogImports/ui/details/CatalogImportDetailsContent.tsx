"use client";

import { useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AppButton } from "@/shared/ui/AppButton";
import { AppSelect } from "@/shared/ui/AppSelect";
import type {
  AnalyzeCatalogImportBatchResponse,
  CatalogImportBatchDetails,
} from "../../model/types";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import { CatalogImportDiagnostics } from "./CatalogImportDiagnostics";
import { CatalogImportBatchHistory } from "../history";
import { CatalogImportMappingEditor } from "../mapping";
import { CatalogImportAppliedProducts } from "../results";
import {
  CatalogImportReviewDecisionPanel,
  CatalogImportReviewPanel,
  CatalogImportSubmitPanel,
} from "../review";
import { CatalogImportRowsPreview } from "../rows";
import { CatalogImportBatchOverview } from "./CatalogImportBatchOverview";
import { CatalogImportDecisionNotices } from "./CatalogImportDecisionNotices";
import { CatalogImportProcessingSummary } from "./CatalogImportProcessingSummary";

interface CatalogImportDetailsContentProps {
  batch: CatalogImportBatchDetails;
}

type ImportViewId =
  | "import-overview"
  | "import-preparation"
  | "import-diagnostics"
  | "import-rows"
  | "import-history"
  | "import-products";

interface ImportView {
  id: ImportViewId;
  label: string;
  count?: number;
  content: ReactNode;
}

export function CatalogImportDetailsContent({
  batch,
}: CatalogImportDetailsContentProps) {
  const queryClient = useQueryClient();

  const [analysis, setAnalysis] =
    useState<AnalyzeCatalogImportBatchResponse | null>(() => {
      return (
        queryClient.getQueryData<AnalyzeCatalogImportBatchResponse | null>(
          catalogImportQueryKeys.analysis(batch.batchId),
        ) ?? null
      );
    });

  function handleAnalysisChange(
    nextAnalysis: AnalyzeCatalogImportBatchResponse | null,
  ): void {
    setAnalysis(nextAnalysis);

    queryClient.setQueryData(
      catalogImportQueryKeys.analysis(batch.batchId),
      nextAnalysis,
    );
  }

  const initialView: ImportViewId =
    batch.status === "Applied"
      ? "import-products"
      : batch.canEdit &&
          (batch.status === "Uploaded" || batch.status === "MappingRequired")
        ? "import-preparation"
        : "import-rows";

  const [navigation, setNavigation] = useState<{
    active: ImportViewId;
    visited: ImportViewId[];
  }>(() => ({
    active: initialView,
    visited: [initialView],
  }));

  const viewRefs = useRef<Partial<Record<ImportViewId, HTMLElement | null>>>(
    {},
  );

  const scrollPositions = useRef<Partial<Record<ImportViewId, number>>>({});

  const previousViewRef = useRef<ImportViewId | null>(null);

  const canEditRows =
    batch.canEdit &&
    (batch.status === "NeedsCorrection" ||
      batch.status === "Ready" ||
      batch.status === "ChangesRequested");

  const views: ImportView[] = [
    {
      id: "import-overview",
      label: "Обзор",
      content: (
        <>
          <CatalogImportProcessingSummary batch={batch} />

          <CatalogImportSubmitPanel
            batchId={batch.batchId}
            originalFileName={batch.originalFileName}
            rowsCount={batch.rowsCount}
            validRowsCount={batch.validRowsCount}
            errorRowsCount={batch.errorRowsCount}
            canSubmit={batch.canSubmit}
          />

          <CatalogImportReviewPanel
            batchId={batch.batchId}
            originalFileName={batch.originalFileName}
            status={batch.status}
            reviewedByUserId={batch.reviewedByUserId}
            reviewedAtUtc={batch.reviewedAtUtc}
          />

          <CatalogImportReviewDecisionPanel
            batchId={batch.batchId}
            originalFileName={batch.originalFileName}
            rowsCount={batch.rowsCount}
            validRowsCount={batch.validRowsCount}
            errorRowsCount={batch.errorRowsCount}
            canRequestChanges={batch.canRequestChanges}
            canReject={batch.canReject}
            canApply={batch.canApply}
          />
        </>
      ),
    },
  ];

  if (batch.canEdit) {
    views.push(
      {
        id: "import-preparation",
        label: "Подготовка",
        content: (
          <CatalogImportMappingEditor
            batchId={batch.batchId}
            onAnalysisChange={handleAnalysisChange}
          />
        ),
      },
      {
        id: "import-diagnostics",
        label: "Диагностика",
        content: (
          <CatalogImportDiagnostics
            batchId={batch.batchId}
            productTypeId={batch.productTypeId}
            analysis={analysis}
            onAnalysisChange={handleAnalysisChange}
          />
        ),
      },
    );
  }

  views.push(
    {
      id: "import-rows",
      label: "Строки",
      count: batch.rowsCount,
      content: (
        <CatalogImportRowsPreview
          batchId={batch.batchId}
          productTypeId={batch.productTypeId}
          expectedVersion={batch.version}
          canEditRows={canEditRows}
        />
      ),
    },
    {
      id: "import-history",
      label: "История",
      content: <CatalogImportBatchHistory batchId={batch.batchId} />,
    },
  );

  if (batch.status === "Applied") {
    views.push({
      id: "import-products",
      label: "Результат",
      content: <CatalogImportAppliedProducts batchId={batch.batchId} />,
    });
  }

  const activeView = views.some((view) => view.id === navigation.active)
    ? navigation.active
    : initialView;

  const serviceViews = views.filter(
    (view) => view.id !== "import-rows" && view.id !== "import-diagnostics",
  );

  const isServiceView = serviceViews.some((view) => view.id === activeView);

  function handleViewChange(value: string): void {
    const nextView = views.find((view) => view.id === value);

    if (!nextView || nextView.id === activeView) {
      return;
    }

    scrollPositions.current[activeView] = window.scrollY;

    setNavigation((current) => ({
      active: nextView.id,
      visited: current.visited.includes(nextView.id)
        ? current.visited
        : [...current.visited, nextView.id],
    }));
  }

  useLayoutEffect(() => {
    const previousView = previousViewRef.current;
    previousViewRef.current = activeView;

    if (previousView === null || previousView === activeView) {
      return;
    }

    const element = viewRefs.current[activeView];

    if (!element) {
      return;
    }

    const scrollMarginTop =
      Number.parseFloat(window.getComputedStyle(element).scrollMarginTop) || 0;

    const top =
      scrollPositions.current[activeView] ??
      Math.max(
        0,
        element.getBoundingClientRect().top + window.scrollY - scrollMarginTop,
      );

    element.focus({ preventScroll: true });
    window.scrollTo({ top, behavior: "instant" });
  }, [activeView]);

  return (
    <div data-import-workspace className="grid min-w-0 gap-6">
      <CatalogImportBatchOverview
        batch={batch}
        navigation={
          <div className="flex min-w-0 flex-wrap items-center gap-2 p-1">
            <AppButton
              variant={activeView === "import-rows" ? "primary" : "secondary"}
              size="sm"
              aria-current={activeView === "import-rows" ? "page" : undefined}
              onClick={() => handleViewChange("import-rows")}
            >
              Строки импорта · {batch.rowsCount}
            </AppButton>

            {views.some((view) => view.id === "import-diagnostics") && (
              <AppButton
                variant={
                  activeView === "import-diagnostics" ? "primary" : "secondary"
                }
                size="sm"
                aria-current={
                  activeView === "import-diagnostics" ? "page" : undefined
                }
                onClick={() => handleViewChange("import-diagnostics")}
              >
                Технический разбор
              </AppButton>
            )}

            <div className="w-full sm:ml-auto sm:w-72">
              <AppSelect
                ariaLabel="Служебные экраны пакета импорта"
                value={isServiceView ? activeView : ""}
                options={[
                  {
                    value: "",
                    label: "Настройка, история и действия",
                    disabled: true,
                  },
                  ...serviceViews.map((view) => ({
                    value: view.id,
                    label: view.label,
                  })),
                ]}
                onChange={handleViewChange}
              />
            </div>
          </div>
        }
      />

      <CatalogImportDecisionNotices batch={batch} />

      {views.map((view) => {
        const isActive = view.id === activeView;
        const wasOpened = navigation.visited.includes(view.id);
        const isService =
          view.id !== "import-rows" && view.id !== "import-diagnostics";

        return (
          <section
            key={view.id}
            ref={(element) => {
              viewRefs.current[view.id] = element;
            }}
            id={`${view.id}-panel`}
            aria-label={view.label}
            hidden={!isActive}
            tabIndex={-1}
            className={
              isActive
                ? "grid min-w-0 scroll-mt-[calc(var(--app-header-height,4rem)+var(--import-workspace-header-height,6rem)+1rem)] gap-6 rounded-2xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
                : "hidden"
            }
          >
            {isService && (
              <header className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4">
                <h2 className="text-base font-semibold text-[var(--app-text)]">
                  {view.label}
                </h2>

                <AppButton
                  variant="secondary"
                  size="sm"
                  onClick={() => handleViewChange("import-rows")}
                >
                  ← К строкам
                </AppButton>
              </header>
            )}

            {(isActive || wasOpened) && view.content}
          </section>
        );
      })}
    </div>
  );
}
