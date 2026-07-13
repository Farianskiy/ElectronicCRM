export interface CatalogProductCharacteristic {
  code: string;
  name?: string | null;
  value?: string | null;
  unit?: string | null;
}

export interface CatalogProductAlias {
  id?: string;
  value?: string | null;
  name?: string | null;
  alias?: string | null;
}

export interface CatalogProductListItem {
  id: string;
  article?: string | null;
  name: string;
  productType?: string | null;
  productTypeCode?: string | null;
  productTypeName?: string | null;
  manufacturer?: string | null;
  manufacturerName?: string | null;
  price?: number | null;
  stockQuantity?: number | null;
  isAvailable?: boolean | null;
}

export interface CatalogProductsResponse {
  items: CatalogProductListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CatalogProductsSearchParams {
  searchText: string;
  onlyInStock: boolean;
  page: number;
  pageSize: number;
}

export interface CatalogProductDetails {
  id: string;
  article?: string | null;
  name: string;
  productType?: string | null;
  productTypeCode?: string | null;
  productTypeName?: string | null;
  manufacturer?: string | null;
  manufacturerName?: string | null;
  price?: number | null;
  stockQuantity?: number | null;
  isAvailable?: boolean | null;
  characteristics?: CatalogProductCharacteristic[];
  aliases?: CatalogProductAlias[];
}