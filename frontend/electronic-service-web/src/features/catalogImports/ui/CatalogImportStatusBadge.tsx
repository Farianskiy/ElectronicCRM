import { getCatalogImportStatusLabel } from "../model/catalogImportStatus";
import type { CatalogImportBatchStatus } from "../model/types";

interface CatalogImportStatusBadgeProps {
  status: CatalogImportBatchStatus;
}

const statusClassNames: Record<CatalogImportBatchStatus, string> = {
  Uploaded: "border-slate-500/30 bg-slate-500/10 text-slate-200",
  MappingRequired: "border-amber-500/30 bg-amber-500/10 text-amber-200",
  NeedsCorrection: "border-red-500/30 bg-red-500/10 text-red-200",
  Ready: "border-teal-500/30 bg-teal-500/10 text-teal-200",
  Submitted: "border-blue-500/30 bg-blue-500/10 text-blue-200",
  UnderReview: "border-violet-500/30 bg-violet-500/10 text-violet-200",
  Applying: "border-cyan-500/30 bg-cyan-500/10 text-cyan-200",
  Applied: "border-green-500/30 bg-green-500/10 text-green-200",
  Rejected: "border-rose-500/30 bg-rose-500/10 text-rose-200",
  Failed: "border-red-500/30 bg-red-500/10 text-red-200",
  ChangesRequested: "border-orange-500/30 bg-orange-500/10 text-orange-200",
};

export function CatalogImportStatusBadge({
  status,
}: CatalogImportStatusBadgeProps) {
  return (
    <span
      className={[
        "inline-flex rounded-full border px-3 py-1",
        "text-xs font-medium",
        statusClassNames[status],
      ].join(" ")}
    >
      {getCatalogImportStatusLabel(status)}
    </span>
  );
}
