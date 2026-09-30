import { httpClient } from "@/shared/api/httpClient";

export async function removeCharacteristicFromProductType(
  productTypeCode: string,
  characteristicDefinitionId: string,
): Promise<void> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  const encodedDefinitionId =
    encodeURIComponent(characteristicDefinitionId);

  await httpClient.delete(
    `/api/catalog/product-types/${encodedProductTypeCode}/characteristics/${encodedDefinitionId}`,
  );
}