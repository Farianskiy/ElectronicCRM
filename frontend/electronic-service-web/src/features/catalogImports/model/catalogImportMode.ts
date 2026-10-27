import type { CatalogImportMode } from "./types";

export function getCatalogImportModeLabel(mode: CatalogImportMode): string {
  switch (mode) {
    case "CreateOnly":
      return "Добавление новых";
    case "UpdateCharacteristicsOnly":
      return "Обновление характеристик";
    case "CreateMissingAndUpdateCharacteristics":
      return "Создание и обновление характеристик";
  }
}

export function getCatalogImportApplyDescription(
  mode: CatalogImportMode,
): string {
  switch (mode) {
    case "CreateOnly":
      return "Будут созданы только новые товары. Существующий товар с той же парой «производитель + артикул» остановит применение пакета.";
    case "UpdateCharacteristicsOnly":
      return "Будут обновлены только характеристики существующих товаров. Отсутствующий товар остановит применение пакета.";
    case "CreateMissingAndUpdateCharacteristics":
      return "Отсутствующие товары будут созданы, а у существующих обновятся только характеристики из Excel.";
  }
}
