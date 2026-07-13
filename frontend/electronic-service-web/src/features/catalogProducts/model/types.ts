export interface CatalogProductCharacteristic {
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
  value: string;
}

export interface CatalogProductListItem {
  id: string;
  article: string;
  name: string;
  productTypeCode: string;
  productTypeName: string;
  manufacturerName: string;
  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;
}

export interface CatalogProductsResponse {
  items: CatalogProductListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CatalogProductsSearchParams {
  search: string;
  productTypeCode?: string | null;
  manufacturer?: string | null;
  page: number;
  pageSize: number;
}

export interface SearchProductCharacteristicRequest {
  code: string;
  value: string;
}

export interface AdvancedCatalogProductsSearchParams {
  search?: string | null;
  productTypeCode?: string | null;
  manufacturer?: string | null;
  characteristics?: SearchProductCharacteristicRequest[];
  page: number;
  pageSize: number;
}

export interface CatalogProductDetails {
  id: string;
  article: string;
  name: string;
  productTypeCode: string;
  productTypeName: string;
  manufacturerName: string;
  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;
  characteristics: CatalogProductCharacteristic[];
  aliases: string[];
}