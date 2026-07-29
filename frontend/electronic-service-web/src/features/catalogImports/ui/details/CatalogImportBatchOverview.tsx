"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { deleteCatalogImportBatch } from "../../api/deleteCatalogImportBatch";
import { downloadCatalogImportFile } from "../../api/downloadCatalogImportFile";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type { CatalogImportBatchDetails } from "../../model/types";
import { CatalogImportStatusBadge } from "../CatalogImportStatusBadge";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatFileSize } from "@/shared/lib/formatters";

interface CatalogImportBatchOverviewProps {
  batch: CatalogImportBatchDetails;
}

export function CatalogImportBatchOverview({
  batch,
}: CatalogImportBatchOverviewProps) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const downloadMutation = useMutation({
    mutationFn: () =>
      downloadCatalogImportFile(batch.batchId, batch.originalFileName),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteCatalogImportBatch(batch.batchId),

    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: catalogImportQueryKeys.myRoot,
      });

      queryClient.removeQueries({
        queryKey: catalogImportQueryKeys.details(batch.batchId),
      });

      router.replace("/catalog/imports");
    },
  });

  function handleDelete(): void {
    const confirmed = window.confirm(
      `Удалить пакет «${batch.originalFileName}»?\n\nИсходный файл и staging-строки будут удалены без возможности восстановления.`,
    );

    if (confirmed) {
      deleteMutation.mutate();
    }
  }

  return (
    <>
      {downloadMutation.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getApiErrorMessage(
            downloadMutation.error,
            "Не удалось скачать исходный Excel-файл.",
          )}
        </section>
      )}

      {deleteMutation.isError && (
        <section className="rounded-3xl border border-red-500/30 bg-red-500/10 p-6 text-red-200">
          {getApiErrorMessage(
            deleteMutation.error,
            "Не удалось удалить пакет импорта.",
          )}
        </section>
      )}

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
          <div className="min-w-0">
            <h2 className="break-words text-2xl font-bold text-white">
              {batch.originalFileName}
            </h2>

            <p className="mt-2 break-all text-sm text-slate-400">
              ID пакета: {batch.batchId}
            </p>

            <p className="mt-1 text-sm text-slate-500">
              Размер: {formatFileSize(batch.fileSizeBytes)}
            </p>
          </div>

          <CatalogImportStatusBadge status={batch.status} />
        </div>

        <div className="mt-6 flex flex-wrap gap-3">
          {batch.canDownloadFile && (
            <button
              type="button"
              disabled={downloadMutation.isPending}
              onClick={() => downloadMutation.mutate()}
              className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-teal-400 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {downloadMutation.isPending
                ? "Скачиваем..."
                : "Скачать исходный Excel"}
            </button>
          )}

          {batch.canDelete && (
            <button
              type="button"
              disabled={deleteMutation.isPending}
              onClick={handleDelete}
              className="rounded-2xl border border-red-500/30 bg-red-500/10 px-5 py-3 text-sm font-medium text-red-200 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {deleteMutation.isPending ? "Удаляем..." : "Удалить пакет"}
            </button>
          )}
        </div>
      </section>
    </>
  );
}
