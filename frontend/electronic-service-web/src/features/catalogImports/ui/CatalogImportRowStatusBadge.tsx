import { getCatalogImportRowStatusLabel } from "../model/catalogImportRowStatus";
import type { CatalogImportRowStatus } from "../model/types";

interface CatalogImportRowStatusBadgeProps {
  status: CatalogImportRowStatus;
}

const statusClassNames: Record<CatalogImportRowStatus, string> = {
  None: "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)]",

  PendingMapping:
    "border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] text-[var(--app-warning)]",

  Valid:
    "border-[var(--app-success-border)] bg-[var(--app-success-soft)] text-[var(--app-success)]",

  Error:
    "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]",
};

export function CatalogImportRowStatusBadge({
  status,
}: CatalogImportRowStatusBadgeProps) {
  return (
    <span
      className={[
        "inline-flex whitespace-nowrap rounded-full border",
        "px-3 py-1 text-xs font-medium",
        statusClassNames[status],
      ].join(" ")}
    >
      {getCatalogImportRowStatusLabel(status)}
    </span>
  );
}
