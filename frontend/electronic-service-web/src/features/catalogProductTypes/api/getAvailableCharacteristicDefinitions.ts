import { httpClient } from "@/shared/api/httpClient";
import type {
  AvailableCharacteristicDefinition,
} from "../model/types";

export async function
getAvailableCharacteristicDefinitions(
  productTypeCode: string,
  search?: string,
): Promise<AvailableCharacteristicDefinition[]> {
  const encodedProductTypeCode =
    encodeURIComponent(productTypeCode);

  const response = await httpClient.get<
    AvailableCharacteristicDefinition[]
  >(
    `/api/catalog/product-types/${encodedProductTypeCode}/characteristics/available`,
    {
      params: {
        search:
          search?.trim().length
            ? search.trim()
            : undefined,
      },
    },
  );

  return response.data;
}