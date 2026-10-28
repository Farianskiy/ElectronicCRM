import type { CatalogImportRowExplanation } from "../../api/getCatalogImportRowExplanations";
import type { CatalogImportRow } from "../../model/types";
import { CatalogImportRowStatusBadge } from "../CatalogImportRowStatusBadge";
import { CatalogImportRowName } from "./CatalogImportRowName";

interface CatalogImportRowListItemProps {
  row: CatalogImportRow;
  explanation?: CatalogImportRowExplanation;
  productTypeName: string;
  productTypeResolutionLabel?: string | null;
  isActive: boolean;
  isSelected: boolean;
  selectionDisabled: boolean;
  onOpen: () => void;
  onToggleSelected: () => void;
}

function formatMetaValue(value: string | null | undefined): string {
  const normalized = value?.trim();
  return normalized ? normalized : "Не указано";
}

export function CatalogImportRowListItem({
  row,
  explanation,
  productTypeName,
  productTypeResolutionLabel,
  isActive,
  isSelected,
  selectionDisabled,
  onOpen,
  onToggleSelected,
}: CatalogImportRowListItemProps) {
  const errorCount = row.issues.length;
  const warningCount = row.warnings.length;
  const characteristics = Object.values(row.data.characteristics)
    .map((value) => value.trim())
    .filter((value) => value.length > 0)
    .slice(0, 3);

  return (
    <article
      className={[
        "grid grid-cols-[auto_minmax(0,1fr)] gap-3 border-b border-[var(--app-border)] p-3 transition-colors last:border-b-0",
        isActive
          ? "bg-[var(--app-accent-soft)] shadow-[inset_3px_0_0_var(--app-accent)]"
          : "bg-[var(--app-panel)] hover:bg-[var(--app-panel-hover)]",
      ].join(" ")}
    >
      <div className="pt-1">
        <input
          type="checkbox"
          checked={isSelected}
          disabled={selectionDisabled}
          onChange={onToggleSelected}
          onClick={(event) => event.stopPropagation()}
          aria-label={`Выбрать строку ${row.rowNumber}`}
          className="h-4 w-4 cursor-pointer accent-[var(--app-accent-strong)] disabled:cursor-not-allowed disabled:opacity-40"
        />
      </div>

      <button
        type="button"
        onClick={onOpen}
        aria-current={isActive ? "true" : undefined}
        className="min-w-0 text-left focus-visible:rounded-xl focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--app-accent)]"
      >
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <span className="text-xs font-semibold text-[var(--app-muted)]">
              Строка {row.rowNumber}
            </span>
            <CatalogImportRowStatusBadge status={row.status} />
          </div>

          {(errorCount > 0 || warningCount > 0) && (
            <span
              className={[
                "rounded-full px-2 py-1 text-xs font-semibold",
                errorCount > 0
                  ? "bg-[var(--app-danger-soft)] text-[var(--app-danger)]"
                  : "bg-[var(--app-warning-soft)] text-[var(--app-warning)]",
              ].join(" ")}
            >
              {errorCount > 0
                ? `${errorCount} ${errorCount === 1 ? "ошибка" : "ошибок"}`
                : `${warningCount} ${warningCount === 1 ? "предупреждение" : "предупреждений"}`}
            </span>
          )}
        </div>

        <div className="mt-2">
          <CatalogImportRowName row={row} item={explanation} />
        </div>

        <p className="mt-2 truncate text-xs text-[var(--app-muted)]">
          {formatMetaValue(row.data.article)} · {formatMetaValue(row.data.manufacturer)}
        </p>

        <div className="mt-2 flex flex-wrap items-center gap-2">
          <span className="rounded-lg border border-[var(--app-border)] bg-[var(--app-surface)] px-2 py-1 text-xs font-medium text-[var(--app-text)]">
            {productTypeName}
          </span>

          {characteristics.map((value, index) => (
            <span
              key={`${value}-${index}`}
              className="max-w-44 truncate rounded-lg bg-[var(--app-panel-strong)] px-2 py-1 text-xs text-[var(--app-muted)]"
              title={value}
            >
              {value}
            </span>
          ))}
        </div>

        {productTypeResolutionLabel && (
          <p className="mt-2 text-xs text-[var(--app-muted)]">
            {productTypeResolutionLabel}
          </p>
        )}

        {errorCount > 0 && row.issues[0] && (
          <p className="mt-2 line-clamp-2 text-xs leading-5 text-[var(--app-danger)]">
            {row.issues[0].message}
          </p>
        )}

        {errorCount === 0 && warningCount > 0 && row.warnings[0] && (
          <p className="mt-2 line-clamp-2 text-xs leading-5 text-[var(--app-warning)]">
            {row.warnings[0].message}
          </p>
        )}
      </button>
    </article>
  );
}
