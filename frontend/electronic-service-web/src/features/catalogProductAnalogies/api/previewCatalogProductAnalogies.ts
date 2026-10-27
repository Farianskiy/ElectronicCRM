import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductAnalogiesPreview,
  PreviewCatalogProductAnalogiesParameters,
} from "../model/types";

export async function previewCatalogProductAnalogies(
  parameters: PreviewCatalogProductAnalogiesParameters,
): Promise<CatalogProductAnalogiesPreview> {
  const response = await httpClient.post<CatalogProductAnalogiesPreview>(
    "/api/catalog/products/analogies/preview",
    {
      productName: parameters.productName,
      article: parameters.article ?? null,
      manufacturerId: parameters.manufacturerId,
      productTypeId: parameters.productTypeId ?? null,
      knownCharacteristics: parameters.knownCharacteristics,
      maximumResults: parameters.maximumResults ?? 10,
      importBatchId: parameters.importBatchId ?? null,
      importRowId: parameters.importRowId ?? null,
    },
  );

  return response.data;
}
