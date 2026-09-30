import type { CatalogImportBatchDetails } from "../../model/types";
import { formatDate } from "@/shared/lib/formatters";

interface CatalogImportDecisionNoticesProps {
  batch: CatalogImportBatchDetails;
}

export function CatalogImportDecisionNotices({
  batch,
}: CatalogImportDecisionNoticesProps) {
  return (
    <>
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

      {batch.status === "Submitted" && (
        <section className="rounded-3xl border border-blue-500/30 bg-blue-500/[0.07] p-6">
          <h2 className="text-lg font-semibold text-blue-100">
            Пакет отправлен на проверку
          </h2>

          <p className="mt-2 text-sm leading-6 text-slate-300">
            Пакет находится в общей очереди технических специалистов.
            Редактирование станет доступно снова, только если Technical вернёт
            пакет на исправление.
          </p>

          <p className="mt-3 text-sm text-blue-200">
            Дата отправки: {formatDate(batch.submittedAtUtc)}
          </p>
        </section>
      )}
    </>
  );
}
