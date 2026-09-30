import { httpClient } from "@/shared/api/httpClient";

export interface SetProductTypeCharacteristicRequiredRequest {
  isRequired: boolean;
}

export async function
setProductTypeCharacteristicRequired(
  productTypeCode: string,
  characteristicDefinitionId: string,
  request: SetProductTypeCharacteristicRequiredRequest,
): Promise<void> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  const encodedDefinitionId =
    encodeURIComponent(characteristicDefinitionId);

  await httpClient.put(
    `/api/catalog/product-types/${encodedProductTypeCode}/characteristics/${encodedDefinitionId}/required`,
    request,
  );
}