import type { AxiosResponse } from "axios";
import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";
import {
  catalogRecognitionDatasetSplits,
  type CatalogRecognitionDatasetBundleExportMetadata,
  type CatalogRecognitionDatasetBundleManifest,
  type CatalogRecognitionDatasetBundleWarning,
  type CatalogRecognitionDatasetSplit,
  type CatalogRecognitionDatasetSplitManifest,
} from "../model/datasetTypes";

const bundleFormatVersionHeader = "x-dataset-bundle-format-version";
const formatVersionHeader = "x-dataset-format-version";
const finalizedUntilUtcHeader = "x-dataset-finalized-until-utc";
const exportedAtUtcHeader = "x-dataset-exported-at-utc";
const exampleCountHeader = "x-dataset-example-count";
const sha256Header = "x-dataset-sha256";
const splitAlgorithmVersionHeader = "x-dataset-split-algorithm-version";
const productGroupCountHeader = "x-dataset-product-group-count";
const bundleManifestHeader = "x-dataset-bundle-manifest";

export async function exportCatalogRecognitionDatasetBundle(finalizedUntilUtc: string): Promise<CatalogRecognitionDatasetBundleExportMetadata> {
  const response = await httpClient.get<Blob>(
    "/api/catalog/recognition/datasets/export-bundle",
    {
      params: {
        finalizedUntilUtc,
      },
      responseType: "blob",
    },
  );

  const manifest = parseBundleManifest(
    getRequiredHeader(response, bundleManifestHeader),
  );

  const exportedAtUtc = getRequiredDateHeader(response, exportedAtUtcHeader);
  const responseFinalizedUntilUtc = getRequiredDateHeader(
    response,
    finalizedUntilUtcHeader,
  );
  const bundleFormatVersion = getRequiredHeader(
    response,
    bundleFormatVersionHeader,
  );
  const datasetFormatVersion = getRequiredHeader(response, formatVersionHeader);
  const splitAlgorithmVersion = getRequiredHeader(
    response,
    splitAlgorithmVersionHeader,
  );
  const exampleCount = parseNonNegativeInteger(
    getRequiredHeader(response, exampleCountHeader),
    exampleCountHeader,
  );
  const productGroupCount = parseNonNegativeInteger(
    getRequiredHeader(response, productGroupCountHeader),
    productGroupCountHeader,
  );
  const datasetSha256 = parseSha256(
    getRequiredHeader(response, sha256Header),
    sha256Header,
  );

  validateHeaderManifestConsistency(
    manifest,
    responseFinalizedUntilUtc,
    bundleFormatVersion,
    datasetFormatVersion,
    splitAlgorithmVersion,
    exampleCount,
    productGroupCount,
    datasetSha256,
  );

  const fileName = downloadFileResponse(
    response,
    buildFallbackFileName(responseFinalizedUntilUtc),
  );

  return {
    fileName,
    exportedAtUtc,
    manifest,
  };
}

function getRequiredHeader(
  response: AxiosResponse<Blob>,
  headerName: string,
): string {
  const value = response.headers[headerName];

  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(
      `В ответе backend отсутствует обязательный заголовок '${headerName}'.`,
    );
  }

  return value.trim();
}

function getRequiredDateHeader(
  response: AxiosResponse<Blob>,
  headerName: string,
): string {
  const value = getRequiredHeader(response, headerName);

  if (Number.isNaN(Date.parse(value))) {
    throw new Error(
      `Backend вернул некорректную дату в заголовке '${headerName}'.`,
    );
  }

  return value;
}

