"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { getCatalogImportBatch } from "../../api/getCatalogImportBatch";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { PageWorkspace } from "@/shared/ui/PageWorkspace";
import { CatalogImportDetailsContent } from "./CatalogImportDetailsContent";

interface CatalogImportDetailsScreenProps {
  batchId: string;
  backHref: string;
  backLabel: string;
}

export function CatalogImportDetailsScreen({
  batchId,
  backHref,
  backLabel,
}: CatalogImportDetailsScreenProps) {
  const batchQuery = useQuery({
    queryKey: catalogImportQueryKeys.details(batchId),
    queryFn: () => getCatalogImportBatch(batchId),
    enabled: batchId.length > 0,
  });

  return (
    <PageWorkspace
      eyebrow="Импорт каталога"
      title="Пакет импорта"
      description="Основная работа — со строками и техническим разбором. Настройка файла, сводка и действия, история доступны из меню шапки пакета."
      contentClassName="grid min-w-0 gap-6"
      actions={
        <Link
          href={backHref}
          className="inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--app-button-secondary-border)] bg-[var(--app-button-secondary-bg)] px-4 py-2 text-sm font-medium text-[var(--app-text)] transition-colors hover:bg-[var(--app-surface-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] motion-reduce:transition-none"
        >
          ← {backLabel}
        </Link>
      }
    >
      {batchQuery.isLoading && (
        <section
          role="status"
          className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6 text-[var(--app-muted)]"
        >
          Загружаем пакет импорта...
        </section>
      )}

      {batchQuery.isError && (
        <section
          role="alert"
          className="rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-6 text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            batchQuery.error,
            "Не удалось загрузить пакет импорта.",
          )}
        </section>
      )}

      {batchQuery.data && (
        <CatalogImportDetailsContent
          key={batchQuery.data.batchId}
          batch={batchQuery.data}
        />
      )}
    </PageWorkspace>
  );
}
