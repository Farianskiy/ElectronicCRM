"use client";

import { CatalogImportDiagnosticList } from "../CatalogImportDiagnosticList";
import type {
  CatalogImportManufacturerRecognitionShadow,
  CatalogImportManufacturerRecognitionShadowCandidate,
  CatalogImportManufacturerRecognitionShadowSample,
  CatalogImportManufacturerRecognitionShadowSampleKind,
  CatalogImportManufacturerResolutionSource,
} from "../../model/types";

interface CatalogImportManufacturerRecognitionShadowPanelProps {
  shadow: CatalogImportManufacturerRecognitionShadow | null;
}

interface SummaryCardProps {
  label: string;
  value: number;
  description: string;
  tone?: "Neutral" | "Success" | "Danger" | "Info" | "Warning";
}

interface SamplePresentation {
  label: string;
  cardClassName: string;
  badgeClassName: string;
  valueClassName: string;
}

const sampleOrder: Record<
  CatalogImportManufacturerRecognitionShadowSampleKind,
  number
> = {
  Conflict: 1,
  NameConflict: 2,
  ComparisonUnavailable: 3,
  Suggestion: 4,
  Match: 5,
  None: 6,
};

function getSummaryCardClassName(tone: SummaryCardProps["tone"]): string {
  switch (tone) {
    case "Success":
      return "border-green-500/25 bg-green-500/[0.06]";

    case "Danger":
      return "border-red-500/25 bg-red-500/[0.06]";

    case "Info":
      return "border-sky-500/25 bg-sky-500/[0.06]";

    case "Warning":
      return "border-amber-500/25 bg-amber-500/[0.06]";

    case "Neutral":
    default:
      return "border-white/10 bg-black/20";
  }
}

function getSamplePresentation(
  kind: CatalogImportManufacturerRecognitionShadowSampleKind,
): SamplePresentation {
  switch (kind) {
    case "Match":
      return {
        label: "Совпадение",
        cardClassName: "border-green-500/25 bg-green-500/[0.04]",
        badgeClassName: "border-green-500/30 bg-green-500/10 text-green-200",
        valueClassName: "text-green-300",
      };

    case "Conflict":
      return {
        label: "Конфликт",
        cardClassName: "border-red-500/30 bg-red-500/[0.05]",
        badgeClassName: "border-red-500/30 bg-red-500/10 text-red-200",
        valueClassName: "text-red-300",
      };

    case "Suggestion":
      return {
        label: "Предложение",
        cardClassName: "border-sky-500/30 bg-sky-500/[0.05]",
        badgeClassName: "border-sky-500/30 bg-sky-500/10 text-sky-200",
        valueClassName: "text-sky-300",
      };

    case "NameConflict":
      return {
        label: "Несколько производителей в наименовании",
        cardClassName: "border-amber-500/30 bg-amber-500/[0.05]",
        badgeClassName: "border-amber-500/30 bg-amber-500/10 text-amber-200",
        valueClassName: "text-amber-300",
      };

    case "ComparisonUnavailable":
      return {
        label: "Сравнение невозможно",
        cardClassName: "border-violet-500/30 bg-violet-500/[0.05]",
        badgeClassName: "border-violet-500/30 bg-violet-500/10 text-violet-200",
        valueClassName: "text-violet-300",
      };

    case "None":
    default:
      return {
        label: "Без решения",
        cardClassName: "border-white/10 bg-white/[0.02]",
        badgeClassName: "border-white/10 bg-white/[0.04] text-slate-300",
        valueClassName: "text-slate-300",
      };
  }
}

function getSourceLabel(
  source?: CatalogImportManufacturerResolutionSource | null,
): string {
  switch (source) {
    case "ExactName":
      return "Точное каноническое имя";

    case "ApprovedAlias":
      return "Подтверждённый alias";

    case "IgnoredNoise":
      return "Подтверждённый шум";

    case "None":
      return "Источник отсутствует";

    default:
      return "Не определён";
  }
}

