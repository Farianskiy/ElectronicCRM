import { httpClient } from "@/shared/api/httpClient";

export interface UpdateCatalogProductGeneralInformationRequest {
  name: string;
  article: string;
  manufacturerId: string;
}

export async function updateCatalogProductGeneralInformation(
  productId: string,
  request: UpdateCatalogProductGeneralInformationRequest,
): Promise<void> {
  const encodedProductId = encodeURIComponent(productId);

  await httpClient.put(
    `/api/catalog/products/${encodedProductId}/general-information`,
    request,
  );
}