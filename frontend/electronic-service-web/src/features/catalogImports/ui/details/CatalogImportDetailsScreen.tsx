"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { getCatalogImportBatch } from "../../api/getCatalogImportBatch";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { PageHeader } from "@/shared/ui/PageHeader";
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
    <div className="grid gap-6">
      <PageHeader
        title="Пакет импорта"
        description="Состояние обработки, результаты проверки и доступные действия."
      />

      <div>
        <Link
          href={backHref}
          className="inline-flex rounded-xl bg-white/[0.06] px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-white/[0.1]"
        >
          ← {backLabel}
        </Link>
      </div>

      {batchQuery.isLoading && (
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6 text-slate-300">
          Загружаем пакет импорта...
        </section>
      )}

      {batchQuery.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getApiErrorMessage(
            batchQuery.error,
            "Не удалось загрузить пакет импорта.",
          )}
        </section>
      )}

      {batchQuery.data && (
        <CatalogImportDetailsContent batch={batchQuery.data} />
      )}
    </div>
  );
}
