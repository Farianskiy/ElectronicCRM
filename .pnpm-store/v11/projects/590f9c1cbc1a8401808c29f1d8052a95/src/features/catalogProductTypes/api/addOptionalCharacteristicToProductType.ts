import { httpClient } from "@/shared/api/httpClient";

export interface AddOptionalCharacteristicToProductTypeRequest {
  characteristicDefinitionId: string;
}

export async function addOptionalCharacteristicToProductType(
  productTypeCode: string,
  request: AddOptionalCharacteristicToProductTypeRequest,
): Promise<void> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  await httpClient.post(
    `/api/catalog/product-types/${encodedProductTypeCode}/characteristics`,
    request,
  );
}