import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getCatalogImportColumnTargetLabel } from "./catalogImportColumnTarget";
import type {
  CatalogImportColumnTargetKind,
  CatalogImportMappingColumn,
} from "./types";

const standardTargetKinds: readonly CatalogImportColumnTargetKind[] = [
  "Name",
  "Article",
  "Manufacturer",
  "Price",
  "StockQuantity",
];

const requiredStandardTargetKinds: readonly CatalogImportColumnTargetKind[] = [
  "Name",
  "Article",
  "Manufacturer",
];

export interface CatalogImportMappingMetrics {
  requiredSystemFieldsMapped: number;
  requiredSystemFieldsTotal: number;

  requiredCharacteristicsMapped: number;
  requiredCharacteristicsTotal: number;

  unmappedColumnsCount: number;
  incompleteCharacteristicColumnsCount: number;
  duplicateAssignmentsCount: number;

  isComplete: boolean;
}

export interface CatalogImportMappingValidationResult {
  errors: string[];
  columnErrors: Record<string, string[]>;
  metrics: CatalogImportMappingMetrics;
}

export function validateCatalogImportMapping(
  productTypeId: string,
  columns: CatalogImportMappingColumn[],
  characteristics: CatalogProductTypeCharacteristicMetadata[],
): CatalogImportMappingValidationResult {
  const errors: string[] = [];
  const columnErrors: Record<string, string[]> = {};

  function addColumnError(
    columnId: string,
    message: string,
  ): void {
    columnErrors[columnId] = [
      ...(columnErrors[columnId] ?? []),
      message,
    ];
  }

  if (!productTypeId) {
    errors.push("Выберите тип товара.");
  }

  const unmappedColumns = columns.filter(
    (column) => column.targetKind === "Unmapped",
  );

  for (const column of unmappedColumns) {
    addColumnError(
      column.columnId,
      "Выберите назначение или укажите, что колонку нужно игнорировать.",
    );
  }

  if (unmappedColumns.length > 0) {
    const headers = unmappedColumns
      .slice(0, 5)
      .map((column) => `«${column.sourceHeader}»`)
      .join(", ");

    const suffix =
      unmappedColumns.length > 5
        ? ` и ещё ${unmappedColumns.length - 5}`
        : "";

    errors.push(
      `Не выбрано назначение для колонок: ${headers}${suffix}.`,
    );
  }

  let requiredSystemFieldsMapped = 0;

  for (const requiredTarget of requiredStandardTargetKinds) {
    const mappedColumns = columns.filter(
      (column) => column.targetKind === requiredTarget,
    );

    if (mappedColumns.length === 1) {
      requiredSystemFieldsMapped++;
    }

    if (mappedColumns.length === 0) {
      errors.push(
        `Не назначено обязательное системное поле «${getCatalogImportColumnTargetLabel(
          requiredTarget,
        )}».`,
      );
    }
  }

  let duplicateAssignmentsCount = 0;

  for (const standardTarget of standardTargetKinds) {
    const mappedColumns = columns.filter(
      (column) => column.targetKind === standardTarget,
    );

    if (mappedColumns.length <= 1) {
      continue;
    }

    duplicateAssignmentsCount++;

    const label =
      getCatalogImportColumnTargetLabel(standardTarget);

    errors.push(
      `Назначение «${label}» выбрано более одного раза.`,
    );

    for (const column of mappedColumns) {
      addColumnError(
        column.columnId,
        `Назначение «${label}» уже используется другой Excel-колонкой.`,
      );
    }
  }

  const characteristicColumns = columns.filter(
    (column) => column.targetKind === "Characteristic",
  );

  const incompleteCharacteristicColumns =
    characteristicColumns.filter(
      (column) => !column.characteristicDefinitionId,
    );

  for (const column of incompleteCharacteristicColumns) {
    addColumnError(
      column.columnId,
      "Выберите конкретную характеристику товара.",
    );
  }

  if (incompleteCharacteristicColumns.length > 0) {
    errors.push(
      `Для ${incompleteCharacteristicColumns.length} ${
        incompleteCharacteristicColumns.length === 1
          ? "колонки"
          : "колонок"
      } не выбрана конкретная характеристика.`,
    );
  }

  const characteristicColumnsByDefinitionId =
    new Map<string, CatalogImportMappingColumn[]>();

  for (const column of characteristicColumns) {
    const definitionId =
      column.characteristicDefinitionId;

    if (!definitionId) {
      continue;
    }

    characteristicColumnsByDefinitionId.set(
      definitionId,
      [
        ...(characteristicColumnsByDefinitionId.get(
          definitionId,
        ) ?? []),
        column,
      ],
    );
  }

  for (
    const [
      definitionId,
      mappedColumns,
    ] of characteristicColumnsByDefinitionId
  ) {
    if (mappedColumns.length <= 1) {
      continue;
    }

    duplicateAssignmentsCount++;

    const characteristic = characteristics.find(
      (item) => item.id === definitionId,
    );

    const characteristicName =
      characteristic?.name ?? definitionId;

    errors.push(
      `Характеристика «${characteristicName}» назначена более чем одной Excel-колонке.`,
    );

    for (const column of mappedColumns) {
      addColumnError(
        column.columnId,
        `Характеристика «${characteristicName}» уже используется другой колонкой.`,
      );
    }
  }

  const requiredCharacteristics =
    characteristics.filter(
      (characteristic) => characteristic.isRequired,
    );

  let requiredCharacteristicsMapped = 0;

  for (const characteristic of requiredCharacteristics) {
    const mappedColumns = characteristicColumns.filter(
      (column) =>
        column.characteristicDefinitionId ===
        characteristic.id,
    );

    if (mappedColumns.length === 1) {
      requiredCharacteristicsMapped++;
      continue;
    }

    if (mappedColumns.length === 0) {
      errors.push(
        `Не сопоставлена обязательная характеристика «${characteristic.name}».`,
      );
    }
  }

  const isComplete =
    errors.length === 0 &&
    productTypeId.length > 0;

  return {
    errors,
    columnErrors,
    metrics: {
      requiredSystemFieldsMapped,
      requiredSystemFieldsTotal:
        requiredStandardTargetKinds.length,

      requiredCharacteristicsMapped,
      requiredCharacteristicsTotal:
        requiredCharacteristics.length,

      unmappedColumnsCount:
        unmappedColumns.length,

      incompleteCharacteristicColumnsCount:
        incompleteCharacteristicColumns.length,

      duplicateAssignmentsCount,
      isComplete,
    },
  };
}