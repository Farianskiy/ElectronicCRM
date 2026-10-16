import { httpClient } from "@/shared/api/httpClient";
import type { ProductTypeArchivePreview } from "../model/types";

export async function previewProductTypeArchive(
  productTypeId: string,
): Promise<ProductTypeArchivePreview> {
  const response = await httpClient.post<ProductTypeArchivePreview>(
    `/api/catalog/product-types/${productTypeId}/archive/preview`,
  );

  return response.data;
}

export async function archiveProductType(
  productTypeId: string,
): Promise<ProductTypeArchivePreview> {
  const response = await httpClient.post<ProductTypeArchivePreview>(
    `/api/catalog/product-types/${productTypeId}/archive`,
  );

  return response.data;
}

export async function restoreProductType(productTypeId: string): Promise<void> {
  await httpClient.post(`/api/catalog/product-types/${productTypeId}/restore`);
}
