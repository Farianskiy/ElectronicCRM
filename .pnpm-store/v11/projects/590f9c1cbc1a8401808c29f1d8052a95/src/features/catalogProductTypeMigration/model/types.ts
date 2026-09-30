export interface ProductTypeMigrationCharacteristicValue {
  definitionId: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
  value: string;
}

export interface ProductTypeMigrationMissingRequiredCharacteristic {
  definitionId: string;
  code: string;
  name: string;
  dataType: string;
  unit?: string | null;
}

export interface ProductTypeMigrationPreview {
  productId: string;
  productVersion: number;

  currentProductTypeId: string;
  currentProductTypeCode: string;
  currentProductTypeName: string;

  targetProductTypeId: string;
  targetProductTypeCode: string;
  targetProductTypeName: string;

  canApplyWithoutAdditionalValues: boolean;

  preservedCharacteristics:
    ProductTypeMigrationCharacteristicValue[];

  removedCharacteristics:
    ProductTypeMigrationCharacteristicValue[];

  missingRequiredCharacteristics:
    ProductTypeMigrationMissingRequiredCharacteristic[];
}

export interface PreviewProductTypeMigrationRequest {
  targetProductTypeId: string;
}

export interface ApplyProductTypeMigrationValueRequest {
  definitionId: string;
  value: string;
}

export interface ApplyProductTypeMigrationRequest {
  targetProductTypeId: string;

  expectedProductVersion: number;

  expectedCurrentProductTypeId: string;

  expectedRemovedCharacteristicDefinitionIds:
    string[];

  expectedMissingRequiredCharacteristicDefinitionIds:
    string[];

  requiredValues:
    ApplyProductTypeMigrationValueRequest[];
}