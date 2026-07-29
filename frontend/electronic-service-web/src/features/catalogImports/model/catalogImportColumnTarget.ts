import type { CatalogImportColumnTargetKind } from "./types";

const targetKindLabels: Record<
  CatalogImportColumnTargetKind,
  string
> = {
  Unmapped: "Не сопоставлено",
  Ignore: "Игнорировать колонку",
  Name: "Наименование",
  Article: "Артикул",
  Manufacturer: "Производитель",
  Price: "Цена",
  StockQuantity: "Остаток",
  Characteristic: "Характеристика товара",
};

export function getCatalogImportColumnTargetLabel(
  targetKind: CatalogImportColumnTargetKind,
): string {
  return targetKindLabels[targetKind];
}