"use client";

import { useParams, useSearchParams } from "next/navigation";
import { CatalogImportDetailsScreen } from "@/features/catalogImports/ui/details";

export default function CatalogImportDetailsPage() {
  const params = useParams<{ batchId: string }>();
  const searchParams = useSearchParams();

  const fromReviewQueue = searchParams.get("from") === "review-queue";

  const backHref = fromReviewQueue
    ? "/catalog/import-reviews"
    : "/catalog/imports";

  const backLabel = fromReviewQueue
    ? "Назад к очереди проверки"
    : "Назад к импортам";

  return (
    <CatalogImportDetailsScreen
      batchId={params.batchId ?? ""}
      backHref={backHref}
      backLabel={backLabel}
    />
  );
}
