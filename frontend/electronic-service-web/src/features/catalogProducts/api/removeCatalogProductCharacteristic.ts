import { httpClient } from "@/shared/api/httpClient";

export async function removeCatalogProductCharacteristic(
  productId: string,
  characteristicCode: string,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);
  const encodedCharacteristicCode = encodeURIComponent(
    characteristicCode,
  );

  await httpClient.delete(
    `/api/catalog/products/${encodedProductId}/characteristics/${encodedCharacteristicCode}`,
  );
}