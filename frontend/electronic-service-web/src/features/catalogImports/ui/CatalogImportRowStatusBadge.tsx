import { getCatalogImportRowStatusLabel } from "../model/catalogImportRowStatus";
import type { CatalogImportRowStatus } from "../model/types";

interface CatalogImportRowStatusBadgeProps {
  status: CatalogImportRowStatus;
}

const statusClassNames: Record<CatalogImportRowStatus, string> = {
  None: "border-slate-500/30 bg-slate-500/10 text-slate-300",
  PendingMapping: "border-amber-500/30 bg-amber-500/10 text-amber-200",
  Valid: "border-green-500/30 bg-green-500/10 text-green-200",
  Error: "border-red-500/30 bg-red-500/10 text-red-200",
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
