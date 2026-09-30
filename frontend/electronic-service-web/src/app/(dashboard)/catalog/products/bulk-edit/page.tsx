"use client";

import Link from "next/link";
import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import {
  hasCatalogProductBulkEditorFilters,
  parseCatalogProductBulkEditorFilters,
} from "@/features/catalogProducts/model/bulkEditorFilters";
import { CatalogProductBulkEditor } from "@/features/catalogProducts/ui/CatalogProductBulkEditor";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";

function BulkEditorPageContent() {
  const searchParams = useSearchParams();
  const filters = parseCatalogProductBulkEditorFilters(
    new URLSearchParams(searchParams.toString()),
  );
  const hasFilters = hasCatalogProductBulkEditorFilters(filters);

  return (
    <PageWorkspace
      eyebrow="Работа с каталогом"
      title="Массовое редактирование товаров"
      description="Редактирование товаров пакетами до 100 строк по фильтрам каталога."
      contentClassName="grid min-w-0 gap-6"
      actions={
        <Link href="/catalog/products" className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-2.5 text-sm font-semibold text-[var(--app-text)] hover:bg-[var(--app-surface-hover)]">
          ← Назад к каталогу
        </Link>
      }
    >
      {hasFilters ? (
        <CatalogProductBulkEditor filters={filters} />
      ) : (
        <section role="alert" className="rounded-3xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] p-6">
          <h2 className="font-semibold text-[var(--app-text)]">Сначала сформируйте выборку</h2>
          <p className="mt-2 text-sm leading-6 text-[var(--app-muted)]">
            Вернитесь в каталог, задайте хотя бы один фильтр и нажмите «Найти».
          </p>
        </section>
      )}
    </PageWorkspace>
  );
}

export default function CatalogProductBulkEditPage() {
  return (
    <Suspense fallback={<div className="p-6 text-sm text-[var(--app-muted)]">Открываем редактор...</div>}>
      <BulkEditorPageContent />
    </Suspense>
  );
}
