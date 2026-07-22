import { httpClient } from "@/shared/api/httpClient";
import type { ProductAuditHistoryPage } from "../model/types";

export async function getCatalogProductAuditHistory(
  productId: string,
  pageNumber: number,
  pageSize: number,
): Promise<ProductAuditHistoryPage> {
  const encodedProductId =
    encodeURIComponent(productId);

  const response =
    await httpClient.get<ProductAuditHistoryPage>(
      `/api/catalog/products/${encodedProductId}/audit-history`,
      {
        params: {
          pageNumber,
          pageSize,
        },
      },
    );

  return response.data;
}