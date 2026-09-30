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
  manufacturerId: string;
  manufacturerName: string;
  priceAmount: number;
  priceCurrency: string;
  basePriceAmount: number;
  basePriceCurrency: string;
  stockQuantity: number;
}

export interface BulkUpdateCatalogProductRequest {
  productId: string;
  name?: string | null;
  article?: string | null;
  manufacturerId?: string | null;
  priceAmount?: number | null;
  priceCurrency?: string | null;
  stockQuantity?: number | null;
}

export interface BulkUpdateCatalogProductsResponse {
  updatedProductsCount: number;
  updatedProductIds: string[];
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
  onlyInStock?: boolean | null;
  page: number;
  pageSize: number;
}

export interface CatalogProductAlias {
  id: string;
  value: string;
}

export interface CatalogProductDetails {
  id: string;
  article: string;
  name: string;

  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;

  manufacturerId: string;
  manufacturerName: string;

  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;

  characteristics: CatalogProductCharacteristic[];
  aliases: CatalogProductAlias[];
}
