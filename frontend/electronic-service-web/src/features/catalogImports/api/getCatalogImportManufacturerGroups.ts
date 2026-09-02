import { httpClient } from "@/shared/api/httpClient";
import type {
  GetCatalogImportManufacturerGroupsParams,
  GetCatalogImportManufacturerGroupsResponse,
} from "../model/types";

export async function getCatalogImportManufacturerGroups(
  params: GetCatalogImportManufacturerGroupsParams,
): Promise<GetCatalogImportManufacturerGroupsResponse> {
  const response =
    await httpClient.get<GetCatalogImportManufacturerGroupsResponse>(
      `/api/catalog/import-batches/${params.batchId}/rows/manufacturer-groups`,
      {
        params: {
          expectedVersion: params.expectedVersion,
        },
      },
    );

  return response.data;
}