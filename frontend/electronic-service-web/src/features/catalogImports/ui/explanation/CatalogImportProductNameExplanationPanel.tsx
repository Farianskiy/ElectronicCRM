"use client";

import { CatalogImportDiagnosticList } from "../CatalogImportDiagnosticList";
import type {
  CatalogImportProductNameEvidenceKind,
  CatalogImportProductNameEvidenceSpan,
  CatalogImportProductNameExplanation,
  CatalogImportProductNameExplanationSample,
  CatalogImportProductNameExplanationSampleKind,
} from "../../model/types";

interface CatalogImportProductNameExplanationPanelProps {
  explanation: CatalogImportProductNameExplanation | null;
}

interface SummaryCardProps {
  label: string;
  value: string | number;
  description: string;
  tone?: "Neutral" | "Success" | "Info" | "Warning";
}

interface SamplePresentation {
  label: string;
  description: string;
  cardClassName: string;
  badgeClassName: string;
  valueClassName: string;
}

type HighlightTone =
  | "Neutral"
  | "Manufacturer"
  | "ProductType"
  | "Characteristic"
  | "Mixed"
  | "Unexplained";

interface HighlightedSegment {
  text: string;
  tone: HighlightTone;
}

const sampleOrder: Record<
  CatalogImportProductNameExplanationSampleKind,
  number
> = {
  Unexplained: 1,
  PartiallyExplained: 2,
  FullyExplained: 3,
  None: 4,
};

function formatCoverage(coverage: number): string {
  if (!Number.isFinite(coverage)) {
    return "—";
  }

  return `${Math.round(coverage * 100)}%`;
}

