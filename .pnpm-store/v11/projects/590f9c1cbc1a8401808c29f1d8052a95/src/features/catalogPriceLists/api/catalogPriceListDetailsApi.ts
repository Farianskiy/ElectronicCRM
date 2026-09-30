import { httpClient } from "@/shared/api/httpClient";
import type {
  ActivateCatalogPriceListResponse,
  BulkUpdateCatalogPriceListRowsRequest,
  BulkUpdateCatalogPriceListRowsResponse,
  CatalogPriceListDetails,
  GetCatalogPriceListRowsParams,
  GetCatalogPriceListRowsResponse,
  ProcessCatalogPriceListResponse,
  SearchCatalogPriceListProductsParams,
  SearchCatalogPriceListProductsResponse,
  UpdateCatalogPriceListRowRequest,
  UpdateCatalogPriceListRowResponse,
  ApplyCatalogPriceListIssueGroupRequest,
  ApplyCatalogPriceListIssueGroupResponse,
  GetCatalogPriceListIssueGroupsParams,
  GetCatalogPriceListIssueGroupsResponse,
} from "../model/types";

export async function getCatalogPriceList(
  priceListId: string,
): Promise<CatalogPriceListDetails> {
  const response = await httpClient.get<CatalogPriceListDetails>(
    `/api/catalog/price-lists/${priceListId}`,
  );

  return response.data;
}

export async function getCatalogPriceListRows(
  params: GetCatalogPriceListRowsParams,
): Promise<GetCatalogPriceListRowsResponse> {
  const queryParams = new URLSearchParams();

  if (params.status) {
    queryParams.set("status", params.status);
  }

  if (params.matchStatus) {
    queryParams.set("matchStatus", params.matchStatus);
  }

  if (params.issueCode?.trim()) {
    queryParams.set("issueCode", params.issueCode.trim());
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<GetCatalogPriceListRowsResponse>(
      `/api/catalog/price-lists/${params.priceListId}/rows?${queryParams.toString()}`,
    );

  return response.data;
}

export async function processCatalogPriceList(
  priceListId: string,
): Promise<ProcessCatalogPriceListResponse> {
  const response =
    await httpClient.post<ProcessCatalogPriceListResponse>(
      `/api/catalog/price-lists/${priceListId}/process`,
    );

  return response.data;
}

export async function activateCatalogPriceList(
  priceListId: string,
): Promise<ActivateCatalogPriceListResponse> {
  const response =
    await httpClient.post<ActivateCatalogPriceListResponse>(
      `/api/catalog/price-lists/${priceListId}/activate`,
    );

  return response.data;
}

export async function searchCatalogPriceListProducts(
  params: SearchCatalogPriceListProductsParams,
): Promise<SearchCatalogPriceListProductsResponse> {
  const queryParams = new URLSearchParams();

  if (params.search?.trim()) {
    queryParams.set("search", params.search.trim());
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<SearchCatalogPriceListProductsResponse>(
      `/api/catalog/price-lists/${params.priceListId}/products?${queryParams.toString()}`,
    );

  return response.data;
}

export async function updateCatalogPriceListRow(
  priceListId: string,
  rowId: string,
  request: UpdateCatalogPriceListRowRequest,
): Promise<UpdateCatalogPriceListRowResponse> {
  const response =
    await httpClient.put<UpdateCatalogPriceListRowResponse>(
      `/api/catalog/price-lists/${priceListId}/rows/${rowId}`,
      request,
    );

  return response.data;
}

export async function bulkUpdateCatalogPriceListRows(
  priceListId: string,
  request: BulkUpdateCatalogPriceListRowsRequest,
): Promise<BulkUpdateCatalogPriceListRowsResponse> {
  const response =
    await httpClient.put<BulkUpdateCatalogPriceListRowsResponse>(
      `/api/catalog/price-lists/${priceListId}/rows/bulk`,
      request,
    );

  return response.data;
}

export async function getCatalogPriceListIssueGroups(
  params: GetCatalogPriceListIssueGroupsParams,
): Promise<GetCatalogPriceListIssueGroupsResponse> {
  const queryParams = new URLSearchParams();

  if (params.issueCode?.trim()) {
    queryParams.set("issueCode", params.issueCode.trim());
  }

  queryParams.set("page", params.page.toString());
  queryParams.set("pageSize", params.pageSize.toString());

  const response =
    await httpClient.get<GetCatalogPriceListIssueGroupsResponse>(
      `/api/catalog/price-lists/${params.priceListId}/issue-groups?${queryParams.toString()}`,
    );

  return response.data;
}

export async function applyCatalogPriceListIssueGroup(
  priceListId: string,
  groupKey: string,
  request: ApplyCatalogPriceListIssueGroupRequest,
): Promise<ApplyCatalogPriceListIssueGroupResponse> {
  const response =
    await httpClient.put<ApplyCatalogPriceListIssueGroupResponse>(
      `/api/catalog/price-lists/${priceListId}/issue-groups/${encodeURIComponent(groupKey)}`,
      request,
    );

  return response.data;
}