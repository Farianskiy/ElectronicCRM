import type {
  AdvancedCatalogProductsSearchParams,
  SearchProductCharacteristicRequest,
} from "./types";

export type BulkEditorFilters = Omit<
  AdvancedCatalogProductsSearchParams,
  "page" | "pageSize"
>;

export function createCatalogProductBulkEditorHref(
  filters: BulkEditorFilters,
): string {
  const params = new URLSearchParams();

  if (filters.search) params.set("search", filters.search);
  if (filters.productTypeCode) {
    params.set("productTypeCode", filters.productTypeCode);
  }
  if (filters.manufacturer) params.set("manufacturer", filters.manufacturer);
  if (filters.onlyInStock !== null && filters.onlyInStock !== undefined) {
    params.set("onlyInStock", String(filters.onlyInStock));
  }
  if (filters.characteristics?.length) {
    params.set("characteristics", JSON.stringify(filters.characteristics));
  }

  return `/catalog/products/bulk-edit?${params.toString()}`;
}

export function parseCatalogProductBulkEditorFilters(
  searchParams: URLSearchParams,
): BulkEditorFilters {
  let characteristics: SearchProductCharacteristicRequest[] = [];
  const serializedCharacteristics = searchParams.get("characteristics");

  if (serializedCharacteristics) {
    try {
      const parsed: unknown = JSON.parse(serializedCharacteristics);

      if (Array.isArray(parsed)) {
        characteristics = parsed.filter(
          (item): item is SearchProductCharacteristicRequest =>
            Boolean(item) &&
            typeof item === "object" &&
            typeof (item as SearchProductCharacteristicRequest).code ===
              "string" &&
            typeof (item as SearchProductCharacteristicRequest).value ===
              "string",
        );
      }
    } catch {
      characteristics = [];
    }
  }

  const onlyInStock = searchParams.get("onlyInStock");

  return {
    search: searchParams.get("search"),
    productTypeCode: searchParams.get("productTypeCode"),
    manufacturer: searchParams.get("manufacturer"),
    characteristics,
    onlyInStock:
      onlyInStock === "true"
        ? true
        : onlyInStock === "false"
          ? false
          : null,
  };
}

export function hasCatalogProductBulkEditorFilters(
  filters: BulkEditorFilters,
): boolean {
  return Boolean(
    filters.search ||
      filters.productTypeCode ||
      filters.manufacturer ||
      filters.characteristics?.length ||
      filters.onlyInStock !== null,
  );
}