function getSummaryCardClassName(tone: SummaryCardProps["tone"]): string {
  switch (tone) {
    case "Success":
      return "border-green-500/25 bg-green-500/[0.06]";

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
  kind: CatalogImportProductNameExplanationSampleKind,
): SamplePresentation {
  switch (kind) {
    case "FullyExplained":
      return {
        label: "Полностью объяснено",
        description:
          "Все значимые буквы и цифры наименования покрыты evidence.",
        cardClassName: "border-green-500/25 bg-green-500/[0.04]",
        badgeClassName: "border-green-500/30 bg-green-500/10 text-green-200",
        valueClassName: "text-green-300",
      };

    case "PartiallyExplained":
      return {
        label: "Объяснено частично",
        description:
          "Часть наименования распознана, но остались непонятные фрагменты.",
        cardClassName: "border-sky-500/25 bg-sky-500/[0.04]",
        badgeClassName: "border-sky-500/30 bg-sky-500/10 text-sky-200",
        valueClassName: "text-sky-300",
      };

    case "Unexplained":
      return {
        label: "Не объяснено",
        description:
          "Ни одна значимая часть наименования пока не покрыта evidence.",
        cardClassName: "border-amber-500/30 bg-amber-500/[0.05]",
        badgeClassName: "border-amber-500/30 bg-amber-500/10 text-amber-200",
        valueClassName: "text-amber-300",
      };

    case "None":
    default:
      return {
        label: "Без результата",
        description: "Для строки не сформирован результат объяснения.",
        cardClassName: "border-white/10 bg-white/[0.02]",
        badgeClassName: "border-white/10 bg-white/[0.04] text-slate-300",
        valueClassName: "text-slate-300",
      };
  }
}

function getEvidenceLabel(kind: CatalogImportProductNameEvidenceKind): string {
  switch (kind) {
    case "Manufacturer":
      return "Производитель";

    case "ProductType":
      return "Тип товара";

    case "Characteristic":
      return "Характеристика";

    case "None":
    default:
      return "Не определено";
  }
}

function getEvidenceClassName(
  kind: CatalogImportProductNameEvidenceKind,
): string {
  switch (kind) {
    case "Manufacturer":
      return "border-emerald-500/25 bg-emerald-500/[0.05]";

    case "ProductType":
      return "border-violet-500/25 bg-violet-500/[0.05]";

    case "Characteristic":
      return "border-sky-500/25 bg-sky-500/[0.05]";

    case "None":
    default:
      return "border-white/10 bg-black/20";
  }
}

function getEvidenceBadgeClassName(
  kind: CatalogImportProductNameEvidenceKind,
): string {
  switch (kind) {
    case "Manufacturer":
      return "border-emerald-500/30 bg-emerald-500/10 text-emerald-200";

    case "ProductType":
      return "border-violet-500/30 bg-violet-500/10 text-violet-200";

    case "Characteristic":
      return "border-sky-500/30 bg-sky-500/10 text-sky-200";

    case "None":
    default:
      return "border-white/10 bg-white/[0.04] text-slate-300";
  }
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

function determineHighlightTone(
  kinds: Set<CatalogImportProductNameEvidenceKind>,
  isUnexplained: boolean,
): HighlightTone {
  if (kinds.size > 1) {
    return "Mixed";
  }

  if (kinds.has("Manufacturer")) {
    return "Manufacturer";
  }

  if (kinds.has("ProductType")) {
    return "ProductType";
  }

  if (kinds.has("Characteristic")) {
    return "Characteristic";
  }

  if (isUnexplained) {
    return "Unexplained";
  }

  return "Neutral";
}

function buildHighlightedSegments(
  sample: CatalogImportProductNameExplanationSample,
): HighlightedSegment[] {
  if (sample.productName.length === 0) {
    return [];
  }

  const evidenceKindsByPosition = Array.from(
    { length: sample.productName.length },
    () => new Set<CatalogImportProductNameEvidenceKind>(),
  );

  const unexplainedPositions = new Array<boolean>(
    sample.productName.length,
  ).fill(false);

  for (const evidence of sample.evidence) {
    const startIndex = Math.max(0, evidence.startIndex);
    const endIndex = Math.min(sample.productName.length, evidence.endIndex);

    for (let index = startIndex; index < endIndex; index += 1) {
      evidenceKindsByPosition[index]?.add(evidence.kind);
    }
  }

  for (const span of sample.unexplainedSpans) {
    const startIndex = Math.max(0, span.startIndex);
    const endIndex = Math.min(sample.productName.length, span.endIndex);

    for (let index = startIndex; index < endIndex; index += 1) {
      unexplainedPositions[index] = true;
    }
  }

  const getTone = (index: number): HighlightTone => {
    const kinds =
      evidenceKindsByPosition[index] ??
      new Set<CatalogImportProductNameEvidenceKind>();

    return determineHighlightTone(kinds, unexplainedPositions[index] === true);
  };

  const segments: HighlightedSegment[] = [];

  let currentTone = getTone(0);
  let currentText = sample.productName.charAt(0);

  for (let index = 1; index < sample.productName.length; index += 1) {
    const tone = getTone(index);
    const character = sample.productName.charAt(index);

    if (tone === currentTone) {
      currentText += character;
      continue;
    }

    segments.push({
      text: currentText,
      tone: currentTone,
    });

    currentTone = tone;
    currentText = character;
  }

  segments.push({
    text: currentText,
    tone: currentTone,
  });

  return segments;
}

function getHighlightedSegmentClassName(tone: HighlightTone): string {
  switch (tone) {
    case "Manufacturer":
      return "rounded bg-emerald-400/25 px-1 text-emerald-100";

    case "ProductType":
      return "rounded bg-violet-400/25 px-1 text-violet-100";

    case "Characteristic":
      return "rounded bg-sky-400/25 px-1 text-sky-100";

    case "Mixed":
      return "rounded bg-fuchsia-400/25 px-1 text-fuchsia-100";

    case "Unexplained":
      return "rounded bg-amber-400/20 px-1 text-amber-100 underline decoration-amber-400/70 decoration-wavy underline-offset-4";

    case "Neutral":
    default:
      return "text-slate-300";
  }
}

function HighlightedProductName({
  sample,
}: {
  sample: CatalogImportProductNameExplanationSample;
}) {
  const segments = buildHighlightedSegments(sample);

  return (
    <div className="rounded-xl border border-white/10 bg-black/30 p-4 font-mono text-sm leading-8 text-slate-300">
      {segments.map((segment, index) =>
        segment.tone === "Neutral" ? (
          <span key={`${index}-${segment.tone}`}>{segment.text}</span>
        ) : (
          <mark
            key={`${index}-${segment.tone}`}
            className={getHighlightedSegmentClassName(segment.tone)}
          >
            {segment.text}
          </mark>
        ),
      )}
    </div>
  );
}

function EvidenceCard({
  evidence,
}: {
  evidence: CatalogImportProductNameEvidenceSpan;
}) {
  return (
    <div
      className={[
        "rounded-xl border p-4",
        getEvidenceClassName(evidence.kind),
      ].join(" ")}
    >
      <div className="flex flex-wrap items-center gap-2">
        <span
          className={[
            "rounded-full border px-2 py-1 text-xs",
            getEvidenceBadgeClassName(evidence.kind),
          ].join(" ")}
        >
          {getEvidenceLabel(evidence.kind)}
        </span>

        <span className="font-mono text-sm font-semibold text-white">
          {evidence.rawValue}
        </span>
      </div>

      <div className="mt-4 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
        <div>
          <p className="text-xs text-slate-500">Целевой код</p>
          <p className="mt-1 break-all font-mono text-slate-200">
            {evidence.targetCode}
          </p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Целевое значение</p>
          <p className="mt-1 text-slate-200">{evidence.targetValue}</p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Источник</p>
          <p className="mt-1 font-mono text-slate-200">{evidence.source}</p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Уверенность</p>
          <p className="mt-1 font-semibold text-slate-200">
            {formatCoverage(evidence.confidence)}
          </p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Приоритет</p>
          <p className="mt-1 font-mono text-slate-200">
            {evidence.priority > 0 ? evidence.priority : "—"}
          </p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Начало span</p>
          <p className="mt-1 font-mono text-slate-200">{evidence.startIndex}</p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Длина span</p>
          <p className="mt-1 font-mono text-slate-200">{evidence.length}</p>
        </div>

        <div>
          <p className="text-xs text-slate-500">Конец span</p>
          <p className="mt-1 font-mono text-slate-200">{evidence.endIndex}</p>
        </div>
      </div>
    </div>
  );
}

function SampleCard({
  sample,
}: {
  sample: CatalogImportProductNameExplanationSample;
}) {
  const presentation = getSamplePresentation(sample.kind);

  return (
    <article className={`rounded-2xl border p-5 ${presentation.cardClassName}`}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`rounded-full border px-3 py-1 text-xs font-medium ${presentation.badgeClassName}`}
          >
            {presentation.label}
          </span>

          <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
            Строка Excel: {sample.rowNumber}
          </span>
        </div>

        <div className="text-right">
          <p className="text-xs text-slate-500">Покрытие</p>
          <p
            className={`mt-1 text-xl font-semibold ${presentation.valueClassName}`}
          >
            {formatCoverage(sample.coverage)}
          </p>
        </div>
      </div>

      <p className="mt-4 text-sm leading-6 text-slate-300">
        {presentation.description}
      </p>

      <div className="mt-4">
        <HighlightedProductName sample={sample} />
      </div>

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        <span className="rounded-full border border-emerald-500/25 bg-emerald-500/10 px-3 py-1 text-emerald-200">
          Производитель: {sample.manufacturerEvidenceCount}
        </span>

        <span className="rounded-full border border-violet-500/25 bg-violet-500/10 px-3 py-1 text-violet-200">
          Тип товара: {sample.productTypeEvidenceCount}
        </span>

        <span className="rounded-full border border-sky-500/25 bg-sky-500/10 px-3 py-1 text-sky-200">
          Характеристики: {sample.characteristicEvidenceCount}
        </span>

        <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-slate-300">
          Покрыто символов: {sample.coveredMeaningfulCharactersCount}/
          {sample.meaningfulCharactersCount}
        </span>
      </div>

      {sample.unexplainedSpans.length > 0 && (
        <div className="mt-5">
          <p className="text-sm font-medium text-amber-100">Непонятные части</p>

          <p className="mt-1 text-xs leading-5 text-slate-500">
            Они могут быть серией, моделью, исполнением или пока неизвестным
            обозначением. Наличие такого фрагмента не означает автоматически,
            что строка ошибочна.
          </p>

          <div className="mt-3 flex flex-wrap gap-2">
            {sample.unexplainedSpans.map((span, index) => (
              <span
                key={`${span.startIndex}-${span.length}-${index}`}
                className="rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 font-mono text-xs text-amber-200"
              >
                {span.rawValue} · start={span.startIndex}, length={span.length}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="mt-5">
        <p className="text-sm font-medium text-slate-300">
          Evidence распознавания
        </p>

        {sample.evidence.length === 0 ? (
          <div className="mt-3 rounded-xl border border-white/10 bg-black/20 p-4 text-sm text-slate-400">
            Для строки не найдено ни одного evidence производителя, типа или
            характеристики.
          </div>
        ) : (
          <div className="mt-3 grid gap-3">
            {sample.evidence.map((evidence, index) => (
              <EvidenceCard
                key={`${evidence.kind}-${evidence.targetCode}-${evidence.startIndex}-${evidence.length}-${index}`}
                evidence={evidence}
              />
            ))}
          </div>
        )}
      </div>
    </article>
  );
}

export function CatalogImportProductNameExplanationPanel({
  explanation,
}: CatalogImportProductNameExplanationPanelProps) {
  if (explanation === null) {
    return (
      <section className="rounded-3xl border border-fuchsia-500/20 bg-fuchsia-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Объяснение наименований
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          После анализа здесь появится единое объяснение производителя, типа,
          характеристик и оставшихся непонятных частей наименования.
        </p>
      </section>
    );
  }

  const orderedSamples = [...explanation.samples].sort((left, right) => {
    const kindDifference = sampleOrder[left.kind] - sampleOrder[right.kind];

    if (kindDifference !== 0) {
      return kindDifference;
    }

    const coverageDifference = left.coverage - right.coverage;

    if (coverageDifference !== 0) {
      return coverageDifference;
    }

    return left.rowNumber - right.rowNumber;
  });

  return (
    <section className="rounded-3xl border border-fuchsia-500/25 bg-fuchsia-500/[0.04] p-6">
      <h2 className="text-xl font-semibold text-white">
        Объяснение наименований
      </h2>

      <p className="mt-2 max-w-5xl text-sm leading-6 text-slate-400">
        Панель объединяет уже полученные evidence производителя, типа товара и
        характеристик. Повторное распознавание для этой панели не запускается.
      </p>

      <div className="mt-5 rounded-2xl border border-sky-500/30 bg-sky-500/[0.07] p-5">
        <p className="font-medium text-sky-100">
          Это read-only объяснение — данные импорта не изменяются
        </p>

        <p className="mt-2 text-sm leading-6 text-sky-200/80">
          Цвет показывает, какая часть Name была понята системой. Непонятные
          фрагменты только выделяются для проверки и не становятся
          производителями, типами или характеристиками автоматически.
        </p>
      </div>

      <div className="mt-5 flex flex-wrap gap-2 text-xs">
        <span className="rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-emerald-200">
          Производитель
        </span>

        <span className="rounded-full border border-violet-500/30 bg-violet-500/10 px-3 py-1 text-violet-200">
          Тип товара
        </span>

        <span className="rounded-full border border-sky-500/30 bg-sky-500/10 px-3 py-1 text-sky-200">
          Характеристика
        </span>

        <span className="rounded-full border border-fuchsia-500/30 bg-fuchsia-500/10 px-3 py-1 text-fuchsia-200">
          Пересечение evidence
        </span>

        <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 text-amber-200">
          Непонятная часть
        </span>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <SummaryCard
          label="Проанализировано строк"
          value={explanation.rowsAnalyzedCount}
          description="Строки Excel с непустым Name."
        />

        <SummaryCard
          label="Есть evidence"
          value={explanation.rowsWithEvidenceCount}
          description="Найден производитель, тип или характеристика."
          tone="Info"
        />

        <SummaryCard
          label="Полностью объяснено"
          value={explanation.fullyExplainedRowsCount}
          description="Все значимые символы покрыты evidence."
          tone="Success"
        />

        <SummaryCard
          label="Объяснено частично"
          value={explanation.partiallyExplainedRowsCount}
          description="Понятна только часть наименования."
          tone="Info"
        />

        <SummaryCard
          label="Не объяснено"
          value={explanation.unexplainedRowsCount}
          description="Нет покрытия значимых частей Name."
          tone={explanation.unexplainedRowsCount > 0 ? "Warning" : "Success"}
        />

        <SummaryCard
          label="Среднее покрытие"
          value={formatCoverage(explanation.averageCoverage)}
          description="Доля распознанных букв и цифр по всему пакету."
          tone="Info"
        />
      </div>

      <div className="mt-7">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h3 className="font-semibold text-white">Диагностические строки</h3>

            <p className="mt-1 text-sm text-slate-400">
              Сначала показаны полностью непонятные, затем частично и полностью
              объяснённые строки.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
              Получено примеров: {orderedSamples.length}
            </span>

            {explanation.samplesTruncated && (
              <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 text-xs text-amber-200">
                Список ограничен
              </span>
            )}
          </div>
        </div>

        {orderedSamples.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
            Строки для объяснения отсутствуют.
          </div>
        ) : (
          <CatalogImportDiagnosticList
            items={orderedSamples}
            ariaLabel="Примеры разбора наименований"
            renderItem={(sample) => (
              <SampleCard
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
