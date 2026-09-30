import type { CatalogImportRowExplanation } from "../../api/getCatalogImportRowExplanations";
import styles from "./CatalogImportRowName.module.css";
import type {
  CatalogImportProductNameExplanationSample,
  CatalogImportProductNameEvidenceSpan,
  CatalogImportRow,
} from "../../model/types";

const tones = {
  Manufacturer: {
    label: "Производитель",
    className: "bg-[var(--app-success-soft)] text-[var(--app-success)]",
  },
  ProductType: {
    label: "Тип товара",
    className: "bg-[var(--app-role-soft)] text-[var(--app-role-text)]",
  },
  Characteristic: {
    label: "Характеристика",
    className:
      "bg-[var(--import-name-characteristic-bg)] text-[var(--import-name-characteristic-text)]",
  },
  Mixed: {
    label: "Несколько объяснений",
    className:
      "bg-[var(--import-name-mixed-bg)] text-[var(--import-name-mixed-text)] underline decoration-double underline-offset-2",
  },
  Unexplained: {
    label: "Без объяснения",
    className:
      "bg-[var(--app-warning-soft)] text-[var(--app-warning)] underline decoration-dotted decoration-1 underline-offset-4",
  },
  Neutral: {
    label: "",
    className: "text-[var(--app-text)]",
  },
};

type Tone = keyof typeof tones;

interface Segment {
  text: string;
  tone: Tone;
  title?: string;
}

function buildSegmentTitle(
  tone: Tone,
  evidence: readonly CatalogImportProductNameEvidenceSpan[],
): string | undefined {
  if (tone === "Neutral") {
    return undefined;
  }

  if (tone === "Unexplained") {
    return [
      "Для этого фрагмента объяснение не найдено.",
      "Это не обязательно ошибка и не указание удалить фрагмент.",
    ].join("\n");
  }

  const descriptions = evidence.map((span) => {
    const label = span.kind === "None" ? "Объяснение" : tones[span.kind].label;

    const lines = [
      `${label}: ${span.targetValue || "значение не указано"}`,
      `Фрагмент: «${span.rawValue}»`,
    ];

    if (span.targetCode) {
      lines.push(`Код: ${span.targetCode}`);
    }

    if (span.source) {
      lines.push(`Источник: ${span.source}`);
    }

    return lines.join("\n");
  });

  const uniqueDescriptions = [...new Set(descriptions)];
  const sections: string[] = [];

  if (uniqueDescriptions.length > 1) {
    sections.push(
      "Для участка найдено несколько объяснений. Это не обязательно конфликт.",
    );
  }

  sections.push(
    ...uniqueDescriptions,
    "Результат распознавания, а не подтверждение сохранённого значения.",
  );

  return sections.join("\n\n");
}

export function buildNameSegments(
  sample: CatalogImportProductNameExplanationSample,
): Segment[] | null {
  const name = sample.productName;
  const spans = [...sample.evidence, ...sample.unexplainedSpans];

  const valid = spans.every(
    (span) =>
      Number.isInteger(span.startIndex) &&
      Number.isInteger(span.length) &&
      span.startIndex >= 0 &&
      span.length > 0 &&
      span.endIndex === span.startIndex + span.length &&
      span.endIndex <= name.length &&
      name.slice(span.startIndex, span.endIndex) === span.rawValue,
  );

  if (
    !valid ||
    sample.evidence.some(
      (span) =>
        !["None", "Manufacturer", "ProductType", "Characteristic"].includes(
          span.kind,
        ),
    )
  ) {
    return null;
  }

  const boundaries = [
    ...new Set([
      0,
      name.length,
      ...spans.flatMap((span) => [span.startIndex, span.endIndex]),
    ]),
  ].sort((left, right) => left - right);

  const segments: Segment[] = [];

  for (let index = 0; index < boundaries.length - 1; index += 1) {
    const start = boundaries[index];
    const end = boundaries[index + 1];

    const activeEvidence = sample.evidence.filter(
      (span) =>
        span.kind !== "None" &&
        span.startIndex <= start &&
        span.endIndex >= end,
    );

    const kinds = [...new Set(activeEvidence.map((span) => span.kind))];

    let tone: Tone = "Neutral";

    if (kinds.length > 1) {
      tone = "Mixed";
    } else if (kinds.length === 1) {
      const kind = kinds[0];

      if (
        kind !== "Manufacturer" &&
        kind !== "ProductType" &&
        kind !== "Characteristic"
      ) {
        return null;
      }

      tone = kind;
    } else if (
      sample.unexplainedSpans.some(
        (span) => span.startIndex <= start && span.endIndex >= end,
      )
    ) {
      tone = "Unexplained";
    }

    const text = name.slice(start, end);
    const title = buildSegmentTitle(tone, activeEvidence);
    const previous = segments[segments.length - 1];

    if (previous?.tone === tone && previous.title === title) {
      previous.text += text;
    } else {
      segments.push({ text, tone, title });
    }
  }

  return segments;
}

const statusMessages: Record<string, string> = {
  NameMissing: "Наименование не задано",
  ProductTypeRequired: "Для объяснения нужен тип товара",
  NameTooLong: "Название слишком длинное для объяснения",
  RecognitionTimedOut: "Время распознавания истекло",
  InvalidEvidence: "Границы объяснения не прошли проверку",
};

export function CatalogImportNameLegend() {
  return (
    <div className={`${styles.palette} flex flex-wrap gap-x-2 gap-y-1 text-xs`}>
      {Object.entries(tones)
        .filter(([key]) => key !== "Neutral")
        .map(([key, tone]) => (
          <span
            key={key}
            className={`rounded px-2 py-0.5 font-medium ${tone.className}`}
          >
            {tone.label}
          </span>
        ))}
    </div>
  );
}

export function CatalogImportRowName({
  row,
  item,
}: {
  row: CatalogImportRow;
  item?: CatalogImportRowExplanation;
}) {
  const name = row.data.name ?? "";

  const matching =
    item &&
    item.rowId === row.rowId &&
    item.rowNumber === row.rowNumber &&
    item.productName === (row.data.name ?? null);

  const sample = matching && item.status === "Ready" ? item.explanation : null;

  const segments =
    sample?.productName === name && sample.rowNumber === row.rowNumber
      ? buildNameSegments(sample)
      : null;

  let note: string | undefined;

  if (item && !matching) {
    note = "Объяснение устарело — обновите подсветку";
  } else if (matching && item.status !== "Ready") {
    note = statusMessages[item.status] ?? "Объяснение недоступно";
  } else if (matching && !segments) {
    note = "Объяснение не прошло проверку";
  } else if (matching && item.hasConflicts) {
    note = "В объяснении есть конфликты";
  }

  return (
    <div className={styles.palette}>
      <p className="whitespace-pre-wrap break-words font-medium leading-6">
        {segments
          ? segments.map((segment, index) => (
              <span
                key={index}
                title={segment.title}
                className={[
                  "rounded-sm",
                  tones[segment.tone].className,
                  segment.title ? "cursor-help" : "",
                ].join(" ")}
              >
                {segment.text}
              </span>
            ))
          : name || "—"}
      </p>

      {note && <p className="mt-1 text-xs text-[var(--app-muted)]">{note}</p>}
    </div>
  );
}
