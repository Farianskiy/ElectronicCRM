"use client";

import { CatalogImportDiagnosticList } from "../CatalogImportDiagnosticList";
import type {
  CatalogImportProductTypeSuggestionShadow,
  CatalogImportProductTypeSuggestionShadowCandidate,
  CatalogImportProductTypeSuggestionShadowSample,
  CatalogImportProductTypeSuggestionShadowSampleKind,
} from "../../model/types";

interface CatalogImportProductTypeSuggestionShadowPanelProps {
  shadow: CatalogImportProductTypeSuggestionShadow | null;
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

interface TextRange {
  startIndex: number;
  endIndex: number;
}

interface HighlightedSegment {
  text: string;
  isEvidence: boolean;
}

const sampleOrder: Record<
  CatalogImportProductTypeSuggestionShadowSampleKind,
  number
> = {
  Conflict: 1,
  NameConflict: 2,
  Suggestion: 3,
  Match: 4,
  Unresolved: 5,
  None: 6,
};

function formatConfidence(confidence?: number | null): string {
  if (confidence === null || confidence === undefined) {
    return "—";
  }

  if (!Number.isFinite(confidence)) {
    return "—";
  }

  return `${Math.round(confidence * 100)}%`;
}

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
  kind: CatalogImportProductTypeSuggestionShadowSampleKind,
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
        label: "Конфликт с типом пакета",
        cardClassName: "border-red-500/30 bg-red-500/[0.05]",
        badgeClassName: "border-red-500/30 bg-red-500/10 text-red-200",
        valueClassName: "text-red-300",
      };

    case "NameConflict":
      return {
        label: "Конфликт внутри наименования",
        cardClassName: "border-amber-500/30 bg-amber-500/[0.05]",
        badgeClassName: "border-amber-500/30 bg-amber-500/10 text-amber-200",
        valueClassName: "text-amber-300",
      };

    case "Suggestion":
      return {
        label: "Предложение",
        cardClassName: "border-sky-500/30 bg-sky-500/[0.05]",
        badgeClassName: "border-sky-500/30 bg-sky-500/10 text-sky-200",
        valueClassName: "text-sky-300",
      };

    case "Unresolved":
      return {
        label: "Тип не определён",
        cardClassName: "border-slate-500/25 bg-slate-500/[0.04]",
        badgeClassName: "border-slate-500/30 bg-slate-500/10 text-slate-200",
        valueClassName: "text-slate-300",
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

function SummaryCard({
  label,
  value,
  description,
  tone = "Neutral",
}: SummaryCardProps) {
  return (
    <div className={`rounded-2xl border p-4 ${getSummaryCardClassName(tone)}`}>
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-2 text-2xl font-semibold text-white">{value}</p>

      <p className="mt-2 text-xs leading-5 text-slate-500">{description}</p>
    </div>
  );
}

function collectEvidenceRanges(
  sample: CatalogImportProductTypeSuggestionShadowSample,
): TextRange[] {
  const ranges = sample.candidates
    .flatMap((candidate) => candidate.evidence)
    .map((evidence) => ({
      startIndex: Math.max(0, evidence.startIndex),
      endIndex: Math.min(sample.productName.length, evidence.endIndex),
    }))
    .filter((range) => range.endIndex > range.startIndex)
    .sort((left, right) => {
      if (left.startIndex !== right.startIndex) {
        return left.startIndex - right.startIndex;
      }

      return left.endIndex - right.endIndex;
    });

  const mergedRanges: TextRange[] = [];

  for (const range of ranges) {
    const previousRange = mergedRanges.at(-1);

    if (!previousRange || range.startIndex > previousRange.endIndex) {
      mergedRanges.push({ ...range });
      continue;
    }

    previousRange.endIndex = Math.max(previousRange.endIndex, range.endIndex);
  }

  return mergedRanges;
}

function buildHighlightedSegments(
  sample: CatalogImportProductTypeSuggestionShadowSample,
): HighlightedSegment[] {
  const ranges = collectEvidenceRanges(sample);

  if (ranges.length === 0) {
    return [
      {
        text: sample.productName,
        isEvidence: false,
      },
    ];
  }

  const segments: HighlightedSegment[] = [];
  let currentIndex = 0;

  for (const range of ranges) {
    if (range.startIndex > currentIndex) {
      segments.push({
        text: sample.productName.slice(currentIndex, range.startIndex),
        isEvidence: false,
      });
    }

    segments.push({
      text: sample.productName.slice(range.startIndex, range.endIndex),
      isEvidence: true,
    });

    currentIndex = range.endIndex;
  }

  if (currentIndex < sample.productName.length) {
    segments.push({
      text: sample.productName.slice(currentIndex),
      isEvidence: false,
    });
  }

  return segments;
}

function HighlightedProductName({
  sample,
}: {
  sample: CatalogImportProductTypeSuggestionShadowSample;
}) {
  const segments = buildHighlightedSegments(sample);

  return (
    <div className="rounded-xl border border-white/10 bg-black/30 p-4 font-mono text-sm leading-7 text-slate-300">
      {segments.map((segment, index) =>
        segment.isEvidence ? (
          <mark
            key={`${index}-${segment.text}`}
            className="rounded bg-violet-400/25 px-1 text-violet-100"
          >
            {segment.text}
          </mark>
        ) : (
          <span key={`${index}-${segment.text}`}>{segment.text}</span>
        ),
      )}
    </div>
  );
}

function CandidateCard({
  candidate,
  suggestedProductTypeId,
}: {
  candidate: CatalogImportProductTypeSuggestionShadowCandidate;
  suggestedProductTypeId?: string | null;
}) {
  const isSelected = candidate.productTypeId === suggestedProductTypeId;

  return (
    <div
      className={[
        "rounded-xl border p-4",
        isSelected
          ? "border-violet-500/30 bg-violet-500/[0.06]"
          : "border-white/10 bg-black/20",
      ].join(" ")}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-semibold text-white">
              {candidate.productTypeName}
            </p>

            {isSelected && (
              <span className="rounded-full border border-violet-500/30 bg-violet-500/10 px-2 py-1 text-xs text-violet-200">
                Предложен
              </span>
            )}

            {!isSelected && (
              <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-2 py-1 text-xs text-amber-200">
                Альтернатива
              </span>
            )}
          </div>

          <p className="mt-1 font-mono text-xs text-violet-300">
            {candidate.productTypeCode}
          </p>
        </div>

        <div className="text-right">
          <p className="text-xs text-slate-500">Уверенность</p>
          <p className="mt-1 font-semibold text-white">
            {formatConfidence(candidate.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-3 flex flex-wrap gap-2">
        <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
          Приоритет: {candidate.highestPriority}
        </span>

        <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
          Evidence: {candidate.evidence.length}
        </span>
      </div>

      <div className="mt-4 grid gap-3">
        {candidate.evidence.map((evidence) => (
          <div
            key={`${evidence.dictionaryTermId}-${evidence.startIndex}`}
            className="rounded-xl border border-white/10 bg-black/20 p-3"
          >
            <div className="flex flex-wrap items-center gap-2">
              <span className="font-mono text-sm font-semibold text-violet-200">
                {evidence.rawValue}
              </span>

              <span className="rounded-full border border-violet-500/20 bg-violet-500/10 px-2 py-1 text-xs text-violet-200">
                priority={evidence.priority}
              </span>

              <span className="rounded-full border border-white/10 bg-white/[0.04] px-2 py-1 text-xs text-slate-300">
                {evidence.source}
              </span>
            </div>

            <div className="mt-3 grid gap-2 text-xs sm:grid-cols-2 xl:grid-cols-4">
              <div>
                <p className="text-slate-500">Словарная фраза</p>
                <p className="mt-1 text-slate-200">{evidence.phrase}</p>
              </div>

              <div>
                <p className="text-slate-500">Нормализовано</p>
                <p className="mt-1 font-mono text-slate-200">
                  {evidence.normalizedValue}
                </p>
              </div>

              <div>
                <p className="text-slate-500">Начало span</p>
                <p className="mt-1 font-mono text-slate-200">
                  {evidence.startIndex}
                </p>
              </div>

              <div>
                <p className="text-slate-500">Длина span</p>
                <p className="mt-1 font-mono text-slate-200">
                  {evidence.length}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function SampleCard({
  sample,
}: {
  sample: CatalogImportProductTypeSuggestionShadowSample;
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
          <p className="text-xs text-slate-500">Уверенность</p>
          <p className={`mt-1 font-semibold ${presentation.valueClassName}`}>
            {formatConfidence(sample.confidence)}
          </p>
        </div>
      </div>

      <p className="mt-4 text-sm leading-6 text-slate-300">{sample.details}</p>

      <div className="mt-4">
        <p className="mb-2 text-xs text-slate-500">
          Фиолетовым выделено evidence типа товара
        </p>

        <HighlightedProductName sample={sample} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Тип пакета</p>
          <p className="mt-1 text-sm text-slate-200">
            {sample.selectedProductTypeName ?? "Не выбран"}
          </p>
          <p className="mt-1 font-mono text-xs text-slate-500">
            {sample.selectedProductTypeCode ?? "—"}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Предложено по Name</p>
          <p className={`mt-1 text-sm ${presentation.valueClassName}`}>
            {sample.suggestedProductTypeName ?? "Не определено"}
          </p>
          <p className="mt-1 font-mono text-xs text-slate-500">
            {sample.suggestedProductTypeCode ?? "—"}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Приоритет</p>
          <p className="mt-1 font-mono text-sm text-slate-200">
            {sample.highestPriority ?? "—"}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Кандидатов</p>
          <p className="mt-1 font-mono text-sm text-slate-200">
            {sample.candidates.length}
          </p>
        </div>
      </div>

      {sample.candidates.length > 0 && (
        <div className="mt-5">
          <p className="text-sm font-medium text-slate-300">
            Кандидаты и evidence
          </p>

          <div className="mt-3 grid gap-3">
            {sample.candidates.map((candidate) => (
              <CandidateCard
                key={`${sample.rowNumber}-${candidate.productTypeId}`}
                candidate={candidate}
                suggestedProductTypeId={sample.suggestedProductTypeId}
              />
            ))}
          </div>
        </div>
      )}
    </article>
  );
}

export function CatalogImportProductTypeSuggestionShadowPanel({
  shadow,
}: CatalogImportProductTypeSuggestionShadowPanelProps) {
  if (shadow === null) {
    return (
      <section className="rounded-3xl border border-violet-500/20 bg-violet-500/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Тип товара по наименованию
        </h2>

        <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
          После анализа здесь появятся предложения Product Type для строк
          текущего Excel-пакета.
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
    <section className="rounded-3xl border border-violet-500/25 bg-violet-500/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Тип товара по наименованию
        </h2>

        <p className="mt-2 max-w-5xl text-sm leading-6 text-slate-400">
          Product Type Suggestion Index один раз загружается для всего пакета,
          проверяет Approved-термины в каждой строке и сравнивает предложение с
          выбранным типом импорта.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-sky-500/30 bg-sky-500/[0.07] p-5">
        <p className="font-medium text-sky-100">
          Это только наблюдение — ProductTypeId не изменяется
        </p>

        <p className="mt-2 text-sm leading-6 text-sky-200/80">
          Совпадение подтверждает выбранный тип. Конфликт только предупреждает о
          расхождении. Если тип пакета не выбран, найденные типы остаются
          предложениями и не назначаются автоматически.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-white/10 bg-black/20 p-5">
        <p className="text-sm text-slate-400">Текущий выбранный тип пакета</p>

        {shadow.hasSelectedProductType ? (
          <>
            <p className="mt-2 font-semibold text-white">
              {shadow.selectedProductTypeName}
            </p>

            <p className="mt-1 font-mono text-sm text-violet-300">
              {shadow.selectedProductTypeCode}
            </p>
          </>
        ) : (
          <p className="mt-2 text-sm text-sky-200">
            Тип ещё не выбран. Все найденные варианты показаны только как
            предложения.
          </p>
        )}
      </div>

      {shadow.hasMixedSuggestedProductTypes && (
        <div className="mt-5 rounded-2xl border border-amber-500/30 bg-amber-500/10 p-5">
          <p className="font-medium text-amber-100">
            В пакете найдены разные предполагаемые типы
          </p>

          <p className="mt-2 text-sm leading-6 text-amber-200/80">
            Строки Excel указывают более чем на один Product Type. Назначать тип
            всему пакету автоматически небезопасно — требуется проверка
            пользователя.
          </p>
        </div>
      )}

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <SummaryCard
          label="Проанализировано строк"
          value={shadow.rowsAnalyzedCount}
          description="Строки с непустым наименованием."
        />

        <SummaryCard
          label="Тип найден"
          value={shadow.suggestedRowsCount}
          description="Строки с одним победившим Product Type."
          tone="Info"
        />

        <SummaryCard
          label="Совпадений"
          value={shadow.matchesCount}
          description="Предложенный тип совпал с типом пакета."
          tone="Success"
        />

        <SummaryCard
          label="Конфликтов"
          value={shadow.conflictsCount}
          description="Предложенный тип отличается от типа пакета."
          tone={shadow.conflictsCount > 0 ? "Danger" : "Success"}
        />

        <SummaryCard
          label="Предложений"
          value={shadow.suggestionsCount}
          description="Тип пакета пуст, но по Name найден кандидат."
          tone="Info"
        />

        <SummaryCard
          label="Конфликтов внутри Name"
          value={shadow.nameConflictsCount}
          description="В одной строке найдены равноприоритетные типы."
          tone={shadow.nameConflictsCount > 0 ? "Warning" : "Success"}
        />

        <SummaryCard
          label="Не определено"
          value={shadow.unresolvedCount}
          description="Approved-термин типа в Name не найден."
          tone="Neutral"
        />

        <SummaryCard
          label="Разных типов"
          value={shadow.distinctSuggestedProductTypesCount}
          description="Количество типов среди победивших предложений."
          tone={shadow.hasMixedSuggestedProductTypes ? "Warning" : "Neutral"}
        />
      </div>

      <div className="mt-7">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h3 className="font-semibold text-white">
              Агрегация по всему Excel
            </h3>

            <p className="mt-1 text-sm text-slate-400">
              Одинаковые предложения объединены, чтобы не проверять каждую
              повторяющуюся строку отдельно.
            </p>
          </div>

          <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
            Групп: {shadow.typeGroups.length}
          </span>
        </div>

        {shadow.typeGroups.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
            В пакете нет строк с однозначно предложенным типом.
          </div>
        ) : (
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            {shadow.typeGroups.map((group) => (
              <article
                key={group.productTypeId}
                className="rounded-2xl border border-violet-500/20 bg-black/20 p-5"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-semibold text-white">
                      {group.productTypeName}
                    </p>

                    <p className="mt-1 font-mono text-xs text-violet-300">
                      {group.productTypeCode}
                    </p>
                  </div>

                  <div className="text-right">
                    <p className="text-xs text-slate-500">Строк</p>
                    <p className="mt-1 text-xl font-semibold text-white">
                      {group.rowsCount}
                    </p>
                  </div>
                </div>

                <div className="mt-4 grid grid-cols-3 gap-3 text-center">
                  <div className="rounded-xl border border-green-500/20 bg-green-500/[0.05] p-3">
                    <p className="text-xs text-slate-500">Совпадений</p>
                    <p className="mt-1 font-semibold text-green-300">
                      {group.matchesCount}
                    </p>
                  </div>

                  <div className="rounded-xl border border-red-500/20 bg-red-500/[0.05] p-3">
                    <p className="text-xs text-slate-500">Конфликтов</p>
                    <p className="mt-1 font-semibold text-red-300">
                      {group.conflictsCount}
                    </p>
                  </div>

                  <div className="rounded-xl border border-sky-500/20 bg-sky-500/[0.05] p-3">
                    <p className="text-xs text-slate-500">Предложений</p>
                    <p className="mt-1 font-semibold text-sky-300">
                      {group.suggestionsCount}
                    </p>
                  </div>
                </div>

                <div className="mt-4 flex flex-wrap gap-2 text-xs">
                  <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-slate-300">
                    Максимальная уверенность:{" "}
                    {formatConfidence(group.highestConfidence)}
                  </span>

                  <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-slate-300">
                    Примеры строк:{" "}
                    {group.exampleRowNumbers.length > 0
                      ? group.exampleRowNumbers.join(", ")
                      : "—"}
                  </span>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>

      <div className="mt-7">
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h3 className="font-semibold text-white">Диагностические строки</h3>

            <p className="mt-1 text-sm text-slate-400">
              Сначала показаны конфликты, затем предложения, совпадения и
              неопределённые строки.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
              Получено примеров: {orderedSamples.length}
            </span>

            {shadow.samplesTruncated && (
              <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-3 py-1 text-xs text-amber-200">
                Список ограничен
              </span>
            )}
          </div>
        </div>

        {orderedSamples.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
            Диагностические строки отсутствуют.
          </div>
        ) : (
          <CatalogImportDiagnosticList
            items={orderedSamples}
            ariaLabel="Примеры определения типа товара"
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
