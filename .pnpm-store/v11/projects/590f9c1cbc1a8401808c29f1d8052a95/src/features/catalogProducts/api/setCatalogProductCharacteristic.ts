import { httpClient } from "@/shared/api/httpClient";

export interface SetCatalogProductCharacteristicRequest {
  code: string;
  value: string;
}

export async function setCatalogProductCharacteristic(
  productId: string,
  request: SetCatalogProductCharacteristicRequest,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);

  await httpClient.post(
    `/api/catalog/products/${encodedProductId}/characteristics`,
    request,
  );
}