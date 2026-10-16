import { httpClient } from "@/shared/api/httpClient";

export async function archiveCatalogProduct(
  productId: string,
  reason?: string | null,
): Promise<void> {
  await httpClient.post(`/api/catalog/products/${productId}/archive`, {
    reason: reason?.trim() || null,
  });
}
