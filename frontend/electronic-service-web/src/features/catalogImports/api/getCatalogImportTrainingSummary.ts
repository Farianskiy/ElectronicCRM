import { httpClient } from "@/shared/api/httpClient";

export interface CatalogImportTrainingSummaryGroup {
  manufacturerId: string;
  manufacturerName: string;
  productTypeId: string;
  productTypeName: string;
  characteristicDefinitionId: string;
  characteristicName: string;
  activeExamplesCount: number;
  distinctValuesCount: number;
}

export interface CatalogImportTrainingSummary {
  batchId: string;
  activeExamplesCount: number;
  evaluationExamplesCount: number;
  revokedExamplesCount: number;
  groups: CatalogImportTrainingSummaryGroup[];
}

export const catalogImportTrainingSummaryQueryKey = (batchId: string) =>
  ["training-examples", "import-summary", batchId] as const;

export async function getCatalogImportTrainingSummary(
  batchId: string,
): Promise<CatalogImportTrainingSummary> {
  const response = await httpClient.get<CatalogImportTrainingSummary>(
    `/api/catalog/import-batches/${batchId}/training-summary`,
  );

  return response.data;
}
