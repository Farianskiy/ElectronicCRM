import { httpClient } from "@/shared/api/httpClient";

export async function removeCatalogProductAlias(
  productId: string,
  aliasId: string,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);
  const encodedAliasId = encodeURIComponent(aliasId);

  await httpClient.delete(
    `/api/catalog/products/${encodedProductId}/aliases/${encodedAliasId}`,
  );
}