function formatConfidence(confidence?: number | null): string {
  if (confidence === null || confidence === undefined) {
    return "—";
  }

  if (!Number.isFinite(confidence)) {
    return "—";
  }

  return `${Math.round(confidence * 100)}%`;
}

function SummaryCard({
  label,
  value,
  description,
  tone = "Neutral",
}: SummaryCardProps) {
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

function ProductNameEvidence({
  productName,
  spanStart,
  spanLength,
}: {
  productName: string;
  spanStart?: number | null;
  spanLength?: number | null;
}) {
  if (
    spanStart === null ||
    spanStart === undefined ||
    spanLength === null ||
    spanLength === undefined ||
    spanStart < 0 ||
    spanLength <= 0 ||
    spanStart + spanLength > productName.length
  ) {
    return (
      <p className="break-words font-mono text-sm leading-6 text-slate-200">
        {productName}
      </p>
    );
  }

  const spanEnd = spanStart + spanLength;
  const before = productName.slice(0, spanStart);
  const recognized = productName.slice(spanStart, spanEnd);
  const after = productName.slice(spanEnd);

  return (
    <p className="break-words font-mono text-sm leading-7 text-slate-300">
      {before}

      <mark className="rounded bg-cyan-400/20 px-1 py-0.5 text-cyan-100">
        {recognized}
      </mark>

      {after}
    </p>
  );
}

function CandidateCard({
  candidate,
}: {
  candidate: CatalogImportManufacturerRecognitionShadowCandidate;
}) {
  return (
    <div className="rounded-xl border border-white/10 bg-black/20 p-4">
      <div className="flex flex-wrap items-center gap-2">
        <p className="font-medium text-white">{candidate.manufacturerName}</p>

        <span className="rounded-full border border-cyan-500/25 bg-cyan-500/10 px-2 py-1 text-xs text-cyan-200">
          {getSourceLabel(candidate.source)}
        </span>

        <span className="rounded-full border border-white/10 bg-white/[0.04] px-2 py-1 text-xs text-slate-300">
          {formatConfidence(candidate.confidence)}
        </span>
      </div>

      <dl className="mt-3 grid gap-3 sm:grid-cols-3">
        <div>
          <dt className="text-xs text-slate-500">Найденный текст</dt>

          <dd className="mt-1 font-mono text-sm text-cyan-200">
            {candidate.rawValue}
          </dd>
        </div>

        <div>
          <dt className="text-xs text-slate-500">Начало span</dt>

          <dd className="mt-1 font-mono text-sm text-slate-200">
            {candidate.spanStart}
          </dd>
        </div>

        <div>
          <dt className="text-xs text-slate-500">Длина span</dt>

          <dd className="mt-1 font-mono text-sm text-slate-200">
            {candidate.spanLength}
          </dd>
        </div>
      </dl>
    </div>
  );
}

function ManufacturerShadowSampleCard({
  sample,
}: {
  sample: CatalogImportManufacturerRecognitionShadowSample;
}) {
  const presentation = getSamplePresentation(sample.kind);

  return (
    <article
      className={["rounded-2xl border p-5", presentation.cardClassName].join(
        " ",
      )}
    >
      <div className="flex flex-col justify-between gap-4 lg:flex-row lg:items-start">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={[
                "rounded-full border px-3 py-1 text-xs font-medium",
                presentation.badgeClassName,
              ].join(" ")}
            >
              {presentation.label}
            </span>

            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 font-mono text-xs text-slate-300">
              Строка Excel: {sample.rowNumber}
            </span>
          </div>

          <p className="mt-4 max-w-5xl text-sm leading-6 text-slate-300">
            {sample.details}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 px-4 py-3">
          <p className="text-xs text-slate-500">Уверенность</p>

          <p className="mt-1 text-xl font-semibold text-white">
            {formatConfidence(sample.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-5 rounded-xl border border-white/10 bg-black/20 p-4">
        <p className="mb-2 text-xs text-slate-500">
          Наименование с выделенным evidence
        </p>

        <ProductNameEvidence
          productName={sample.productName}
          spanStart={sample.spanStart}
          spanLength={sample.spanLength}
        />
      </div>

      <dl className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Manufacturer из Excel</dt>

          <dd className="mt-2 break-words font-mono text-sm text-white">
            {sample.excelManufacturerName ?? "Не заполнен"}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Найдено в наименовании</dt>

          <dd
            className={[
              "mt-2 break-words font-mono text-sm font-medium",
              presentation.valueClassName,
            ].join(" ")}
          >
            {sample.recognizedManufacturerName ??
              (sample.candidates.length > 0
                ? "Найдено несколько вариантов"
                : "Не найдено")}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Источник распознавания</dt>

          <dd className="mt-2 text-sm text-slate-200">
            {getSourceLabel(sample.recognitionSource)}
          </dd>

          {sample.recognitionSource && (
            <p className="mt-1 font-mono text-xs text-slate-500">
              {sample.recognitionSource}
            </p>
          )}
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Span</dt>

          <dd className="mt-2 font-mono text-sm text-slate-200">
            {sample.spanStart !== null &&
            sample.spanStart !== undefined &&
            sample.spanLength !== null &&
            sample.spanLength !== undefined
              ? `start=${sample.spanStart}, length=${sample.spanLength}`
              : "Единый span отсутствует"}
          </dd>
        </div>
      </dl>

      <dl className="mt-4 grid gap-4 lg:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Источник значения Excel</dt>

          <dd className="mt-2 text-sm text-slate-200">
            {sample.excelManufacturerResolutionSource ?? "Не разрешён"}
          </dd>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-4">
          <dt className="text-xs text-slate-500">Фрагмент из наименования</dt>

          <dd className="mt-2 break-words font-mono text-sm text-cyan-200">
            {sample.rawRecognizedValue ?? "Единый фрагмент отсутствует"}
          </dd>
        </div>
      </dl>

      {sample.candidates.length > 0 && (
        <div className="mt-5">
          <h4 className="font-medium text-white">
            Evidence-кандидаты производителя
          </h4>

          <p className="mt-2 text-sm leading-6 text-slate-400">
            Все кандидаты получены из общего справочника Manufacturer и
            подтверждённых ManufacturerAlias.
          </p>

          <div className="mt-3 grid gap-3 xl:grid-cols-2">
            {sample.candidates.map((candidate, index) => (
              <CandidateCard
                key={[
                  candidate.manufacturerId,
                  candidate.spanStart,
                  candidate.spanLength,
                  index,
                ].join("-")}
                candidate={candidate}
              />
            ))}
          </div>
        </div>
      )}
    </article>
  );
}

export function CatalogImportManufacturerRecognitionShadowPanel({
  shadow,
}: CatalogImportManufacturerRecognitionShadowPanelProps) {
  if (shadow === null) {
    return (
      <section className="rounded-3xl border border-cyan-500/20 bg-cyan-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Производитель по наименованию
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          После анализа здесь появится сравнение Manufacturer из Excel с
          производителем, найденным внутри полного наименования товара.
        </p>
      </section>
    );
  }

  const orderedSamples = [...shadow.samples].sort((left, right) => {
    const kindDifference = sampleOrder[left.kind] - sampleOrder[right.kind];

    if (kindDifference !== 0) {
      return kindDifference;
    }

    return left.rowNumber - right.rowNumber;
  });

  return (
    <section className="rounded-3xl border border-cyan-500/25 bg-cyan-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Производитель по наименованию
        </h2>

        <p className="mt-2 max-w-5xl text-sm leading-6 text-slate-400">
          Один ManufacturerResolutionIndex параллельно проверяет отдельную
          колонку Manufacturer и полное наименование товара. Так импорт и
          Ассистент используют один справочник канонических производителей и
          Approved-алиасов.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-sky-500/30 bg-sky-500/[0.07] p-5">
        <p className="font-medium text-sky-100">
          Это только наблюдение — данные импорта не изменяются
        </p>

        <p className="mt-2 text-sm leading-6 text-sky-200/80">
          При совпадении показывается подтверждение. При конфликте значение
          Excel остаётся авторитетным. Если Excel пуст, найденный производитель
          показывается только как предложение и не записывается в
          ManufacturerId.
        </p>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-3">
        <SummaryCard
          label="Совпадений"
          value={shadow.matchesCount}
          description="Excel и наименование указывают на одного канонического производителя."
          tone="Success"
        />

        <SummaryCard
          label="Конфликтов"
          value={shadow.conflictsCount}
          description="Excel и наименование указывают на разных производителей."
          tone={shadow.conflictsCount > 0 ? "Danger" : "Success"}
        />

        <SummaryCard
          label="Предложений"
          value={shadow.suggestionsCount}
          description="Excel пуст, но производитель найден внутри наименования."
          tone="Info"
        />
      </div>

      <div className="mt-4 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <SummaryCard
          label="Проанализировано строк"
          value={shadow.rowsAnalyzedCount}
          description="Строки, в которых удалось прочитать непустое наименование."
        />

        <SummaryCard
          label="Manufacturer заполнен"
          value={shadow.rowsWithExcelManufacturerValueCount}
          description="Строки с явным значением производителя в Excel."
        />

        <SummaryCard
          label="Найдено в наименовании"
          value={shadow.rowsWithRecognizedManufacturerCount}
          description="Строки с одним уверенно распознанным производителем."
          tone="Success"
        />

        <SummaryCard
          label="Конфликтов внутри Name"
          value={shadow.nameConflictsCount}
          description="В одном наименовании найдены разные производители."
          tone={shadow.nameConflictsCount > 0 ? "Danger" : "Success"}
        />

        <SummaryCard
          label="Сравнение невозможно"
          value={shadow.comparisonUnavailableCount}
          description="Excel заполнен, но его значение ещё не разрешено в ManufacturerId."
          tone={shadow.comparisonUnavailableCount > 0 ? "Warning" : "Success"}
        />
      </div>

      <div className="mt-8">
        <div className="flex flex-col justify-between gap-3 lg:flex-row lg:items-end">
          <div>
            <h3 className="text-lg font-semibold text-white">
              Диагностические решения
            </h3>

            <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
              Сначала показаны настоящие конфликты, затем неоднозначные случаи,
              предложения и подтверждённые совпадения.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-300">
              Не найдено в Name: {shadow.nameUnresolvedCount}
            </span>

            <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-300">
              Подробных примеров: {shadow.samples.length}
            </span>
          </div>
        </div>

        {shadow.samplesTruncated && (
          <div className="mt-4 rounded-2xl border border-amber-500/25 bg-amber-500/[0.06] p-4 text-sm leading-6 text-amber-100">
            Backend ограничил подробный список первыми 100 приоритетными
            примерами. Общие счётчики при этом рассчитаны по всем строкам.
          </div>
        )}

        {orderedSamples.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5">
            <p className="font-medium text-slate-200">
              Диагностические решения отсутствуют
            </p>

            <p className="mt-2 text-sm leading-6 text-slate-400">
              Производитель не был распознан либо в анализе пока нет строк с
              наименованием.
            </p>
          </div>
        ) : (
          <CatalogImportDiagnosticList
            items={orderedSamples}
            ariaLabel="Примеры распознавания производителей"
            renderItem={(sample) => (
              <ManufacturerShadowSampleCard
                key={`${sample.rowNumber}-${sample.kind}`}
                sample={sample}
              />
            )}
          />
        )}
      </div>
    </section>
  );
}
