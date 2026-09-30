import { httpClient } from "@/shared/api/httpClient";
import type {
  BulkUpdateCatalogProductRequest,
  BulkUpdateCatalogProductsResponse,
} from "../model/types";

export async function bulkUpdateCatalogProducts(
  rows: BulkUpdateCatalogProductRequest[],
): Promise<BulkUpdateCatalogProductsResponse> {
  const response = await httpClient.put<BulkUpdateCatalogProductsResponse>(
    "/api/catalog/products/bulk",
    { rows },
  );

  return response.data;
}
