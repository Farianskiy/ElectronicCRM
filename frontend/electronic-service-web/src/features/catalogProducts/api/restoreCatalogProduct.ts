import { httpClient } from "@/shared/api/httpClient";

export async function restoreCatalogProduct(productId: string): Promise<void> {
  await httpClient.post(`/api/catalog/products/${productId}/restore`);
}
