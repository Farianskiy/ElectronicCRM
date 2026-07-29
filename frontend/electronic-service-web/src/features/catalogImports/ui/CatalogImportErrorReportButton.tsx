"use client";

import { useMutation } from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { downloadCatalogImportErrorReport } from "../api/downloadCatalogImportErrorReport";

interface CatalogImportErrorReportButtonProps {
  batchId: string;
  originalFileName: string;
  errorRowsCount: number;
}

function createFallbackFileName(originalFileName: string): string {
  const baseName = originalFileName.replace(/\.xlsx$/i, "");

  return `${baseName || "catalog-import"}_ошибки.xlsx`;
}

export function CatalogImportErrorReportButton({
  batchId,
  originalFileName,
  errorRowsCount,
}: CatalogImportErrorReportButtonProps) {
  const downloadMutation = useMutation({
    mutationFn: () =>
      downloadCatalogImportErrorReport(
        batchId,
        createFallbackFileName(originalFileName),
      ),
  });

  if (errorRowsCount <= 0) {
    return null;
  }

  return (
    <div className="grid gap-3">
      <button
        type="button"
        disabled={downloadMutation.isPending}
        onClick={() => downloadMutation.mutate()}
        className="rounded-2xl border border-red-500/30 bg-red-500/10 px-5 py-3 text-sm font-semibold text-red-100 transition hover:bg-red-500/20 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {downloadMutation.isPending
          ? "Формируем отчёт..."
          : `Скачать отчёт об ошибках (${errorRowsCount})`}
      </button>

      {downloadMutation.isError && (
        <p className="max-w-md text-sm text-red-300">
          {getApiErrorMessage(
            downloadMutation.error,
            "Не удалось скачать отчёт об ошибках.",
          )}
        </p>
      )}
    </div>
  );
}
