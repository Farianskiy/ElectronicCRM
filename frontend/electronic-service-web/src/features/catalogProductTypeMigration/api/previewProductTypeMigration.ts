import { httpClient } from "@/shared/api/httpClient";
import type {
  PreviewProductTypeMigrationRequest,
  ProductTypeMigrationPreview,
} from "../model/types";

export async function previewProductTypeMigration(
  productId: string,
  request: PreviewProductTypeMigrationRequest,
): Promise<ProductTypeMigrationPreview> {
  const encodedProductId =
    encodeURIComponent(productId);

  const response =
    await httpClient.post<ProductTypeMigrationPreview>(
      `/api/catalog/products/${encodedProductId}/product-type-migration/preview`,
      request,
    );

  return response.data;
}