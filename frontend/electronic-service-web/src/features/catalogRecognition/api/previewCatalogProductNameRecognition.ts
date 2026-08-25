import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductNameRecognitionPreview,
  PreviewCatalogProductNameRecognitionParameters,
} from "../model/types";

export async function previewCatalogProductNameRecognition(
  parameters: PreviewCatalogProductNameRecognitionParameters,
): Promise<CatalogProductNameRecognitionPreview> {
  const response =
    await httpClient.post<CatalogProductNameRecognitionPreview>(
      "/api/catalog/recognition/preview",
      {
        productName: parameters.productName,
        productTypeCode: parameters.productTypeCode,
      },
    );

  return response.data;
}