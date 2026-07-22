export function catalogProductAuditHistoryQueryKey(
  productId: string,
): readonly ["catalog-product-audit-history", string] {
  return [
    "catalog-product-audit-history",
    productId,
  ] as const;
}