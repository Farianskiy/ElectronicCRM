export const catalogRecognitionDatasetExampleKinds = [
  "AcceptedSpan",
  "AcceptedValue",
  "CorrectedValue",
  "RejectedSpan",
  "RejectedValue",
  "ManualValue",
  "ConflictResolution",
] as const;

export type CatalogRecognitionDatasetExampleKind =
  (typeof catalogRecognitionDatasetExampleKinds)[number];

export type CatalogRecognitionDatasetExampleCounts = Record<
  CatalogRecognitionDatasetExampleKind,
  number
>;

export interface CatalogRecognitionDatasetExportMetadata {
  fileName: string;
  formatVersion: string;
  finalizedUntilUtc: string;
  exportedAtUtc: string;
  exampleCount: number;
  exampleCounts: CatalogRecognitionDatasetExampleCounts;
  sha256: string;
}

export const catalogRecognitionDatasetSplits = [
  "Train",
  "Validation",
  "Test",
] as const;

export type CatalogRecognitionDatasetSplit =
  (typeof catalogRecognitionDatasetSplits)[number];

export interface CatalogRecognitionDatasetSplitManifest {
  split: CatalogRecognitionDatasetSplit;
  fileName: string;
  exampleCount: number;
  productGroupCount: number;
  sha256: string;
}

export interface CatalogRecognitionDatasetBundleWarning {
  code: string;
  message: string;
}

export interface CatalogRecognitionDatasetBundleManifest {
  bundleFormatVersion: string;
  datasetFormatVersion: string;
  finalizedUntilUtc: string;
  splitAlgorithmVersion: string;
  productGroupCount: number;
  exampleCount: number;
  splits: CatalogRecognitionDatasetSplitManifest[];
  datasetSha256: string;
  warnings: CatalogRecognitionDatasetBundleWarning[];
}

export interface CatalogRecognitionDatasetBundleExportMetadata {
  fileName: string;
  exportedAtUtc: string;
  manifest: CatalogRecognitionDatasetBundleManifest;
}