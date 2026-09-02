import type {
  CatalogRecognitionConflict,
  CatalogRecognizedCharacteristic,
} from "../model/types";

interface RecognitionHighlightedProductNameProps {
  productName: string;
  characteristics: CatalogRecognizedCharacteristic[];
  conflicts: CatalogRecognitionConflict[];
}

type HighlightKind = "Plain" | "Recognized" | "Conflict";

interface HighlightOwner {
  characteristicCode: string;
  kind: Exclude<HighlightKind, "Plain">;
}

interface HighlightSegment {
  text: string;
  kind: HighlightKind;
  characteristicCodes: string[];
}

export function RecognitionHighlightedProductName({
  productName,
  characteristics,
  conflicts,
}: RecognitionHighlightedProductNameProps) {
  const segments = buildHighlightSegments(
    productName,
    characteristics,
    conflicts,
  );

  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <div>
        <h2 className="text-xl font-semibold text-white">
          Разбор исходного наименования
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Зелёным отмечены принятые результаты. Красным отмечены участки, по
          которым движок обнаружил неразрешённый конфликт.
        </p>
      </div>

      <div className="mt-5 rounded-2xl border border-white/10 bg-black/30 p-5">
        <p className="whitespace-pre-wrap break-words font-mono text-lg leading-9 text-slate-200">
          {segments.map((segment, index) => (
            <span
              key={`${index}-${segment.kind}-${segment.text}`}
              title={
                segment.characteristicCodes.length > 0
                  ? segment.characteristicCodes.join(", ")
                  : undefined
              }
              className={getHighlightSegmentClassName(segment.kind)}
            >
              {segment.text}
            </span>
          ))}
        </p>
      </div>

      <div className="mt-4 flex flex-wrap gap-3 text-xs">
        <HighlightLegend
          colorClassName="bg-teal-400"
          label="Распознано и принято"
        />

        <HighlightLegend
          colorClassName="bg-red-400"
          label="Неразрешённый конфликт"
        />

        <HighlightLegend
          colorClassName="bg-slate-600"
          label="Не использовано движком"
        />
      </div>
    </section>
  );
}

function buildHighlightSegments(
  productName: string,
  characteristics: CatalogRecognizedCharacteristic[],
  conflicts: CatalogRecognitionConflict[],
): HighlightSegment[] {
  if (productName.length === 0) {
    return [];
  }

  const ownersByCharacter = Array.from(
    { length: productName.length },
    (): HighlightOwner[] => [],
  );

  for (const characteristic of characteristics) {
    addOwnerToCharacters(
      ownersByCharacter,
      productName.length,
      characteristic,
      "Recognized",
    );
  }

  for (const conflict of conflicts) {
    for (const candidate of conflict.candidates) {
      addOwnerToCharacters(
        ownersByCharacter,
        productName.length,
        candidate,
        "Conflict",
      );
    }
  }

  const segments: HighlightSegment[] = [];

  for (let index = 0; index < productName.length; index++) {
    const owners = ownersByCharacter[index] ?? [];
    const kind = getCharacterHighlightKind(owners);
    const characteristicCodes = getOwnerCharacteristicCodes(owners);
    const character = productName[index] ?? "";
    const previousSegment = segments.at(-1);

    if (
      previousSegment &&
      previousSegment.kind === kind &&
      arraysEqual(previousSegment.characteristicCodes, characteristicCodes)
    ) {
      previousSegment.text += character;
      continue;
    }

    segments.push({
      text: character,
      kind,
      characteristicCodes,
    });
  }

  return segments;
}

function addOwnerToCharacters(
  ownersByCharacter: HighlightOwner[][],
  productNameLength: number,
  characteristic: CatalogRecognizedCharacteristic,
  kind: Exclude<HighlightKind, "Plain">,
): void {
  if (
    characteristic.startIndex < 0 ||
    characteristic.length <= 0 ||
    characteristic.startIndex >= productNameLength
  ) {
    return;
  }

  const endIndex = Math.min(
    characteristic.startIndex + characteristic.length,
    productNameLength,
  );

  for (let index = characteristic.startIndex; index < endIndex; index++) {
    const owners = ownersByCharacter[index];

    if (!owners) {
      continue;
    }

    const alreadyAdded = owners.some(
      (owner) =>
        owner.characteristicCode === characteristic.characteristicCode &&
        owner.kind === kind,
    );

    if (alreadyAdded) {
      continue;
    }

    owners.push({
      characteristicCode: characteristic.characteristicCode,
      kind,
    });
  }
}

function getCharacterHighlightKind(owners: HighlightOwner[]): HighlightKind {
  if (owners.some((owner) => owner.kind === "Conflict")) {
    return "Conflict";
  }

  if (owners.some((owner) => owner.kind === "Recognized")) {
    return "Recognized";
  }

  return "Plain";
}

function getOwnerCharacteristicCodes(owners: HighlightOwner[]): string[] {
  return Array.from(
    new Set(owners.map((owner) => owner.characteristicCode)),
  ).sort((left, right) => left.localeCompare(right));
}

function arraysEqual(left: string[], right: string[]): boolean {
  if (left.length !== right.length) {
    return false;
  }

  return left.every((value, index) => value === right[index]);
}

function getHighlightSegmentClassName(kind: HighlightKind): string {
  if (kind === "Conflict") {
    return "rounded bg-red-500/30 px-0.5 text-red-100 underline decoration-red-400 decoration-2 underline-offset-4";
  }

  if (kind === "Recognized") {
    return "rounded bg-teal-500/30 px-0.5 text-teal-100 underline decoration-teal-400 decoration-2 underline-offset-4";
  }

  return "text-slate-400";
}

function HighlightLegend({
  colorClassName,
  label,
}: {
  colorClassName: string;
  label: string;
}) {
  return (
    <div className="flex items-center gap-2 text-slate-400">
      <span className={`h-2.5 w-2.5 rounded-full ${colorClassName}`} />

      <span>{label}</span>
    </div>
  );
}
