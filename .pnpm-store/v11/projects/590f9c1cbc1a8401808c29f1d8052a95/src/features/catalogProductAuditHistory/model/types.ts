export interface ProductAuditHistoryChange {
  field: string;
  label: string;
  before?: string | null;
  after?: string | null;
}

export interface ProductAuditHistoryItem {
  id: string;
  operation: string;
  source: string;
  sourceId?: string | null;
  changedByUserId?: string | null;
  changedAtUtc: string;
  changes: ProductAuditHistoryChange[];
}

export interface ProductAuditHistoryPage {
  productId: string;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: ProductAuditHistoryItem[];
}