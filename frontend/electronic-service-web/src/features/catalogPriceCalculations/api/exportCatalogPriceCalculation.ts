import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";

export async function exportCatalogPriceCalculation(calculationId: string): Promise<string> {
  const response = await httpClient.get<Blob>(`/api/catalog/price-calculations/${calculationId}/export`, {
    responseType: "blob",
  });

  return downloadFileResponse(response, `price-calculation-${calculationId}.xlsx`);
}