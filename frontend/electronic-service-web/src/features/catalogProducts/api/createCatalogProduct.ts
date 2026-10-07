import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateCatalogProductRequest,
  CreateCatalogProductResponse,
} from "../model/types";

export async function createCatalogProduct(
  request: CreateCatalogProductRequest,
): Promise<CreateCatalogProductResponse> {
  const response = await httpClient.post<CreateCatalogProductResponse>(
    "/api/catalog/products",
    request,
  );

  return response.data;
}
