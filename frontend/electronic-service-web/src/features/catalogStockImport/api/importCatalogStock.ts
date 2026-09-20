import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogStockImportRequest,
  CatalogStockImportResponse,
} from "../model/types";

export async function importCatalogStock(
  request: CatalogStockImportRequest,
): Promise<CatalogStockImportResponse> {
  const formData = new FormData();

  formData.append("manufacturerId", request.manufacturerId);
  formData.append("file", request.file);

  const response = await httpClient.post<CatalogStockImportResponse>(
    "/api/catalog/products/stock-import",
    formData,
  );

  return response.data;
}