function parseBundleManifest(
  encodedManifest: string,
): CatalogRecognitionDatasetBundleManifest {
  let decodedManifest: string;

  try {
    const binaryValue = window.atob(encodedManifest);
    const bytes = Uint8Array.from(
      binaryValue,
      (character) => character.charCodeAt(0),
    );

    decodedManifest = new TextDecoder().decode(bytes);
  } catch {
    throw new Error("Не удалось декодировать manifest ZIP-пакета.");
  }

  let parsedManifest: unknown;

  try {
    parsedManifest = JSON.parse(decodedManifest);
  } catch {
    throw new Error("Backend вернул некорректный JSON manifest.");
  }

  const manifest = requireRecord(parsedManifest, "manifest");
  const rawSplits = requireArray(manifest, "splits");
  const rawWarnings = requireArray(manifest, "warnings");

  const splits = rawSplits.map((value, index) =>
    parseSplitManifest(value, index),
  );
  const warnings = rawWarnings.map((value, index) =>
    parseWarning(value, index),
  );

  validateSplits(splits);

  const result: CatalogRecognitionDatasetBundleManifest = {
    bundleFormatVersion: requireString(manifest, "bundleFormatVersion"),
    datasetFormatVersion: requireString(manifest, "datasetFormatVersion"),
    finalizedUntilUtc: requireDateString(manifest, "finalizedUntilUtc"),
    splitAlgorithmVersion: requireString(manifest, "splitAlgorithmVersion"),
    productGroupCount: requireNonNegativeInteger(
      manifest,
      "productGroupCount",
    ),
    exampleCount: requireNonNegativeInteger(manifest, "exampleCount"),
    splits,
    datasetSha256: requireSha256(manifest, "datasetSha256"),
    warnings,
  };

  const splitExampleCount = result.splits.reduce(
    (total, split) => total + split.exampleCount,
    0,
  );

  if (splitExampleCount !== result.exampleCount) {
    throw new Error(
      "Количество примеров в частях ZIP не совпадает с общим количеством manifest.",
    );
  }

  return result;
}

function parseSplitManifest(
  value: unknown,
  index: number,
): CatalogRecognitionDatasetSplitManifest {
  const split = requireRecord(value, `splits[${index}]`);
  const splitName = requireString(split, "split");

  if (!isDatasetSplit(splitName)) {
    throw new Error(`Manifest содержит неизвестную часть '${splitName}'.`);
  }

  return {
    split: splitName,
    fileName: requireString(split, "fileName"),
    exampleCount: requireNonNegativeInteger(split, "exampleCount"),
    productGroupCount: requireNonNegativeInteger(
      split,
      "productGroupCount",
    ),
    sha256: requireSha256(split, "sha256"),
  };
}

function parseWarning(
  value: unknown,
  index: number,
): CatalogRecognitionDatasetBundleWarning {
  const warning = requireRecord(value, `warnings[${index}]`);

  return {
    code: requireString(warning, "code"),
    message: requireString(warning, "message"),
  };
}

function validateSplits(
  splits: CatalogRecognitionDatasetSplitManifest[],
): void {
  if (splits.length !== catalogRecognitionDatasetSplits.length) {
    throw new Error(
      "Manifest должен содержать Train, Validation и Test.",
    );
  }

  for (const expectedSplit of catalogRecognitionDatasetSplits) {
    const matchingSplits = splits.filter(
      (split) => split.split === expectedSplit,
    );

    if (matchingSplits.length !== 1) {
      throw new Error(
        `Manifest должен содержать ровно одну часть '${expectedSplit}'.`,
      );
    }
  }
}

