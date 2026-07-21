import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductTypeCharacteristicSchema,
} from "../model/types";

export async function
getCatalogProductTypeCharacteristicSchema(
  productTypeCode: string,
): Promise<CatalogProductTypeCharacteristicSchema> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  const response =
    await httpClient.get<
      CatalogProductTypeCharacteristicSchema
    >(
      `/api/catalog/product-types/${encodedProductTypeCode}/characteristics/schema`,
    );

  return response.data;
}