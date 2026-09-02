"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { AppButton } from "@/shared/ui/AppButton";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { deleteCatalogImportBatch } from "../../api/deleteCatalogImportBatch";
import { downloadCatalogImportFile } from "../../api/downloadCatalogImportFile";
import { catalogImportQueryKeys } from "../../model/queryKeys";
import type { CatalogImportBatchDetails } from "../../model/types";
import { CatalogImportStatusBadge } from "../status";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatFileSize } from "@/shared/lib/formatters";

interface CatalogImportBatchOverviewProps {
  batch: CatalogImportBatchDetails;
  navigation?: ReactNode;
}

export function CatalogImportBatchOverview({
  batch,
  navigation,
}: CatalogImportBatchOverviewProps) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const headerRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const header = headerRef.current;
    const workspace = header?.closest<HTMLElement>("[data-import-workspace]");

    if (!header || !workspace) {
      return;
    }

    function updateHeaderHeight(): void {
      if (!header || !workspace) {
        return;
      }

      workspace.style.setProperty(
        "--import-workspace-header-height",
        `${Math.ceil(header.getBoundingClientRect().height)}px`,
      );
    }

    updateHeaderHeight();

    const observer = new ResizeObserver(updateHeaderHeight);
    observer.observe(header);

    return () => {
      observer.disconnect();
      workspace.style.removeProperty("--import-workspace-header-height");
    };
  }, []);

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
      <section
        ref={headerRef}
        aria-label="Сведения о пакете импорта"
        className={[
          "sticky top-[calc(var(--app-header-height,4rem)+0.5rem)] z-20",
          "min-w-0 self-start rounded-2xl",
          "border border-[var(--app-border)]",
          "bg-[var(--app-panel)]",
          "shadow-sm shadow-[var(--app-shadow)]",
        ].join(" ")}
      >
        <div className="grid min-w-0 gap-3 p-3 sm:p-4">
          <div className="flex min-w-0 flex-wrap items-center justify-between gap-2">
            <h2
              title={batch.originalFileName}
              className="min-w-0 flex-1 basis-48 truncate text-sm font-semibold text-[var(--app-text)] sm:text-base"
            >
              {batch.originalFileName}
            </h2>

            <CatalogImportStatusBadge status={batch.status} />
          </div>

          <dl className="flex flex-wrap gap-x-5 gap-y-2 text-xs sm:text-sm">
            <div className="flex items-baseline gap-1.5">
              <dt className="text-[var(--app-muted)]">Всего строк:</dt>
              <dd className="font-semibold tabular-nums text-[var(--app-text)]">
                {batch.rowsCount}
              </dd>
            </div>

            <div className="flex items-baseline gap-1.5">
              <dt className="text-[var(--app-muted)]">Корректных:</dt>
              <dd className="font-semibold tabular-nums text-[var(--app-success)]">
                {batch.validRowsCount}
              </dd>
            </div>

            <div className="flex items-baseline gap-1.5">
              <dt className="text-[var(--app-muted)]">С ошибками:</dt>
              <dd
                className={[
                  "font-semibold tabular-nums",
                  batch.errorRowsCount > 0
                    ? "text-[var(--app-danger)]"
                    : "text-[var(--app-text)]",
                ].join(" ")}
              >
                {batch.errorRowsCount}
              </dd>
            </div>
          </dl>
        </div>

        {navigation && (
          <div className="min-w-0 border-t border-[var(--app-border)] p-1.5 sm:px-3">
            {navigation}
          </div>
        )}
      </section>

      {downloadMutation.isError && (
        <section
          role="alert"
          className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            downloadMutation.error,
            "Не удалось скачать исходный Excel-файл.",
          )}
        </section>
      )}

      {deleteMutation.isError && (
        <section
          role="alert"
          className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]"
        >
          {getApiErrorMessage(
            deleteMutation.error,
            "Не удалось удалить пакет импорта.",
          )}
        </section>
      )}

      <section
        aria-label="Исходный файл и действия"
        className="min-w-0 rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4"
      >
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-[var(--app-muted)]">
            Исходный Excel · {formatFileSize(batch.fileSizeBytes)}
          </p>

          <div className="flex flex-wrap gap-2">
            {batch.canDownloadFile && (
              <AppButton
                variant="primary"
                size="sm"
                loading={downloadMutation.isPending}
                onClick={() => downloadMutation.mutate()}
              >
                {downloadMutation.isPending
                  ? "Скачиваем..."
                  : "Скачать исходный Excel"}
              </AppButton>
            )}

            {batch.canDelete && (
              <AppButton
                variant="danger"
                size="sm"
                loading={deleteMutation.isPending}
                onClick={handleDelete}
              >
                {deleteMutation.isPending ? "Удаляем..." : "Удалить пакет"}
              </AppButton>
            )}
          </div>
        </div>

        <details className="mt-3 text-sm">
          <summary className="w-fit cursor-pointer rounded text-[var(--app-muted)]">
            Сведения о файле
          </summary>

          <dl className="mt-3 grid min-w-0 gap-3">
            <div>
              <dt className="text-xs text-[var(--app-muted)]">
                Полное имя файла
              </dt>
              <dd className="mt-1 break-all text-[var(--app-text)]">
                {batch.originalFileName}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-[var(--app-muted)]">
                Идентификатор пакета
              </dt>
              <dd className="mt-1 break-all font-mono text-xs text-[var(--app-text)]">
                {batch.batchId}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-[var(--app-muted)]">Версия пакета</dt>
              <dd className="mt-1 tabular-nums text-[var(--app-text)]">
                {batch.version}
              </dd>
            </div>
          </dl>
        </details>
      </section>
    </>
  );
}
