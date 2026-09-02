"use client";

import { useMutation } from "@tanstack/react-query";
import type { FormEvent } from "react";
import { useState } from "react";
import { previewCatalogProductTypeSuggestion } from "../api/previewCatalogProductTypeSuggestion";
import type {
  CatalogProductTypeSuggestionCandidate,
  CatalogProductTypeSuggestionPreview,
  CatalogProductTypeSuggestionStatus,
} from "../model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";

interface ProductTypeSuggestionExample {
  label: string;
  productName: string;
}

interface EvidenceSpan {
  startIndex: number;
  endIndex: number;
}

interface HighlightedSegment {
  text: string;
  isEvidence: boolean;
}

interface UnusedSpan {
  rawValue: string;
  startIndex: number;
  length: number;
  endIndex: number;
}

const productTypeSuggestionExamples: ProductTypeSuggestionExample[] = [
  {
    label: "Сильный термин",
    productName: "автоматический выключатель CHINT NB1-63 3P C16",
  },
  {
    label: "Тип и альтернатива",
    productName: "диф автомат IEK 2P C16 30mA",
  },
  {
    label: "Конфликт типов",
    productName: "УЗО рубильник 2P 40A",
  },
  {
    label: "Без термина типа",
    productName: "CHINT NB1-63 3P C16",
  },
];

