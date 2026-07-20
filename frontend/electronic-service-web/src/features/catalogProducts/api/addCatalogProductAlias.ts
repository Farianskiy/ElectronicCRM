import { httpClient } from "@/shared/api/httpClient";

export interface AddCatalogProductAliasRequest {
  alias: string;
}

export async function addCatalogProductAlias(
  productId: string,
  request: AddCatalogProductAliasRequest,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);

  await httpClient.post(
    `/api/catalog/products/${encodedProductId}/aliases`,
    request,
  );
}