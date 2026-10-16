import { httpClient } from "@/shared/api/httpClient";
import type {
  ProductTypeKind,
  ProductTypeKindChangePreview,
} from "../model/types";

export async function previewProductTypeKindChange(
  productTypeId: string,
  targetKind: ProductTypeKind,
): Promise<ProductTypeKindChangePreview> {
  const response = await httpClient.post<ProductTypeKindChangePreview>(
    `/api/catalog/product-types/${productTypeId}/kind/preview`,
    { targetKind },
  );

  return response.data;
}