function validateHeaderManifestConsistency(
  manifest: CatalogRecognitionDatasetBundleManifest,
  finalizedUntilUtc: string,
  bundleFormatVersion: string,
  datasetFormatVersion: string,
  splitAlgorithmVersion: string,
  exampleCount: number,
  productGroupCount: number,
  datasetSha256: string,
): void {
  if (
    Date.parse(manifest.finalizedUntilUtc) !==
    Date.parse(finalizedUntilUtc)
  ) {
    throw new Error(
      "Момент отсечения в HTTP-заголовке не совпадает с manifest.",
    );
  }

  if (manifest.bundleFormatVersion !== bundleFormatVersion) {
    throw new Error(
      "Версия ZIP-пакета в HTTP-заголовке не совпадает с manifest.",
    );
  }

  if (manifest.datasetFormatVersion !== datasetFormatVersion) {
    throw new Error(
      "Версия JSONL в HTTP-заголовке не совпадает с manifest.",
    );
  }

  if (manifest.splitAlgorithmVersion !== splitAlgorithmVersion) {
    throw new Error(
      "Версия алгоритма разделения не совпадает с manifest.",
    );
  }

  if (manifest.exampleCount !== exampleCount) {
    throw new Error(
      "Количество примеров в HTTP-заголовке не совпадает с manifest.",
    );
  }

  if (manifest.productGroupCount !== productGroupCount) {
    throw new Error(
      "Количество групп товаров в HTTP-заголовке не совпадает с manifest.",
    );
  }

  if (manifest.datasetSha256.toUpperCase() !== datasetSha256) {
    throw new Error(
      "SHA-256 датасета в HTTP-заголовке не совпадает с manifest.",
    );
  }
}

function requireRecord(
  value: unknown,
  fieldName: string,
): Record<string, unknown> {
  if (
    typeof value !== "object" ||
    value === null ||
    Array.isArray(value)
  ) {
    throw new Error(`Поле '${fieldName}' должно быть объектом.`);
  }

  return value as Record<string, unknown>;
}

function requireArray(
  source: Record<string, unknown>,
  fieldName: string,
): unknown[] {
  const value = source[fieldName];

  if (!Array.isArray(value)) {
    throw new Error(`Поле '${fieldName}' должно быть массивом.`);
  }

  return value;
}

function requireString(
  source: Record<string, unknown>,
  fieldName: string,
): string {
  const value = source[fieldName];

  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(`Поле '${fieldName}' должно быть непустой строкой.`);
  }

  return value;
}

function requireDateString(
  source: Record<string, unknown>,
  fieldName: string,
): string {
  const value = requireString(source, fieldName);

  if (Number.isNaN(Date.parse(value))) {
    throw new Error(`Поле '${fieldName}' содержит некорректную дату.`);
  }

  return value;
}

function requireNonNegativeInteger(
  source: Record<string, unknown>,
  fieldName: string,
): number {
  const value = source[fieldName];

  if (
    typeof value !== "number" ||
    !Number.isSafeInteger(value) ||
    value < 0
  ) {
    throw new Error(
      `Поле '${fieldName}' должно быть целым неотрицательным числом.`,
    );
  }

  return value;
}

function requireSha256(
  source: Record<string, unknown>,
  fieldName: string,
): string {
  const value = requireString(source, fieldName);

  return parseSha256(value, fieldName);
}

function parseNonNegativeInteger(
  value: string,
  fieldName: string,
): number {
  const parsedValue = Number(value);

  if (!Number.isSafeInteger(parsedValue) || parsedValue < 0) {
    throw new Error(
      `Backend вернул некорректное число в поле '${fieldName}'.`,
    );
  }

  return parsedValue;
}

function parseSha256(value: string, fieldName: string): string {
  if (!/^[A-Fa-f0-9]{64}$/.test(value)) {
    throw new Error(
      `Backend вернул некорректный SHA-256 в поле '${fieldName}'.`,
    );
  }

  return value.toUpperCase();
}

function isDatasetSplit(
  value: string,
): value is CatalogRecognitionDatasetSplit {
  return catalogRecognitionDatasetSplits.some(
    (split) => split === value,
  );
}

function buildFallbackFileName(finalizedUntilUtc: string): string {
  const timestamp = new Date(finalizedUntilUtc)
    .toISOString()
    .replaceAll("-", "")
    .replaceAll(":", "")
    .replace(/\.\d{3}Z$/, "Z");

  return `catalog-recognition-dataset-${timestamp}.zip`;
}