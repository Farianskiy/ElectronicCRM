import type {
  CatalogProductTypeCharacteristicMetadata,
} from "@/features/catalogMetadata/model/types";
import { AppInput } from "@/shared/ui/AppInput";
import { AppSelect } from "@/shared/ui/AppSelect";
import type { CatalogImportCharacteristicOrigin } from "../../model/types";

interface CatalogImportCharacteristicGridProps {
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  values: Record<string, string>;
  origins: Record<string, CatalogImportCharacteristicOrigin>;
  disabled: boolean;
  onChange: (characteristicId: string, value: string) => void;
}

function getSourcePresentation(origin?: CatalogImportCharacteristicOrigin): {
  label: string;
  className: string;
} {
  switch (origin?.source) {
    case "Excel":
      return {
        label: "Из Excel",
        className: "bg-sky-500/10 text-sky-200",
      };
    case "Recognition":
      return {
        label: "Из названия",
        className: "bg-emerald-500/10 text-emerald-200",
      };
    case "Manual":
      return {
        label: "Вручную",
        className: "bg-violet-500/10 text-violet-200",
      };
    case "CatalogAnalogy":
      return {
        label: "По аналогам",
        className: "bg-amber-500/10 text-amber-200",
      };
    case "TrainingExample":
      return {
        label: "По примерам",
        className: "bg-cyan-500/10 text-cyan-200",
      };
    default:
      return {
        label: "Не определено",
        className: "bg-[var(--app-surface)] text-[var(--app-muted)]",
      };
  }
}

function CharacteristicValueInput({
  characteristic,
  value,
  disabled,
  onChange,
}: {
  characteristic: CatalogProductTypeCharacteristicMetadata;
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  const ariaLabel = characteristic.unit
    ? `${characteristic.name}, ${characteristic.unit}`
    : characteristic.name;

  if (characteristic.dataType === "Boolean") {
    return (
      <AppSelect
        ariaLabel={ariaLabel}
        value={value}
        disabled={disabled}
        onChange={onChange}
        options={[
          { value: "", label: "Не указано" },
          { value: "true", label: "Да" },
          { value: "false", label: "Нет" },
        ]}
      />
    );
  }

  return (
    <AppInput
      type="text"
      inputMode={characteristic.dataType === "Number" ? "decimal" : "text"}
      value={value}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
      placeholder={
        characteristic.dataType === "Number"
          ? "Введите число"
          : "Введите значение"
      }
      aria-label={ariaLabel}
    />
  );
}

function CharacteristicOriginDetails({
  origin,
}: {
  origin?: CatalogImportCharacteristicOrigin;
}) {
  if (!origin) {
    return null;
  }

  const details: string[] = [];
  if (origin.rawValue?.trim()) {
    details.push(`Фрагмент: «${origin.rawValue.trim()}»`);
  }
  if (origin.confidence != null) {
    details.push(`Уверенность: ${Math.round(origin.confidence * 100)}%`);
  }
  if (origin.analogyFamilyAnchor) {
    details.push(`Семейство: ${origin.analogyFamilyAnchor}`);
  }
  if (origin.source === "CatalogAnalogy") {
    details.push(
      `Поддержка: ${
        origin.analogySupportingProductCount ??
        origin.analogySupportingProductIds.length
      }`,
    );
  }
  if (origin.source === "TrainingExample") {
    details.push(
      `Подтверждённых примеров: ${origin.trainingSourceRowIds?.length ?? 0}`,
    );
  }

  if (details.length === 0) {
    return null;
  }

  return (
    <details className="mt-2 text-xs text-[var(--app-muted)]">
      <summary className="cursor-pointer select-none">Как получено</summary>
      <div className="mt-2 grid gap-1 rounded-lg bg-black/10 p-2">
        {details.map((detail) => (
          <p key={detail}>{detail}</p>
        ))}
        {origin.source === "CatalogAnalogy" && (
          <p className="text-[var(--app-warning)]">
            При повторном анализе значение будет рассчитано заново.
          </p>
        )}
        {origin.source === "TrainingExample" && (
          <p className="text-cyan-200">
            Значение перенесено из подтверждённых примеров этого импорта.
          </p>
        )}
      </div>
    </details>
  );
}

export function CatalogImportCharacteristicGrid({
  characteristics,
  values,
  origins,
  disabled,
  onChange,
}: CatalogImportCharacteristicGridProps) {
  return (
    <div className="mt-4 overflow-hidden rounded-xl border border-[var(--app-border)]">
      <div className="hidden grid-cols-[minmax(170px,0.8fr)_minmax(220px,1fr)_auto] gap-3 bg-[var(--app-surface)] px-4 py-2 text-xs font-medium text-[var(--app-muted)] md:grid">
        <span>Характеристика</span>
        <span>Значение</span>
        <span>Источник</span>
      </div>

      {characteristics.map((characteristic) => {
        const origin = origins[characteristic.id];
        const source = getSourcePresentation(origin);

        return (
          <div
            key={characteristic.id}
            className="grid gap-3 border-t border-[var(--app-border)] p-4 first:border-t-0 md:grid-cols-[minmax(170px,0.8fr)_minmax(220px,1fr)_auto] md:items-start"
          >
            <div className="min-w-0">
              <p className="text-sm font-medium text-[var(--app-text)]">
                {characteristic.name}
                {characteristic.unit ? `, ${characteristic.unit}` : ""}
              </p>
              {characteristic.isRequired && (
                <p className="mt-1 text-xs text-[var(--app-warning)]">
                  Обязательная
                </p>
              )}
              <CharacteristicOriginDetails origin={origin} />
            </div>

            <CharacteristicValueInput
              characteristic={characteristic}
              value={values[characteristic.id] ?? ""}
              disabled={disabled}
              onChange={(value) => onChange(characteristic.id, value)}
            />

            <span
              className={`w-fit rounded-full px-2.5 py-1 text-xs font-medium ${source.className}`}
            >
              {source.label}
            </span>
          </div>
        );
      })}
    </div>
  );
}
