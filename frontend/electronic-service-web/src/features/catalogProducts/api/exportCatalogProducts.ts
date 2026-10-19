import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";
import type { AdvancedCatalogProductsSearchParams } from "../model/types";

export async function exportCatalogProducts(
  params: AdvancedCatalogProductsSearchParams,
): Promise<string> {
  const response = await httpClient.post<Blob>(
    "/api/catalog/products/export",
    {
      search: params.search,
      productTypeCode: params.productTypeCode,
      manufacturer: params.manufacturer,
      characteristics: params.characteristics ?? [],
      onlyInStock: params.onlyInStock ?? null,
      includeArchived: params.includeArchived ?? false,
      onlyArchived: params.onlyArchived ?? false,
      page: 1,
      pageSize: 100,
    },
    {
      responseType: "blob",
    },
  );

  return downloadFileResponse(response, "catalog-products.xlsx");
}
