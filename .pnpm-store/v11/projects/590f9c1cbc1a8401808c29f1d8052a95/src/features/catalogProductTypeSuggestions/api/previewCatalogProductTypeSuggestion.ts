import { httpClient } from "@/shared/api/httpClient";
import type {
  CatalogProductTypeSuggestionPreview,
  PreviewCatalogProductTypeSuggestionParameters,
} from "../model/types";

export async function previewCatalogProductTypeSuggestion(
  parameters: PreviewCatalogProductTypeSuggestionParameters,
): Promise<CatalogProductTypeSuggestionPreview> {
  const response =
    await httpClient.post<CatalogProductTypeSuggestionPreview>(
      "/api/catalog/product-types/suggestions/preview",
      {
        productName: parameters.productName,
      },
    );

  return response.data;
}