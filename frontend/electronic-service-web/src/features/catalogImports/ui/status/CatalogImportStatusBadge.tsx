import { getCatalogImportStatusLabel } from "../../model/catalogImportStatus";
import type { CatalogImportBatchStatus } from "../../model/types";

interface CatalogImportStatusBadgeProps {
  status: CatalogImportBatchStatus;
}

const statusClassNames: Record<CatalogImportBatchStatus, string> = {
  Uploaded: [
    "border-slate-500/30 bg-slate-500/10 text-slate-200",
    "[[data-theme=light]_&]:border-slate-200 [[data-theme=light]_&]:bg-slate-50 [[data-theme=light]_&]:text-slate-700",
  ].join(" "),
  MappingRequired: [
    "border-amber-500/30 bg-amber-500/10 text-amber-200",
    "[[data-theme=light]_&]:border-amber-200 [[data-theme=light]_&]:bg-amber-50 [[data-theme=light]_&]:text-amber-700",
  ].join(" "),
  NeedsCorrection: [
    "border-red-500/30 bg-red-500/10 text-red-200",
    "[[data-theme=light]_&]:border-red-200 [[data-theme=light]_&]:bg-red-50 [[data-theme=light]_&]:text-red-700",
  ].join(" "),
  Ready: [
    "border-teal-500/30 bg-teal-500/10 text-teal-200",
    "[[data-theme=light]_&]:border-teal-200 [[data-theme=light]_&]:bg-teal-50 [[data-theme=light]_&]:text-teal-700",
  ].join(" "),
  Submitted: [
    "border-blue-500/30 bg-blue-500/10 text-blue-200",
    "[[data-theme=light]_&]:border-blue-200 [[data-theme=light]_&]:bg-blue-50 [[data-theme=light]_&]:text-blue-700",
  ].join(" "),
  UnderReview: [
    "border-violet-500/30 bg-violet-500/10 text-violet-200",
    "[[data-theme=light]_&]:border-violet-200 [[data-theme=light]_&]:bg-violet-50 [[data-theme=light]_&]:text-violet-700",
  ].join(" "),
  Applying: [
    "border-cyan-500/30 bg-cyan-500/10 text-cyan-200",
    "[[data-theme=light]_&]:border-cyan-200 [[data-theme=light]_&]:bg-cyan-50 [[data-theme=light]_&]:text-cyan-700",
  ].join(" "),
  Applied: [
    "border-green-500/30 bg-green-500/10 text-green-200",
    "[[data-theme=light]_&]:border-green-200 [[data-theme=light]_&]:bg-green-50 [[data-theme=light]_&]:text-green-700",
  ].join(" "),
  Rejected: [
    "border-rose-500/30 bg-rose-500/10 text-rose-200",
    "[[data-theme=light]_&]:border-rose-200 [[data-theme=light]_&]:bg-rose-50 [[data-theme=light]_&]:text-rose-700",
  ].join(" "),
  Failed: [
    "border-red-500/30 bg-red-500/10 text-red-200",
    "[[data-theme=light]_&]:border-red-200 [[data-theme=light]_&]:bg-red-50 [[data-theme=light]_&]:text-red-700",
  ].join(" "),
  ChangesRequested: [
    "border-orange-500/30 bg-orange-500/10 text-orange-200",
    "[[data-theme=light]_&]:border-orange-200 [[data-theme=light]_&]:bg-orange-50 [[data-theme=light]_&]:text-orange-700",
  ].join(" "),
};

export function CatalogImportStatusBadge({
  status,
}: CatalogImportStatusBadgeProps) {
  return (
    <span
      className={[
        "inline-flex max-w-full items-center rounded-full border px-3 py-1",
        "text-xs font-medium leading-5 [overflow-wrap:anywhere]",
        statusClassNames[status],
      ].join(" ")}
    >
      {getCatalogImportStatusLabel(status)}
    </span>
  );
}
