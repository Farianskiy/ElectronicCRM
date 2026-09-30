"use client";

import { useRef, useState, type ReactNode } from "react";
import { AppButton } from "@/shared/ui/AppButton";

interface CatalogImportDiagnosticListProps<T> {
  items: readonly T[];
  ariaLabel: string;
  renderItem: (item: T, index: number) => ReactNode;
}

const pageSize = 5;

export function CatalogImportDiagnosticList<T>({
  items,
  ariaLabel,
  renderItem,
}: CatalogImportDiagnosticListProps<T>) {
  const [requestedPage, setRequestedPage] = useState(1);
  const containerRef = useRef<HTMLDivElement>(null);

  const totalPages = Math.max(1, Math.ceil(items.length / pageSize));
  const page = Math.min(requestedPage, totalPages);
  const startIndex = (page - 1) * pageSize;
  const endIndex = Math.min(startIndex + pageSize, items.length);
  const visibleItems = items.slice(startIndex, endIndex);

  function changePage(nextPage: number): void {
    setRequestedPage(Math.max(1, Math.min(nextPage, totalPages)));
    containerRef.current?.focus({ preventScroll: true });
    containerRef.current?.scrollIntoView({ block: "start" });
  }

  function renderNavigation(position: "top" | "bottom") {
    return (
      <nav
        aria-label={`${ariaLabel}: ${position === "top" ? "верхняя" : "нижняя"} навигация`}
        className="flex flex-col gap-3 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-3 sm:flex-row sm:items-center sm:justify-between"
      >
        <p
          role={position === "top" ? "status" : undefined}
          className="text-sm text-[var(--app-muted)]"
        >
          Примеры {startIndex + 1}–{endIndex} из {items.length}
        </p>

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-2">
            <AppButton
              variant="secondary"
              size="sm"
              disabled={page === 1}
              onClick={() => changePage(page - 1)}
              aria-label="Предыдущая страница примеров"
            >
              Назад
            </AppButton>

            <span className="text-sm text-[var(--app-muted)]">
              {page} / {totalPages}
            </span>

            <AppButton
              variant="secondary"
              size="sm"
              disabled={page === totalPages}
              onClick={() => changePage(page + 1)}
              aria-label="Следующая страница примеров"
            >
              Далее
            </AppButton>
          </div>
        )}
      </nav>
    );
  }

  if (items.length === 0) {
    return (
      <p className="mt-4 text-sm text-[var(--app-muted)]">
        Диагностические примеры отсутствуют.
      </p>
    );
  }

  return (
    <div
      ref={containerRef}
      role="region"
      aria-label={ariaLabel}
      tabIndex={-1}
      className="mt-4 grid min-w-0 scroll-mt-24 gap-4 rounded-2xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
    >
      {renderNavigation("top")}

      <div className="grid min-w-0 gap-4">
        {visibleItems.map((item, index) =>
          renderItem(item, startIndex + index),
        )}
      </div>

      {totalPages > 1 && renderNavigation("bottom")}
    </div>
  );
}
