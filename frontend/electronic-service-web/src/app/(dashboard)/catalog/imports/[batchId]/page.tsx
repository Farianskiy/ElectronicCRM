"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { deleteCatalogImportBatch } from "@/features/catalogImports/api/deleteCatalogImportBatch";
import { downloadCatalogImportFile } from "@/features/catalogImports/api/downloadCatalogImportFile";
import { getCatalogImportBatch } from "@/features/catalogImports/api/getCatalogImportBatch";
import { CatalogImportStatusBadge } from "@/features/catalogImports/ui/CatalogImportStatusBadge";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatDate, formatFileSize } from "@/shared/lib/formatters";
import { PageHeader } from "@/shared/ui/PageHeader";
import { CatalogImportRowsPreview } from "@/features/catalogImports/ui/CatalogImportRowsPreview";
import { CatalogImportMappingEditor } from "@/features/catalogImports/ui/CatalogImportMappingEditor";
import { CatalogImportSubmitPanel } from "@/features/catalogImports/ui/CatalogImportSubmitPanel";
import { CatalogImportReviewPanel } from "@/features/catalogImports/ui/CatalogImportReviewPanel";
import { CatalogImportReviewDecisionPanel } from "@/features/catalogImports/ui/CatalogImportReviewDecisionPanel";
import { CatalogImportBatchHistory } from "@/features/catalogImports/ui/CatalogImportBatchHistory";
import { CatalogImportAppliedProducts } from "@/features/catalogImports/ui/CatalogImportAppliedProducts";
import { CatalogImportErrorReportButton } from "@/features/catalogImports/ui/CatalogImportErrorReportButton";

function getBatchIdFromParams(params: ReturnType<typeof useParams>): string {
  const batchId = params.batchId;

  if (typeof batchId === "string") {
    return batchId;
  }

  if (Array.isArray(batchId)) {
    return batchId[0] ?? "";
  }

  return "";
}

function InfoCard({
  label,
  value,
  valueClassName = "text-white",
}: {
  label: string;
  value: string;
  valueClassName?: string;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>
      <p className={`mt-1 text-lg font-semibold ${valueClassName}`}>{value}</p>
    </div>
  );
}

