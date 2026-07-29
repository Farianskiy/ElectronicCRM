import type { CatalogImportBatchDetails } from "../../model/types";
import { CatalogImportErrorReportButton } from "../CatalogImportErrorReportButton";

interface CatalogImportProcessingSummaryProps {
  batch: CatalogImportBatchDetails;
}

interface SummaryCardProps {
  label: string;
  value: string;
  valueClassName?: string;
}

function SummaryCard({
  label,
  value,
  valueClassName = "text-white",
}: SummaryCardProps) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-sm text-slate-400">{label}</p>

      <p className={`mt-1 text-lg font-semibold ${valueClassName}`}>{value}</p>
    </div>
  );
}

export function CatalogImportProcessingSummary({
  batch,
}: CatalogImportProcessingSummaryProps) {
  return (
    <>
      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Результаты обработки
        </h2>

        <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryCard label="Всего строк" value={batch.rowsCount.toString()} />

          <SummaryCard
            label="Корректных строк"
            value={batch.validRowsCount.toString()}
            valueClassName="text-green-300"
          />

          <SummaryCard
            label="Строк с ошибками"
            value={batch.errorRowsCount.toString()}
            valueClassName={
              batch.errorRowsCount > 0 ? "text-red-300" : "text-white"
            }
          />

          <SummaryCard label="Версия" value={batch.version.toString()} />
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
                Скачайте Excel-отчёт с исходными значениями, номерами строк и
                подробными результатами валидации.
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
    </>
  );
}
