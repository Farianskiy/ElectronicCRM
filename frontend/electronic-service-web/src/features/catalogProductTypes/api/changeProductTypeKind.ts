import { httpClient } from "@/shared/api/httpClient";
import type {
  ProductTypeKind,
  ProductTypeKindChangePreview,
} from "../model/types";

export async function changeProductTypeKind(
  productTypeId: string,
  targetKind: ProductTypeKind,
): Promise<ProductTypeKindChangePreview> {
  const response = await httpClient.patch<ProductTypeKindChangePreview>(
    `/api/catalog/product-types/${productTypeId}/kind`,
    { targetKind },
  );

  return response.data;
}