export default function CatalogImportDetailsPage() {
  const params = useParams();
  const router = useRouter();
  const queryClient = useQueryClient();

  const searchParams = useSearchParams();

  const fromReviewQueue = searchParams.get("from") === "review-queue";

  const backHref = fromReviewQueue
    ? "/catalog/import-reviews"
    : "/catalog/imports";

  const backLabel = fromReviewQueue
    ? "Назад к очереди проверки"
    : "Назад к импортам";

  const batchId = getBatchIdFromParams(params);

  const batchQuery = useQuery({
    queryKey: ["catalog-import-batches", "details", batchId],
    queryFn: () => getCatalogImportBatch(batchId),
    enabled: batchId.length > 0,
  });

  const batch = batchQuery.data;

  const downloadMutation = useMutation({
    mutationFn: async () => {
      if (!batch) {
        throw new Error("Пакет импорта ещё не загружен.");
      }

      await downloadCatalogImportFile(batch.batchId, batch.originalFileName);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!batch) {
        throw new Error("Пакет импорта ещё не загружен.");
      }

      await deleteCatalogImportBatch(batch.batchId);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["catalog-import-batches", "my"],
      });

      router.replace("/catalog/imports");
    },
  });

  function handleDelete(): void {
    if (!batch) {
      return;
    }

    const confirmed = window.confirm(
      `Удалить пакет «${batch.originalFileName}»?\n\nИсходный файл и staging-строки будут удалены без возможности восстановления.`,
    );

    if (confirmed) {
      deleteMutation.mutate();
    }
  }

  const canEditRows =
    Boolean(batch?.canEdit) &&
    (batch?.status === "NeedsCorrection" ||
      batch?.status === "Ready" ||
      batch?.status === "ChangesRequested");

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

      {batch && (
        <>
          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-start">
              <div>
                <h2 className="text-2xl font-bold text-white">
                  {batch.originalFileName}
                </h2>

                <p className="mt-2 text-sm text-slate-400">
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

          {batch.changesRequestComment && (
            <section className="rounded-3xl border border-orange-500/30 bg-orange-500/10 p-6">
              <h2 className="text-lg font-semibold text-orange-100">
                Пакет возвращён на исправление
              </h2>

              <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-orange-200">
                {batch.changesRequestComment}
              </p>

              <p className="mt-3 text-xs text-orange-300/70">
                Дата решения: {formatDate(batch.changesRequestedAtUtc)}
              </p>
            </section>
          )}

          {batch.rejectionReason && (
            <section className="rounded-3xl border border-rose-500/30 bg-rose-500/10 p-6">
              <h2 className="text-lg font-semibold text-rose-100">
                Пакет окончательно отклонён
              </h2>

              <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-rose-200">
                {batch.rejectionReason}
              </p>

              <p className="mt-3 text-xs text-rose-300/70">
                Дата решения: {formatDate(batch.rejectedAtUtc)}
              </p>
            </section>
          )}

          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Результаты обработки
            </h2>

            <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <InfoCard
                label="Всего строк"
                value={batch.rowsCount.toString()}
              />

              <InfoCard
                label="Корректных строк"
                value={batch.validRowsCount.toString()}
                valueClassName="text-green-300"
              />

              <InfoCard
                label="Строк с ошибками"
                value={batch.errorRowsCount.toString()}
                valueClassName={
                  batch.errorRowsCount > 0 ? "text-red-300" : "text-white"
                }
              />

              <InfoCard label="Версия" value={batch.version.toString()} />
            </div>
          </section>

          {batch.errorRowsCount > 0 && (
            <section className="rounded-3xl border border-red-500/20 bg-red-500/[0.04] p-6">
              <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-center">
                <div>
                  <h2 className="text-xl font-semibold text-red-100">
                    В пакете обнаружены ошибки
                  </h2>

                  <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">
                    Скачайте Excel-отчёт с исходными значениями, номерами строк
                    и подробными результатами валидации.
                  </p>
                </div>

                <CatalogImportErrorReportButton
                  batchId={batch.batchId}
                  originalFileName={batch.originalFileName}
                  errorRowsCount={batch.errorRowsCount}
                />
              </div>
            </section>
          )}

          <CatalogImportSubmitPanel
            batchId={batch.batchId}
            originalFileName={batch.originalFileName}
            rowsCount={batch.rowsCount}
            validRowsCount={batch.validRowsCount}
            errorRowsCount={batch.errorRowsCount}
            canSubmit={batch.canSubmit}
          />

          {batch.canEdit && (
            <CatalogImportMappingEditor batchId={batch.batchId} />
          )}

          <CatalogImportBatchHistory batchId={batch.batchId} />

          {batch.status === "Applied" && (
            <CatalogImportAppliedProducts batchId={batch.batchId} />
          )}

          <CatalogImportRowsPreview
            batchId={batch.batchId}
            productTypeId={batch.productTypeId}
            canEditRows={canEditRows}
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

          {batch.status === "Submitted" && (
            <section className="rounded-3xl border border-blue-500/30 bg-blue-500/[0.07] p-6">
              <h2 className="text-lg font-semibold text-blue-100">
                Пакет отправлен на проверку
              </h2>

              <p className="mt-2 text-sm leading-6 text-slate-300">
                Пакет находится в общей очереди технических специалистов.
                Редактирование возобновится только в том случае, если Technical
                вернёт его на исправление.
              </p>

              <p className="mt-3 text-sm text-blue-200">
                Дата отправки: {formatDate(batch.submittedAtUtc)}
              </p>
            </section>
          )}

          <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
            <h2 className="text-xl font-semibold text-white">
              Доступные операции backend
            </h2>

            <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <PermissionCard label="Редактирование" allowed={batch.canEdit} />
              <PermissionCard label="Отправка" allowed={batch.canSubmit} />
              <PermissionCard label="Применение" allowed={batch.canApply} />
              <PermissionCard
                label="Возврат на исправление"
                allowed={batch.canRequestChanges}
              />
            </div>
          </section>
        </>
      )}
    </div>
  );
}

function PermissionCard({
  label,
  allowed,
}: {
  label: string;
  allowed: boolean;
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p
        className={
          allowed
            ? "mt-2 text-sm font-semibold text-green-300"
            : "mt-2 text-sm font-semibold text-slate-500"
        }
      >
        {allowed ? "Доступно" : "Недоступно"}
      </p>
    </div>
  );
}
