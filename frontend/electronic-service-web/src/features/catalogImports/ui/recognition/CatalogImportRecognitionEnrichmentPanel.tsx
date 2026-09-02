import type { CatalogImportRecognitionEnrichment } from "../../model/types";

interface CatalogImportRecognitionEnrichmentPanelProps {
  recognitionEnrichment: CatalogImportRecognitionEnrichment | null;
}

interface EnrichmentMetricProps {
  label: string;
  value: number;
  description: string;
  tone: "Neutral" | "Success" | "Warning" | "Danger";
}

function getMetricClassName(tone: EnrichmentMetricProps["tone"]): string {
  switch (tone) {
    case "Success":
      return "border-green-500/25 bg-green-500/[0.06]";

    case "Warning":
      return "border-amber-500/25 bg-amber-500/[0.06]";

    case "Danger":
      return "border-red-500/25 bg-red-500/[0.06]";

    case "Neutral":
    default:
      return "border-white/10 bg-black/20";
  }
}

function EnrichmentMetric({
  label,
  value,
  description,
  tone,
}: EnrichmentMetricProps) {
  return (
    <div
      className={["rounded-2xl border p-4", getMetricClassName(tone)].join(" ")}
    >
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>

      <p className="mt-2 text-xs leading-5 text-slate-500">{description}</p>
    </div>
  );
}

export function CatalogImportRecognitionEnrichmentPanel({
  recognitionEnrichment,
}: CatalogImportRecognitionEnrichmentPanelProps) {
  if (recognitionEnrichment === null) {
    return null;
  }

  return (
    <section className="mt-6 rounded-2xl border border-green-500/25 bg-green-500/[0.04] p-5">
      <h3 className="text-lg font-semibold text-white">
        Безопасное автозаполнение пустых значений
      </h3>

      <p className="mt-2 max-w-5xl text-sm leading-6 text-slate-400">
        Этот блок показывает уже не предположения Shadow Mode, а результат
        RecognitionEnrichment. Значение считается записанным только после
        проверки источника, уверенности, конфликтов и допустимости значения для
        характеристики.
      </p>

      <div className="mt-4 rounded-xl border border-green-500/30 bg-green-500/[0.08] p-4">
        <p className="font-medium text-green-100">
          Явные значения Excel не перезаписываются
        </p>

        <p className="mt-2 text-sm leading-6 text-green-200/80">
          RecognitionEnrichment работает только с пустыми значениями. Если Excel
          уже содержит значение, оно остаётся источником истины. Если найден
          конфликт или недостаточная уверенность, автоматическое заполнение
          запрещается.
        </p>
      </div>

      <div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <EnrichmentMetric
          label="Проверено строк"
          value={recognitionEnrichment.rowsAnalyzedCount}
          description="Строки с наименованием, переданные Recognition Engine."
          tone="Neutral"
        />

        <EnrichmentMetric
          label="Дополнено строк"
          value={recognitionEnrichment.filledRowsCount}
          description="Строки, в которые записано хотя бы одно отсутствовавшее значение."
          tone={
            recognitionEnrichment.filledRowsCount > 0 ? "Success" : "Neutral"
          }
        />

        <EnrichmentMetric
          label="Записано значений"
          value={recognitionEnrichment.filledValuesCount}
          description="Количество характеристик, безопасно заполненных из наименования."
          tone={
            recognitionEnrichment.filledValuesCount > 0 ? "Success" : "Neutral"
          }
        />

        <EnrichmentMetric
          label="Заблокировано конфликтом"
          value={recognitionEnrichment.blockedByRecognitionConflictCount}
          description="Recognition Engine не смог выбрать единственное значение."
          tone={
            recognitionEnrichment.blockedByRecognitionConflictCount > 0
              ? "Danger"
              : "Success"
          }
        />

        <EnrichmentMetric
          label="Недостаточная уверенность"
          value={recognitionEnrichment.blockedByLowConfidenceCount}
          description="Confidence оказался ниже порога автоматического заполнения."
          tone={
            recognitionEnrichment.blockedByLowConfidenceCount > 0
              ? "Warning"
              : "Success"
          }
        />

        <EnrichmentMetric
          label="Источник запрещён"
          value={recognitionEnrichment.blockedByUnsupportedSourceCount}
          description="Кандидат найден эвристикой или другим источником, которому пока нельзя автоматически изменять импорт."
          tone={
            recognitionEnrichment.blockedByUnsupportedSourceCount > 0
              ? "Warning"
              : "Success"
          }
        />

        <EnrichmentMetric
          label="Некорректное значение Excel"
          value={recognitionEnrichment.blockedByInvalidExcelValueCount}
          description="Excel содержал явное, но недопустимое значение. Движок не стал молча заменять его."
          tone={
            recognitionEnrichment.blockedByInvalidExcelValueCount > 0
              ? "Danger"
              : "Success"
          }
        />

        <EnrichmentMetric
          label="Недопустимый результат Recognition"
          value={recognitionEnrichment.blockedByInvalidRecognizedValueCount}
          description="Найденное значение не прошло нормализацию характеристики."
          tone={
            recognitionEnrichment.blockedByInvalidRecognizedValueCount > 0
              ? "Danger"
              : "Success"
          }
        />

        <EnrichmentMetric
          label="Ошибок распознавания"
          value={recognitionEnrichment.failedRecognitionRowsCount}
          description="Строки, для которых распознавание не удалось завершить."
          tone={
            recognitionEnrichment.failedRecognitionRowsCount > 0
              ? "Danger"
              : "Success"
          }
        />
      </div>

      {recognitionEnrichment.appliedValuesDetailsTruncated && (
        <div className="mt-4 rounded-xl border border-amber-500/30 bg-amber-500/[0.08] p-4 text-sm leading-6 text-amber-100">
          Общее количество записанных значений является точным, но подробный
          список ограничен первыми 500 значениями, чтобы HTTP-ответ не
          становился слишком большим.
        </div>
      )}
    </section>
  );
}
