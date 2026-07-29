import type {
  CatalogImportBatchStatus,
  CatalogImportReviewQueueStatus,
  CatalogImportRowFilterStatus,
} from "./types";

const root = ["catalog-import-batches"] as const;

const myRoot = [...root, "my"] as const;

const reviewQueueRoot = [
  ...root,
  "review-queue",
] as const;

export const catalogImportQueryKeys = {
  root,

  myRoot,

  my: (
    status: CatalogImportBatchStatus | null,
    page: number,
    pageSize: number,
  ) =>
    [
      ...myRoot,
      status,
      page,
      pageSize,
    ] as const,

  details: (batchId: string) =>
    [
      ...root,
      "details",
      batchId,
    ] as const,

  mapping: (batchId: string) =>
    [
      ...root,
      "mapping",
      batchId,
    ] as const,

  rowsRoot: (batchId: string) =>
    [
      ...root,
      "rows",
      batchId,
    ] as const,

  rows: (
    batchId: string,
    status: CatalogImportRowFilterStatus | null,
    page: number,
    pageSize: number,
  ) =>
    [
      ...root,
      "rows",
      batchId,
      status,
      page,
      pageSize,
    ] as const,

  reviewQueueRoot,

  reviewQueue: (
    status: CatalogImportReviewQueueStatus | null,
    page: number,
    pageSize: number,
  ) =>
    [
      ...reviewQueueRoot,
      status,
      page,
      pageSize,
    ] as const,

  history: (batchId: string) =>
    [
      ...root,
      "history",
      batchId,
    ] as const,

  appliedProductsRoot: (batchId: string) =>
    [
      ...root,
      "applied-products",
      batchId,
    ] as const,

  appliedProducts: (
    batchId: string,
    page: number,
    pageSize: number,
  ) =>
    [
      ...root,
      "applied-products",
      batchId,
      page,
      pageSize,
    ] as const,
};