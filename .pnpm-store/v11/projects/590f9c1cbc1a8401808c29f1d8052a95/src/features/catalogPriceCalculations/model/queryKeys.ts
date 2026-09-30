import type { CatalogPriceCalculationStatus } from "./types";

const root = ["catalog-price-calculations"] as const;

const listRoot = [...root, "list"] as const;

export const catalogPriceCalculationQueryKeys = {
  root,

  listRoot,

  list: (
    status: CatalogPriceCalculationStatus | null,
    page: number,
    pageSize: number,
  ) => [...listRoot, status, page, pageSize] as const,

  details: (calculationId: string) =>
    [...root, "details", calculationId] as const,

  productsRoot: (calculationId: string) =>
    [...root, "products", calculationId] as const,

  products: (
    calculationId: string,
    search: string,
    page: number,
    pageSize: number,
  ) =>
    [
      ...root,
      "products",
      calculationId,
      search,
      page,
      pageSize,
    ] as const,
};