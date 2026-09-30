export interface CatalogStockImportIssue {
  rowNumber: number;
  article: string;
  message: string;
}

export interface CatalogStockImportResponse {
  readRowsCount: number;
  matchedRowsCount: number;
  updatedProductsCount: number;
  skippedRowsCount: number;
  issues: CatalogStockImportIssue[];
}

export interface CatalogStockImportRequest {
  manufacturerId: string;
  file: File;
}
