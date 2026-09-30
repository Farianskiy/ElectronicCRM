import { httpClient } from "@/shared/api/httpClient";
import type {
  UploadCatalogPriceListRequest,
  UploadCatalogPriceListResponse,
} from "../model/types";

export async function uploadCatalogPriceList(
  request: UploadCatalogPriceListRequest,
): Promise<UploadCatalogPriceListResponse> {
  const formData = new FormData();

  formData.append("manufacturerId", request.manufacturerId);
  formData.append("file", request.file);

  const response =
    await httpClient.post<UploadCatalogPriceListResponse>(
      "/api/catalog/price-lists",
      formData,
    );

  return response.data;
}