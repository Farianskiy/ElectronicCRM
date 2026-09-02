import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateApprovedManufacturerAliasRequest,
  CreateApprovedManufacturerAliasResponse,
} from "../model/types";

export async function createApprovedManufacturerAlias(
  request: CreateApprovedManufacturerAliasRequest,
): Promise<CreateApprovedManufacturerAliasResponse> {
  const response = await httpClient.post<CreateApprovedManufacturerAliasResponse>(
    "/api/catalog/manufacturer-aliases",
    request,
  );

  return response.data;
}