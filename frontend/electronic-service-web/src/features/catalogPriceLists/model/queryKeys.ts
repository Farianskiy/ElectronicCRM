import type {
  CatalogPriceListRowMatchStatus,
  CatalogPriceListRowStatus,
  CatalogPriceListStatus,
} from "./types";

const root = ["catalog-price-lists"] as const;

const versionsRoot = [...root, "versions"] as const;

export const catalogPriceListQueryKeys = {
  root,

  versionsRoot,

  versions: (
    manufacturerId: string,
    status: CatalogPriceListStatus | null,
    page: number,
    pageSize: number,
  ) =>
    [
      ...versionsRoot,
      manufacturerId,
      status,
      page,
      pageSize,
    ] as const,

  details: (priceListId: string) =>
    [...root, "details", priceListId] as const,

  rowsRoot: (priceListId: string) =>
    [...root, "rows", priceListId] as const,

  rows: (
    priceListId: string,
    status: CatalogPriceListRowStatus | null,
    matchStatus: CatalogPriceListRowMatchStatus | null,
    issueCode: string,
    page: number,
    pageSize: number,
  ) =>
    [
      ...root,
      "rows",
      priceListId,
      status,
      matchStatus,
      issueCode,
      page,
      pageSize,
    ] as const,

  issueGroupsRoot: (priceListId: string) =>
  [...root, "issue-groups", priceListId] as const,

issueGroups: (
  priceListId: string,
  issueCode: string,
  page: number,
  pageSize: number,
) =>
  [
    ...root,
    "issue-groups",
    priceListId,
    issueCode,
    page,
    pageSize,
  ] as const,

  productsRoot: (priceListId: string) =>
    [...root, "products", priceListId] as const,

  products: (
    priceListId: string,
    search: string,
    page: number,
    pageSize: number,
  ) =>
    [
      ...root,
      "products",
      priceListId,
      search,
      page,
      pageSize,
    ] as const,
};