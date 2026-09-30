import { httpClient } from "@/shared/api/httpClient";
import type { CatalogProductTypeCharacteristicMetadata } from "../model/types";

export async function getCatalogProductTypeCharacteristics(
  productTypeCode: string,
): Promise<CatalogProductTypeCharacteristicMetadata[]> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  const response = await httpClient.get<
    CatalogProductTypeCharacteristicMetadata[]
  >(
    `/api/catalog/metadata/product-types/${encodedProductTypeCode}/characteristics`,
  );

  return response.data;
}