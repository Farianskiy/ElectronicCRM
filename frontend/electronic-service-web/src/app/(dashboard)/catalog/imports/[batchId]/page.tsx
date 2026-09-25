"use client";

import { useParams, useSearchParams } from "next/navigation";
import { CatalogImportDetailsScreen } from "@/features/catalogImports";

export default function CatalogImportDetailsPage() {
  const params = useParams<{ batchId: string }>();
  const searchParams = useSearchParams();

  const fromReviewQueue = searchParams.get("from") === "review-queue";

  const requestedReturn = searchParams.get("learningReturn");
  const learningReturn = requestedReturn?.startsWith(
    "/catalog/recognition/learning?",
  )
    ? requestedReturn
    : null;
  const backHref =
    learningReturn ??
    (fromReviewQueue ? "/catalog/import-reviews" : "/catalog/imports");

  const backLabel = learningReturn
    ? "Назад к обучению выбранных товаров"
    : fromReviewQueue
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
