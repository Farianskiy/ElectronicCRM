import type {
  CatalogRecognitionSource,
  CatalogRecognizedCharacteristic,
} from "../model/types";

export type RecognitionCandidateCardTone = "Accepted" | "Conflict" | "Evidence";

interface RecognitionCandidateCardProps {
  candidate: CatalogRecognizedCharacteristic;
  productName: string;
  tone: RecognitionCandidateCardTone;
}

export function RecognitionCandidateCard({
  candidate,
  productName,
  tone,
}: RecognitionCandidateCardProps) {
  const spanEndIndex = candidate.startIndex + candidate.length;

  const spanFragment = getSpanFragment(
    productName,
    candidate.startIndex,
    candidate.length,
  );

  return (
    <article className={`rounded-3xl border p-5 ${getCardClassName(tone)}`}>
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div className="min-w-0">
          <p className="break-all font-mono text-sm font-semibold text-white">
            {candidate.characteristicCode}
          </p>

          <p className="mt-2 text-sm text-slate-400">
            Итоговое нормализованное значение
          </p>

          <p className="mt-1 break-words text-2xl font-semibold text-white">
            {candidate.normalizedValue}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <RecognitionSourceBadge source={candidate.source} />

          <span
            className={`rounded-full border px-3 py-1 text-xs font-medium ${getConfidenceClassName(
              candidate.confidence,
            )}`}
          >
            {formatConfidence(candidate.confidence)}
          </span>
        </div>
      </div>

      <div className="mt-5 grid gap-3 sm:grid-cols-2">
        <CandidateDetail
          label="Исходное значение RawValue"
          value={candidate.rawValue}
        />

        <CandidateDetail
          label="Фрагмент по span"
          value={spanFragment ?? "Span выходит за пределы строки"}
        />

        <CandidateDetail
          label="Начальная позиция StartIndex"
          value={candidate.startIndex.toString()}
        />

        <CandidateDetail
          label="Длина Length"
          value={candidate.length.toString()}
        />

        <CandidateDetail
          label="Диапазон span"
          value={`${candidate.startIndex}..${spanEndIndex}`}
        />

        <CandidateDetail
          label="Приоритет"
          value={candidate.priority.toString()}
        />
      </div>

      <div className="mt-3 rounded-2xl border border-white/10 bg-black/20 p-4">
        <p className="text-xs text-slate-500">RecognizerKey</p>

        <p className="mt-2 break-all font-mono text-sm text-slate-200">
          {candidate.recognizerKey}
        </p>
      </div>
    </article>
  );
}

function getSpanFragment(
  productName: string,
  startIndex: number,
  length: number,
): string | null {
  if (
    startIndex < 0 ||
    length <= 0 ||
    startIndex >= productName.length ||
    startIndex + length > productName.length
  ) {
    return null;
  }

  return productName.slice(startIndex, startIndex + length);
}

function formatConfidence(confidence: number): string {
  return `${(confidence * 100).toFixed(1)}%`;
}

function getCardClassName(tone: RecognitionCandidateCardTone): string {
  if (tone === "Accepted") {
    return "border-teal-500/25 bg-teal-500/[0.06]";
  }

  if (tone === "Conflict") {
    return "border-red-500/30 bg-red-500/[0.07]";
  }

  return "border-white/10 bg-white/[0.04]";
}

function getConfidenceClassName(confidence: number): string {
  if (confidence >= 0.98) {
    return "border-green-500/30 bg-green-500/10 text-green-300";
  }

  if (confidence >= 0.85) {
    return "border-amber-500/30 bg-amber-500/10 text-amber-300";
  }

  return "border-red-500/30 bg-red-500/10 text-red-300";
}

function RecognitionSourceBadge({
  source,
}: {
  source: CatalogRecognitionSource;
}) {
  return (
    <span
      className={`rounded-full border px-3 py-1 text-xs font-medium ${getSourceClassName(
        source,
      )}`}
    >
      {getSourceLabel(source)}
    </span>
  );
}

function getSourceLabel(source: CatalogRecognitionSource): string {
  if (source === "Dictionary") {
    return "Словарь";
  }

  if (source === "Rule") {
    return "Правило";
  }

  if (source === "Heuristic") {
    return "Эвристика";
  }

  if (source === "MachineLearning") {
    return "Machine Learning";
  }

  return "Источник не определён";
}

function getSourceClassName(source: CatalogRecognitionSource): string {
  if (source === "Dictionary") {
    return "border-violet-500/30 bg-violet-500/10 text-violet-300";
  }

  if (source === "Rule") {
    return "border-blue-500/30 bg-blue-500/10 text-blue-300";
  }

  if (source === "Heuristic") {
    return "border-amber-500/30 bg-amber-500/10 text-amber-300";
  }

  if (source === "MachineLearning") {
    return "border-fuchsia-500/30 bg-fuchsia-500/10 text-fuchsia-300";
  }

  return "border-slate-500/30 bg-slate-500/10 text-slate-300";
}

function CandidateDetail({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-black/20 p-4">
      <p className="text-xs text-slate-500">{label}</p>

      <p className="mt-2 break-words text-sm text-slate-200">{value}</p>
    </div>
  );
}
