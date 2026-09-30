import { httpClient } from "@/shared/api/httpClient";
import type {
  ApplyProductTypeMigrationRequest,
} from "../model/types";

export async function applyProductTypeMigration(
  productId: string,
  request: ApplyProductTypeMigrationRequest,
): Promise<void> {
  const encodedProductId =
    encodeURIComponent(productId);

  await httpClient.put(
    `/api/catalog/products/${encodedProductId}/product-type-migration`,
    request,
  );
}