export function ProductTypeSuggestionPreviewPanel() {
  const [productName, setProductName] = useState(
    productTypeSuggestionExamples[0]?.productName ?? "",
  );

  const [validationError, setValidationError] = useState<string | null>(null);

  const previewMutation = useMutation({
    mutationFn: previewCatalogProductTypeSuggestion,
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    if (productName.trim().length === 0) {
      setValidationError("Введите наименование товара.");
      return;
    }

    setValidationError(null);

    previewMutation.mutate({
      productName,
    });
  }

  function handleProductNameChange(value: string): void {
    setProductName(value);
    setValidationError(null);
    previewMutation.reset();
  }

  function applyExample(example: ProductTypeSuggestionExample): void {
    setProductName(example.productName);
    setValidationError(null);
    previewMutation.reset();
  }

  return (
    <section className="rounded-3xl border border-violet-500/20 bg-violet-500/[0.05] p-6">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-violet-300">
          Product Type Suggestions
        </p>

        <h2 className="mt-2 text-xl font-semibold text-white">
          Предполагаемый тип товара по наименованию
        </h2>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Сервис ищет только подтверждённые словарные термины типа товара,
          учитывает границы слов и приоритеты, а затем показывает выбранный тип,
          альтернативы, конфликт и точные участки наименования.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-blue-500/30 bg-blue-500/10 p-4">
        <p className="font-medium text-blue-100">
          Это безопасная read-only проверка
        </p>

        <p className="mt-2 text-sm leading-6 text-blue-100/80">
          Результат является только предложением. Проверка не назначает
          ProductTypeId, не изменяет товары, не изменяет импорт и не создаёт
          словарные термины.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="mt-6 grid gap-4">
        <label className="grid gap-2">
          <span className="text-sm font-medium text-slate-300">
            Наименование товара
          </span>

          <textarea
            value={productName}
            onChange={(event) => handleProductNameChange(event.target.value)}
            rows={3}
            placeholder="Например: автоматический выключатель CHINT NB1-63 3P C16"
            className="resize-y rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-violet-400 focus:ring-2 focus:ring-violet-400/20"
          />
        </label>

        {validationError && (
          <div className="rounded-2xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-200">
            {validationError}
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          <button
            type="submit"
            disabled={previewMutation.isPending}
            className="rounded-2xl bg-violet-500 px-5 py-3 text-sm font-medium text-white transition hover:bg-violet-400 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {previewMutation.isPending
              ? "Определяем тип..."
              : "Проверить тип товара"}
          </button>

          {previewMutation.data && (
            <button
              type="button"
              onClick={() => previewMutation.reset()}
              className="rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-3 text-sm font-medium text-slate-300 transition hover:bg-white/[0.08]"
            >
              Скрыть результат
            </button>
          )}
        </div>
      </form>

      <div className="mt-6 border-t border-white/10 pt-5">
        <p className="text-sm font-medium text-slate-300">
          Подготовленные диагностические примеры
        </p>

        <div className="mt-3 flex flex-wrap gap-3">
          {productTypeSuggestionExamples.map((example) => (
            <button
              key={example.label}
              type="button"
              onClick={() => applyExample(example)}
              className="rounded-xl border border-white/10 bg-white/[0.03] px-4 py-2 text-sm text-slate-300 transition hover:border-violet-500/30 hover:bg-violet-500/10 hover:text-violet-200"
            >
              {example.label}
            </button>
          ))}
        </div>
      </div>

      {previewMutation.isError && (
        <div className="mt-6 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-red-200">
          <p className="font-medium">Не удалось определить тип товара</p>

          <p className="mt-2 text-sm">
            {getApiErrorMessage(
              previewMutation.error,
              "Сервис Product Type Suggestions не смог обработать наименование.",
            )}
          </p>
        </div>
      )}

      {previewMutation.data && (
        <ProductTypeSuggestionResult preview={previewMutation.data} />
      )}
    </section>
  );
}

function ProductTypeSuggestionResult({
  preview,
}: {
  preview: CatalogProductTypeSuggestionPreview;
}) {
  const statusPresentation = getStatusPresentation(preview.status);
  const highlightedSegments = buildHighlightedSegments(preview);
  const unusedSpans = collectUnusedSpans(preview);

  return (
    <div className="mt-6 grid gap-5 border-t border-white/10 pt-6">
      <div
        className={`rounded-2xl border p-5 ${statusPresentation.containerClassName}`}
      >
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className={`font-semibold ${statusPresentation.titleClassName}`}>
              {statusPresentation.title}
            </p>

            <p className="mt-2 text-sm leading-6 text-slate-300">
              {getStatusDescription(preview)}
            </p>
          </div>

          <span
            className={`rounded-full border px-3 py-1 text-xs font-semibold ${statusPresentation.badgeClassName}`}
          >
            {preview.status}
          </span>
        </div>
      </div>

      <div className="rounded-2xl border border-white/10 bg-black/20 p-5">
        <h3 className="font-semibold text-white">
          Наименование с evidence типа товара
        </h3>

        <p className="mt-2 text-sm text-slate-400">
          Фиолетовым выделены только те части, которые участвовали в выборе
          Product Type.
        </p>

        <div className="mt-4 rounded-xl border border-white/10 bg-black/30 p-4 font-mono text-sm leading-7 text-slate-300">
          {highlightedSegments.map((segment, index) =>
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
      </div>

      {preview.selectedCandidate && (
        <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/[0.08] p-5">
          <p className="text-sm text-emerald-200/80">Предложенный тип</p>

          <p className="mt-2 text-lg font-semibold text-emerald-100">
            {preview.selectedCandidate.productTypeName}
          </p>

          <p className="mt-1 font-mono text-sm text-emerald-300">
            {preview.selectedCandidate.productTypeCode}
          </p>

          <div className="mt-4 flex flex-wrap gap-2">
            <span className="rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-xs text-emerald-200">
              Уверенность:{" "}
              {formatConfidence(preview.selectedCandidate.confidence)}
            </span>

            <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
              Приоритет: {preview.selectedCandidate.highestPriority}
            </span>
          </div>
        </div>
      )}

      <div>
        <div className="flex flex-wrap items-end justify-between gap-3">
          <div>
            <h3 className="font-semibold text-white">Кандидаты типа товара</h3>

            <p className="mt-1 text-sm text-slate-400">
              Здесь сохраняются как победивший кандидат, так и низкоприоритетные
              альтернативы.
            </p>
          </div>

          <span className="rounded-full border border-white/10 bg-black/20 px-3 py-1 text-xs text-slate-300">
            Найдено: {preview.candidates.length}
          </span>
        </div>

        {preview.candidates.length === 0 ? (
          <div className="mt-4 rounded-2xl border border-slate-500/20 bg-slate-500/[0.06] p-5 text-sm text-slate-300">
            В наименовании не найден ни один Approved-термин типа товара.
          </div>
        ) : (
          <div className="mt-4 grid gap-4">
            {preview.candidates.map((candidate) => (
              <ProductTypeCandidateCard
                key={candidate.productTypeId}
                candidate={candidate}
                isSelected={
                  candidate.productTypeId ===
                  preview.selectedCandidate?.productTypeId
                }
              />
            ))}
          </div>
        )}
      </div>

      <div className="rounded-2xl border border-sky-500/20 bg-sky-500/[0.05] p-5">
        <h3 className="font-semibold text-sky-100">
          Части, не использованные для определения типа
        </h3>

        <p className="mt-2 text-sm leading-6 text-slate-400">
          Это не обязательно ошибки. Здесь могут находиться производитель,
          серия, модель и характеристики. Текущий сервис типа товара просто не
          использовал их как доказательство Product Type.
        </p>

        {unusedSpans.length === 0 ? (
          <p className="mt-4 text-sm text-sky-200">
            Всё непустое наименование вошло в evidence типа товара.
          </p>
        ) : (
          <div className="mt-4 flex flex-wrap gap-2">
            {unusedSpans.map((span) => (
              <span
                key={`${span.startIndex}-${span.endIndex}-${span.rawValue}`}
                className="rounded-xl border border-sky-500/20 bg-black/20 px-3 py-2 font-mono text-xs text-sky-100"
              >
                {span.rawValue} · start={span.startIndex}, length={span.length}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function ProductTypeCandidateCard({
  candidate,
  isSelected,
}: {
  candidate: CatalogProductTypeSuggestionCandidate;
  isSelected: boolean;
}) {
  return (
    <article
      className={`rounded-2xl border p-5 ${
        isSelected
          ? "border-emerald-500/30 bg-emerald-500/[0.06]"
          : "border-white/10 bg-black/20"
      }`}
    >
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h4 className="font-semibold text-white">
              {candidate.productTypeName}
            </h4>

            {isSelected && (
              <span className="rounded-full border border-emerald-500/30 bg-emerald-500/10 px-2 py-1 text-xs text-emerald-200">
                Выбран
              </span>
            )}

            {!isSelected && (
              <span className="rounded-full border border-amber-500/30 bg-amber-500/10 px-2 py-1 text-xs text-amber-200">
                Альтернатива
              </span>
            )}
          </div>

          <p className="mt-1 font-mono text-sm text-violet-300">
            {candidate.productTypeCode}
          </p>
        </div>

        <div className="text-right">
          <p className="text-xs text-slate-500">Уверенность</p>
          <p className="mt-1 text-lg font-semibold text-white">
            {formatConfidence(candidate.confidence)}
          </p>
        </div>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Максимальный приоритет</p>
          <p className="mt-1 font-mono text-sm text-slate-200">
            {candidate.highestPriority}
          </p>
        </div>

        <div className="rounded-xl border border-white/10 bg-black/20 p-3">
          <p className="text-xs text-slate-500">Количество evidence</p>
          <p className="mt-1 font-mono text-sm text-slate-200">
            {candidate.evidence.length}
          </p>
        </div>
      </div>

      <div className="mt-4">
        <p className="text-sm font-medium text-slate-300">
          Словарные доказательства
        </p>

        <div className="mt-3 grid gap-3">
          {candidate.evidence.map((evidence) => (
            <div
              key={`${evidence.dictionaryTermId}-${evidence.startIndex}`}
              className="rounded-xl border border-white/10 bg-black/20 p-4"
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

              <div className="mt-3 grid gap-3 text-xs sm:grid-cols-2 lg:grid-cols-4">
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
    </article>
  );
}

function getStatusPresentation(status: CatalogProductTypeSuggestionStatus) {
  switch (status) {
    case "Suggested":
      return {
        title: "Тип товара предложен",
        containerClassName: "border-emerald-500/30 bg-emerald-500/[0.08]",
        titleClassName: "text-emerald-100",
        badgeClassName:
          "border-emerald-500/30 bg-emerald-500/10 text-emerald-200",
      };

    case "Conflict":
      return {
        title: "Обнаружен конфликт типов",
        containerClassName: "border-red-500/30 bg-red-500/[0.08]",
        titleClassName: "text-red-100",
        badgeClassName: "border-red-500/30 bg-red-500/10 text-red-200",
      };

    case "Unresolved":
      return {
        title: "Тип товара не определён",
        containerClassName: "border-sky-500/30 bg-sky-500/[0.08]",
        titleClassName: "text-sky-100",
        badgeClassName: "border-sky-500/30 bg-sky-500/10 text-sky-200",
      };

    default:
      return {
        title: "Результат отсутствует",
        containerClassName: "border-slate-500/30 bg-slate-500/[0.08]",
        titleClassName: "text-slate-100",
        badgeClassName: "border-slate-500/30 bg-slate-500/10 text-slate-200",
      };
  }
}

function getStatusDescription(
  preview: CatalogProductTypeSuggestionPreview,
): string {
  if (preview.status === "Suggested" && preview.selectedCandidate) {
    return `На основании Approved-терминов предложен тип «${preview.selectedCandidate.productTypeName}». Это предложение ещё ничего не назначает.`;
  }

  if (preview.status === "Conflict") {
    const productTypeNames = preview.candidates
      .map((candidate) => candidate.productTypeName)
      .join(", ");

    return `Наивысший приоритет одновременно получили разные типы: ${productTypeNames}. Автоматический выбор запрещён.`;
  }

  if (preview.status === "Unresolved") {
    return "В наименовании не найден подтверждённый словарный термин типа товара. Сервис честно оставил результат неопределённым.";
  }

  return "Сервис не сформировал предложение типа товара.";
}

function formatConfidence(confidence: number): string {
  return `${Math.round(confidence * 100)}%`;
}

function collectEvidenceSpans(
  preview: CatalogProductTypeSuggestionPreview,
): EvidenceSpan[] {
  const spans = preview.candidates
    .flatMap((candidate) => candidate.evidence)
    .map((evidence) => ({
      startIndex: Math.max(0, evidence.startIndex),
      endIndex: Math.min(preview.productName.length, evidence.endIndex),
    }))
    .filter((span) => span.endIndex > span.startIndex)
    .sort((left, right) => {
      if (left.startIndex !== right.startIndex) {
        return left.startIndex - right.startIndex;
      }

      return left.endIndex - right.endIndex;
    });

  const mergedSpans: EvidenceSpan[] = [];

  for (const span of spans) {
    const previousSpan = mergedSpans.at(-1);

    if (!previousSpan || span.startIndex > previousSpan.endIndex) {
      mergedSpans.push({ ...span });
      continue;
    }

    previousSpan.endIndex = Math.max(previousSpan.endIndex, span.endIndex);
  }

  return mergedSpans;
}

function buildHighlightedSegments(
  preview: CatalogProductTypeSuggestionPreview,
): HighlightedSegment[] {
  const spans = collectEvidenceSpans(preview);

  if (spans.length === 0) {
    return [
      {
        text: preview.productName,
        isEvidence: false,
      },
    ];
  }

  const segments: HighlightedSegment[] = [];
  let currentIndex = 0;

  for (const span of spans) {
    if (span.startIndex > currentIndex) {
      segments.push({
        text: preview.productName.slice(currentIndex, span.startIndex),
        isEvidence: false,
      });
    }

    segments.push({
      text: preview.productName.slice(span.startIndex, span.endIndex),
      isEvidence: true,
    });

    currentIndex = span.endIndex;
  }

  if (currentIndex < preview.productName.length) {
    segments.push({
      text: preview.productName.slice(currentIndex),
      isEvidence: false,
    });
  }

  return segments;
}

function collectUnusedSpans(
  preview: CatalogProductTypeSuggestionPreview,
): UnusedSpan[] {
  const evidenceSpans = collectEvidenceSpans(preview);
  const unusedRanges: EvidenceSpan[] = [];
  let currentIndex = 0;

  for (const evidenceSpan of evidenceSpans) {
    if (evidenceSpan.startIndex > currentIndex) {
      unusedRanges.push({
        startIndex: currentIndex,
        endIndex: evidenceSpan.startIndex,
      });
    }

    currentIndex = Math.max(currentIndex, evidenceSpan.endIndex);
  }

  if (currentIndex < preview.productName.length) {
    unusedRanges.push({
      startIndex: currentIndex,
      endIndex: preview.productName.length,
    });
  }

  const unusedSpans: UnusedSpan[] = [];

  for (const range of unusedRanges) {
    const rangeText = preview.productName.slice(
      range.startIndex,
      range.endIndex,
    );

    const tokenPattern = /\S+/gu;
    let match = tokenPattern.exec(rangeText);

    while (match) {
      const startIndex = range.startIndex + match.index;
      const rawValue = match[0];

      unusedSpans.push({
        rawValue,
        startIndex,
        length: rawValue.length,
        endIndex: startIndex + rawValue.length,
      });

      match = tokenPattern.exec(rangeText);
    }
  }

  return unusedSpans;
}
