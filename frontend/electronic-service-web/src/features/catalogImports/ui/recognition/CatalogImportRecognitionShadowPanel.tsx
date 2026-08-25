import type {
  CatalogImportRecognitionAppliedValue,
  CatalogImportRecognitionEnrichment,
  CatalogImportRecognitionShadow,
  CatalogImportRecognitionShadowSample,
  CatalogImportRecognitionShadowSampleKind,
} from "../../model/types";

import { CatalogImportRecognitionConflictSummary } from "./CatalogImportRecognitionConflictSummary";
import { CatalogImportRecognitionEnrichmentPanel } from "./CatalogImportRecognitionEnrichmentPanel";

interface CatalogImportRecognitionShadowPanelProps {
  recognitionShadow: CatalogImportRecognitionShadow | null;
  recognitionEnrichment: CatalogImportRecognitionEnrichment | null;
}

interface ShadowSummaryCardProps {
  label: string;
  value: number;
  description: string;
  tone?: "Neutral" | "Success" | "Warning" | "Danger";
}

interface SampleKindPresentation {
  label: string;
  className: string;
}

interface ImportResultPresentation {
  value: string;
  description: string;
  className: string;
}

function formatConfidence(confidence?: number | null): string {
  if (confidence === null || confidence === undefined) {
    return "—";
  }

  if (!Number.isFinite(confidence)) {
    return "—";
  }

  return `${(confidence * 100).toFixed(1)}%`;
}

function formatSpan(
  spanStart?: number | null,
  spanLength?: number | null,
): string {
  if (
    spanStart === null ||
    spanStart === undefined ||
    spanLength === null ||
    spanLength === undefined
  ) {
    return "—";
  }

  return `${spanStart}..${spanStart + spanLength}`;
}

function getAppliedValueKey(
  rowNumber: number,
  characteristicCode: string,
): string {
  return `${rowNumber}:${characteristicCode.trim().toUpperCase()}`;
}

function getSampleKindPresentation(
  kind: CatalogImportRecognitionShadowSampleKind,
): SampleKindPresentation {
  switch (kind) {
    case "Conflict":
      return {
        label: "Конфликт Excel и наименования",
        className: "border-red-500/30 bg-red-500/10 text-red-200",
      };

    case "NotRecognized":
      return {
        label: "Не подтверждено наименованием",
        className: "border-slate-500/30 bg-slate-500/10 text-slate-200",
      };

    case "RecognitionWithoutExplicitValue":
      return {
        label: "Можно заполнить из наименования",
        className: "border-sky-500/30 bg-sky-500/10 text-sky-200",
      };

    case "Ambiguous":
      return {
        label: "Неоднозначное распознавание",
        className: "border-fuchsia-500/30 bg-fuchsia-500/10 text-fuchsia-200",
      };

    case "None":
    default:
      return {
        label: "Диагностический пример",
        className: "border-slate-500/30 bg-slate-500/10 text-slate-200",
      };
  }
}

function getImportResultPresentation(
  sample: CatalogImportRecognitionShadowSample,
  appliedValue: CatalogImportRecognitionAppliedValue | null,
  appliedValuesDetailsTruncated: boolean,
): ImportResultPresentation {
  switch (sample.kind) {
    case "Conflict":
      return {
        value: sample.excelValue ?? "Требуется решение",
        description:
          "В текущем импорте приоритет остаётся у явного значения Excel. Красный статус показывает, что наименование предлагает другое значение и конфликт требует внимания.",
        className: "border-red-500/30 bg-red-500/[0.08]",
      };

    case "NotRecognized":
      return {
        value: sample.excelValue ?? "Нет значения",
        description:
          "Явное значение Excel будет использовано при импорте. Отсутствие подтверждения в наименовании не отменяет и не изменяет это значение.",
        className: "border-green-500/30 bg-green-500/[0.08]",
      };

    case "RecognitionWithoutExplicitValue":
      if (appliedValue !== null) {
        return {
          value: appliedValue.value,
          description:
            "Ячейка Excel была пустой. Кандидат прошёл проверки безопасности RecognitionEnrichment и уже записан в нормализованную строку импорта.",
          className: "border-green-500/30 bg-green-500/[0.08]",
        };
      }

      if (appliedValuesDetailsTruncated) {
        return {
          value: "Проверьте общую сводку автозаполнения",
          description:
            "Кандидат найден, но подробный список применённых значений был ограничен первыми 500 записями. По этой карточке нельзя достоверно определить факт записи.",
          className: "border-amber-500/30 bg-amber-500/[0.08]",
        };
      }

      return {
        value: "Автозаполнение не выполнено",
        description:
          "Кандидат найден, но не прошёл одну из проверок безопасности. Причину можно определить по сводке RecognitionEnrichment: конфликт, confidence, источник или недопустимое значение.",
        className: "border-amber-500/30 bg-amber-500/[0.08]",
      };

    case "Ambiguous":
      if (sample.excelValue) {
        return {
          value: sample.excelValue,
          description:
            "Recognition Engine нашёл несколько вариантов, но явное значение Excel остаётся итоговым значением импорта.",
          className: "border-fuchsia-500/30 bg-fuchsia-500/[0.08]",
        };
      }

      return {
        value: "Автоматически не заполняется",
        description:
          "Явного значения Excel нет, а Recognition Engine нашёл несколько вариантов. Для безопасного импорта требуется решение пользователя.",
        className: "border-fuchsia-500/30 bg-fuchsia-500/[0.08]",
      };

    case "None":
    default:
      return {
        value: sample.excelValue ?? sample.recognizedValue ?? "Нет значения",
        description:
          "Итоговое значение определяется доступными источниками данных.",
        className: "border-white/10 bg-white/[0.03]",
      };
  }
}

