import { httpClient } from "@/shared/api/httpClient";
import type {
  ApplyCatalogPriceCalculationImportRequest,
  ApplyCatalogPriceCalculationImportResponse,
  CatalogPriceCalculationDetails,
  PreviewCatalogPriceCalculationImportRequest,
  PreviewCatalogPriceCalculationImportResponse,
  SearchCatalogPriceCalculationProductsParams,
  SearchCatalogPriceCalculationProductsResponse,
} from "../model/types";

export async function getCatalogPriceCalculation(
  calculationId: string,
): Promise<CatalogPriceCalculationDetails> {
  const response = await httpClient.get<CatalogPriceCalculationDetails>(
    `/api/catalog/price-calculations/${calculationId}`,
  );

  return response.data;
}

export async function searchCatalogPriceCalculationProducts(
  params: SearchCatalogPriceCalculationProductsParams,
): Promise<SearchCatalogPriceCalculationProductsResponse> {
  const queryParams = new URLSearchParams();

  if (params.search.trim().length > 0) {
    queryParams.set("search", params.search.trim());
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<SearchCatalogPriceCalculationProductsResponse>(
      `/api/catalog/price-calculations/${params.calculationId}/products?${queryParams.toString()}`,
    );

  return response.data;
}

export async function addCatalogPriceCalculationLine(request: {
  calculationId: string;
  productId: string;
  quantity: number;
}): Promise<void> {
  await httpClient.post(
    `/api/catalog/price-calculations/${request.calculationId}/lines`,
    {
      productId: request.productId,
      quantity: request.quantity,
    },
  );
}

export async function changeCatalogPriceCalculationLineQuantity(request: {
  calculationId: string;
  lineId: string;
  quantity: number;
}): Promise<void> {
  await httpClient.patch(
    `/api/catalog/price-calculations/${request.calculationId}/lines/${request.lineId}/quantity`,
    {
      quantity: request.quantity,
    },
  );
}

export async function removeCatalogPriceCalculationLine(request: {
  calculationId: string;
  lineId: string;
}): Promise<void> {
  await httpClient.delete(
    `/api/catalog/price-calculations/${request.calculationId}/lines/${request.lineId}`,
  );
}

export async function setCatalogPriceCalculationManufacturerDiscount(request: {
  calculationId: string;
  manufacturerId: string;
  discountPercent: number;
}): Promise<void> {
  await httpClient.put(
    `/api/catalog/price-calculations/${request.calculationId}/discounts/${request.manufacturerId}`,
    {
      discountPercent: request.discountPercent,
    },
  );
}

export async function removeCatalogPriceCalculationManufacturerDiscount(request: {
  calculationId: string;
  manufacturerId: string;
}): Promise<void> {
  await httpClient.delete(
    `/api/catalog/price-calculations/${request.calculationId}/discounts/${request.manufacturerId}`,
  );
}

export async function completeCatalogPriceCalculation(
  calculationId: string,
): Promise<void> {
  await httpClient.post(
    `/api/catalog/price-calculations/${calculationId}/complete`,
  );
}

export async function previewCatalogPriceCalculationImport(
  request: PreviewCatalogPriceCalculationImportRequest,
): Promise<PreviewCatalogPriceCalculationImportResponse> {
  const formData = new FormData();

  formData.append("file", request.file);

  const response =
    await httpClient.post<PreviewCatalogPriceCalculationImportResponse>(
      `/api/catalog/price-calculations/${request.calculationId}/import-preview`,
      formData,
    );

  return response.data;
}

export async function applyCatalogPriceCalculationImport(
  request: ApplyCatalogPriceCalculationImportRequest,
): Promise<ApplyCatalogPriceCalculationImportResponse> {
  const response =
    await httpClient.post<ApplyCatalogPriceCalculationImportResponse>(
      `/api/catalog/price-calculations/${request.calculationId}/import-apply`,
      {
        rows: request.rows,
      },
    );

  return response.data;
}