function getDiagnosticExplanation(
  sample: CatalogImportRecognitionShadowSample,
): string | null {
  if (sample.kind === "NotRecognized") {
    return "Явное значение Excel уже прочитано, нормализовано и остаётся итоговым значением импорта. В диагностику строка попала только потому, что внутри наименования не найдено независимое текстовое подтверждение.";
  }

  return sample.details ?? null;
}

function getSummaryCardClassName(tone: ShadowSummaryCardProps["tone"]): string {
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

function ShadowSummaryCard({
  label,
  value,
  description,
  tone = "Neutral",
}: ShadowSummaryCardProps) {
  return (
    <div
      className={["rounded-2xl border p-4", getSummaryCardClassName(tone)].join(
        " ",
      )}
    >
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>

      <p className="mt-2 text-xs leading-5 text-slate-500">{description}</p>
    </div>
  );
}

function ShadowSampleCard({
  sample,
  appliedValue,
  appliedValuesDetailsTruncated,
}: {
  sample: CatalogImportRecognitionShadowSample;
  appliedValue: CatalogImportRecognitionAppliedValue | null;
  appliedValuesDetailsTruncated: boolean;
}) {
  const presentation = getSampleKindPresentation(sample.kind);
  const importResult = getImportResultPresentation(
    sample,
    appliedValue,
    appliedValuesDetailsTruncated,
  );
  const diagnosticExplanation = getDiagnosticExplanation(sample);

  const hasRecognitionEvidence =
    sample.recognizedValue !== null && sample.recognizedValue !== undefined;

  const nameEvidenceClassName =
    sample.kind === "Conflict"
      ? "border-red-500/30 bg-red-500/[0.06]"
      : "border-sky-500/20 bg-sky-500/[0.05]";

  return (
    <article className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={[
                "rounded-full border px-3 py-1 text-xs font-medium",
                presentation.className,
              ].join(" ")}
            >
              {presentation.label}
            </span>

            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-400">
              Строка Excel: {sample.rowNumber}
            </span>
          </div>

          <p className="mt-4 text-sm font-medium text-white">
            {sample.productName}
          </p>
        </div>

        <div className="text-left lg:text-right">
          <p className="text-xs text-slate-500">
            Уверенность распознавания из наименования
          </p>

          <p className="mt-1 font-mono text-sm text-slate-200">
            {formatConfidence(sample.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-5 rounded-xl border border-white/10 bg-white/[0.03] p-4">
        <p className="text-xs text-slate-500">Характеристика</p>

        <p className="mt-2 text-sm font-medium text-white">
          {sample.characteristicName}
        </p>

        <p className="mt-1 break-all font-mono text-xs text-teal-300">
          {sample.characteristicCode}
        </p>
      </div>

      <div className="mt-4 grid gap-4 xl:grid-cols-3">
        <section className="rounded-xl border border-green-500/20 bg-green-500/[0.05] p-4">
          <p className="text-xs font-medium uppercase tracking-wide text-green-300">
            1. Явное значение Excel
          </p>

          <p className="mt-3 break-words font-mono text-lg font-semibold text-white">
            {sample.excelValue ?? "Ячейка Excel пустая"}
          </p>

          <p className="mt-3 text-sm leading-6 text-green-100/70">
            Значение получено из отдельной сопоставленной колонки Excel и имеет
            приоритет при импорте.
          </p>
        </section>

        <section
          className={["rounded-xl border p-4", nameEvidenceClassName].join(" ")}
        >
          <p className="text-xs font-medium uppercase tracking-wide text-sky-300">
            2. Подтверждение из наименования
          </p>

          <p className="mt-3 break-words font-mono text-lg font-semibold text-white">
            {sample.recognizedValue ?? "Подтверждение не найдено"}
          </p>

          <dl className="mt-4 grid gap-3 text-sm">
            <div>
              <dt className="text-xs text-slate-500">Найденный фрагмент</dt>

              <dd className="mt-1 break-words font-mono text-slate-200">
                {sample.rawRecognizedValue ?? "—"}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-slate-500">Источник распознавания</dt>

              <dd className="mt-1 break-words font-mono text-slate-200">
                {sample.recognitionSource ?? "—"}
              </dd>
            </div>
          </dl>
        </section>

        <section
          className={["rounded-xl border p-4", importResult.className].join(
            " ",
          )}
        >
          <p className="text-xs font-medium uppercase tracking-wide text-slate-300">
            3. Итог импорта
          </p>

          <p className="mt-3 break-words font-mono text-lg font-semibold text-white">
            {importResult.value}
          </p>

          <p className="mt-3 text-sm leading-6 text-slate-300">
            {importResult.description}
          </p>
        </section>
      </div>

      {sample.kind === "RecognitionWithoutExplicitValue" && (
        <section className="mt-4 rounded-xl border border-sky-500/20 bg-sky-500/[0.05] p-4">
          <h4 className="font-medium text-white">
            Решение RecognitionEnrichment
          </h4>

          <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-xl border border-sky-500/20 bg-black/20 p-3">
              <p className="text-xs text-slate-500">Кандидат найден</p>

              <p className="mt-2 font-medium text-sky-200">Да</p>
            </div>

            <div className="rounded-xl border border-white/10 bg-black/20 p-3">
              <p className="text-xs text-slate-500">Автозаполнение разрешено</p>

              <p
                className={[
                  "mt-2 font-medium",
                  appliedValue !== null ? "text-green-300" : "text-amber-300",
                ].join(" ")}
              >
                {appliedValue !== null
                  ? "Да"
                  : appliedValuesDetailsTruncated
                    ? "Нет данных в сокращённой диагностике"
                    : "Нет"}
              </p>
            </div>

            <div className="rounded-xl border border-white/10 bg-black/20 p-3">
              <p className="text-xs text-slate-500">
                Значение записано в строку импорта
              </p>

              <p
                className={[
                  "mt-2 font-medium",
                  appliedValue !== null ? "text-green-300" : "text-amber-300",
                ].join(" ")}
              >
                {appliedValue !== null
                  ? "Да"
                  : appliedValuesDetailsTruncated
                    ? "Требуется проверка сводки"
                    : "Нет"}
              </p>
            </div>

            <div className="rounded-xl border border-white/10 bg-black/20 p-3">
              <p className="text-xs text-slate-500">
                Источник итогового значения
              </p>

              <p className="mt-2 break-words font-mono text-sm text-slate-200">
                {appliedValue !== null
                  ? `Recognition / ${appliedValue.recognitionSource}`
                  : sample.recognitionSource
                    ? `Кандидат / ${sample.recognitionSource}`
                    : "—"}
              </p>
            </div>
          </div>
        </section>
      )}

      {hasRecognitionEvidence && (
        <details className="mt-4 rounded-xl border border-white/10 bg-white/[0.02]">
          <summary className="cursor-pointer px-4 py-3 text-sm font-medium text-slate-300">
            Техническое доказательство распознавания
          </summary>

          <dl className="grid gap-4 border-t border-white/10 p-4 md:grid-cols-2 xl:grid-cols-4">
            <div>
              <dt className="text-xs text-slate-500">Confidence</dt>

              <dd className="mt-1 font-mono text-sm text-slate-200">
                {formatConfidence(sample.confidence)}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-slate-500">Span</dt>

              <dd className="mt-1 font-mono text-sm text-slate-200">
                {formatSpan(sample.spanStart, sample.spanLength)}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-slate-500">Priority</dt>

              <dd className="mt-1 font-mono text-sm text-slate-200">
                {sample.priority ?? "—"}
              </dd>
            </div>

            <div>
              <dt className="text-xs text-slate-500">RecognizerKey</dt>

              <dd className="mt-1 break-all font-mono text-xs text-slate-200">
                {sample.recognizerKey ?? "—"}
              </dd>
            </div>
          </dl>
        </details>
      )}

      {diagnosticExplanation && (
        <div className="mt-4 rounded-xl border border-blue-500/20 bg-blue-500/[0.05] p-4">
          <p className="text-xs text-blue-300">
            Почему пример показан в диагностике
          </p>

          <p className="mt-2 text-sm leading-6 text-slate-300">
            {diagnosticExplanation}
          </p>
        </div>
      )}
    </article>
  );
}

export function CatalogImportRecognitionShadowPanel({
  recognitionShadow,
  recognitionEnrichment,
}: CatalogImportRecognitionShadowPanelProps) {
  if (recognitionShadow === null) {
    return (
      <section className="rounded-3xl border border-sky-500/20 bg-sky-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Shadow Mode распознавания
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          Выберите тип товара и нажмите «Повторить автораспознавание» либо
          сохраните сопоставление колонок. После анализа здесь появится
          сравнение явных значений Excel со значениями, найденными в
          наименованиях товаров.
        </p>

        <div className="mt-5 rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-4">
          <p className="text-sm font-medium text-green-100">
            Этот режим не изменяет характеристики товаров
          </p>

          <p className="mt-2 text-sm leading-6 text-green-200/80">
            Recognition Engine работает параллельно существующему импорту. Он
            только измеряет качество распознавания и показывает будущие
            возможности автоматического заполнения.
          </p>
        </div>
      </section>
    );
  }

  const appliedValuesByKey = new Map(
    (recognitionEnrichment?.appliedValues ?? []).map(
      (appliedValue) =>
        [
          getAppliedValueKey(
            appliedValue.rowNumber,
            appliedValue.characteristicCode,
          ),
          appliedValue,
        ] as const,
    ),
  );

  return (
    <section className="rounded-3xl border border-sky-500/25 bg-sky-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Shadow Mode распознавания
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          Сначала Shadow Mode сравнивает явные значения Excel с результатами
          Recognition Engine. Затем RecognitionEnrichment может безопасно
          заполнить только отсутствующие значения, прошедшие все проверки.
        </p>
      </div>

      <CatalogImportRecognitionEnrichmentPanel
        recognitionEnrichment={recognitionEnrichment}
      />

      <div className="mt-5 rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-5">
        <p className="text-sm font-medium text-green-100">
          Явные значения Excel являются итоговыми значениями импорта
        </p>

        <p className="mt-2 text-sm leading-6 text-green-200/80">
          Recognition Engine сейчас работает параллельно и не перезаписывает
          Excel. Если подтверждение в наименовании не найдено, корректное
          значение Excel всё равно будет использовано. Красным отмечается только
          настоящий конфликт, когда Excel и наименование предлагают разные
          значения.
        </p>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <ShadowSummaryCard
          label="Проанализировано строк"
          value={recognitionShadow.rowsAnalyzed}
          description="Строки, в которых присутствовало наименование товара."
        />

        <ShadowSummaryCard
          label="Строк с распознаванием"
          value={recognitionShadow.rowsWithRecognition}
          description="Строки, где движок нашёл хотя бы одну характеристику или неоднозначность."
        />

        <ShadowSummaryCard
          label="Совпадений"
          value={recognitionShadow.matchesCount}
          description="Значение Excel совпало со значением из наименования."
          tone="Success"
        />

        <ShadowSummaryCard
          label="Конфликтов"
          value={recognitionShadow.conflictsCount}
          description="Excel и наименование содержат разные значения."
          tone={recognitionShadow.conflictsCount > 0 ? "Danger" : "Success"}
        />

        <ShadowSummaryCard
          label="Не подтверждено наименованием"
          value={recognitionShadow.notRecognizedCount}
          description="Явное значение Excel есть и будет использовано. В наименовании независимое подтверждение не найдено."
          tone="Neutral"
        />

        <ShadowSummaryCard
          label="Можно заполнить"
          value={recognitionShadow.recognitionWithoutExplicitValueCount}
          description="Движок нашёл значение, хотя отдельная ячейка Excel была пустой."
          tone={
            recognitionShadow.recognitionWithoutExplicitValueCount > 0
              ? "Success"
              : "Neutral"
          }
        />

        <ShadowSummaryCard
          label="Неоднозначностей"
          value={recognitionShadow.ambiguousCount}
          description="Источники одного уровня предложили разные значения."
          tone={recognitionShadow.ambiguousCount > 0 ? "Danger" : "Success"}
        />

        <ShadowSummaryCard
          label="Ошибок запуска"
          value={recognitionShadow.failedRowsCount}
          description="Строки, на которых распознавание не удалось завершить."
          tone={recognitionShadow.failedRowsCount > 0 ? "Danger" : "Success"}
        />
      </div>

      <div className="mt-8">
        <h3 className="text-lg font-semibold text-white">
          Статистика по характеристикам
        </h3>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Здесь видно, какие характеристики уже хорошо распознаются, а для каких
          нужны дополнительные правила, словарные термины или профили.
        </p>

        <div className="mt-4 overflow-x-auto rounded-2xl border border-white/10">
          <table className="w-full min-w-[1100px] border-collapse text-left text-sm">
            <thead className="bg-black/30 text-slate-400">
              <tr>
                <th className="px-4 py-3 font-medium">Характеристика</th>
                <th className="px-4 py-3 font-medium">Явных значений Excel</th>
                <th className="px-4 py-3 font-medium">
                  {" "}
                  Найдено в наименовании
                </th>
                <th className="px-4 py-3 font-medium">Подтверждено</th>
                <th className="px-4 py-3 font-medium">Настоящий конфликт</th>
                <th className="px-4 py-3 font-medium">
                  Нет подтверждения в имени
                </th>
                <th className="px-4 py-3 font-medium">
                  Кандидат для заполнения
                </th>
                <th className="px-4 py-3 font-medium">Неоднозначно</th>
              </tr>
            </thead>

            <tbody className="divide-y divide-white/10">
              {recognitionShadow.characteristics.map((characteristic) => (
                <tr
                  key={characteristic.characteristicCode}
                  className="bg-white/[0.01]"
                >
                  <td className="px-4 py-4">
                    <p className="font-medium text-white">
                      {characteristic.characteristicName}
                    </p>

                    <p className="mt-1 font-mono text-xs text-teal-300">
                      {characteristic.characteristicCode}
                    </p>
                  </td>

                  <td className="px-4 py-4 text-slate-200">
                    {characteristic.explicitValuesCount}
                  </td>

                  <td className="px-4 py-4 text-slate-200">
                    {characteristic.recognizedValuesCount}
                  </td>

                  <td className="px-4 py-4 text-green-300">
                    {characteristic.matchesCount}
                  </td>

                  <td className="px-4 py-4 text-red-300">
                    {characteristic.conflictsCount}
                  </td>

                  <td className="px-4 py-4 text-slate-300">
                    {characteristic.notRecognizedCount}
                  </td>

                  <td className="px-4 py-4 text-sky-300">
                    {characteristic.recognitionWithoutExplicitValueCount}
                  </td>

                  <td className="px-4 py-4 text-fuchsia-300">
                    {characteristic.ambiguousCount}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="mt-8">
        <CatalogImportRecognitionConflictSummary
          conflictGroups={recognitionShadow.conflictGroups ?? []}
          totalConflictsCount={recognitionShadow.conflictsCount}
        />

        <h3 className="text-lg font-semibold text-white">
          Диагностические примеры
        </h3>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Backend возвращает не больше 100 примеров для анализа. Здесь могут
          находиться как настоящие конфликты, так и безопасные случаи, когда
          значение Excel используется без дополнительного подтверждения внутри
          наименования.
        </p>

        {recognitionShadow.samples.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-green-500/25 bg-green-500/[0.06] p-5">
            <p className="font-medium text-green-100">
              Дополнительных примеров для анализа нет
            </p>

            <p className="mt-2 text-sm leading-6 text-green-200/80">
              В текущем файле нет конфликтов, неоднозначностей, отсутствующего
              подтверждения в наименовании и потенциальных кандидатов для
              заполнения пустых ячеек.
            </p>
          </div>
        ) : (
          <div className="mt-4 grid gap-4">
            {recognitionShadow.samples.map((sample, index) => {
              const appliedValue =
                appliedValuesByKey.get(
                  getAppliedValueKey(
                    sample.rowNumber,
                    sample.characteristicCode,
                  ),
                ) ?? null;

              return (
                <ShadowSampleCard
                  key={[
                    sample.rowNumber,
                    sample.characteristicCode,
                    sample.kind,
                    index,
                  ].join("-")}
                  sample={sample}
                  appliedValue={appliedValue}
                  appliedValuesDetailsTruncated={
                    recognitionEnrichment?.appliedValuesDetailsTruncated ??
                    false
                  }
                />
              );
            })}
          </div>
        )}
      </div>
    </section>
  );
}
