import type { AxiosResponse } from "axios";
import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import { httpClient } from "@/shared/api/httpClient";
import {
  catalogRecognitionDatasetExampleKinds,
  type CatalogRecognitionDatasetExampleCounts,
  type CatalogRecognitionDatasetExampleKind,
  type CatalogRecognitionDatasetExportMetadata,
} from "../model/datasetTypes";

const formatVersionHeader = "x-dataset-format-version";
const finalizedUntilUtcHeader = "x-dataset-finalized-until-utc";
const exportedAtUtcHeader = "x-dataset-exported-at-utc";
const exampleCountHeader = "x-dataset-example-count";
const exampleCountsHeader = "x-dataset-example-counts";
const sha256Header = "x-dataset-sha256";

export async function exportCatalogRecognitionDataset(finalizedUntilUtc: string): Promise<CatalogRecognitionDatasetExportMetadata> {
  const response = await httpClient.get<Blob>(
    "/api/catalog/recognition/datasets/export",
    {
      params: {
        finalizedUntilUtc,
      },
      responseType: "blob",
    },
  );

  const formatVersion = getRequiredHeader(response, formatVersionHeader);
  const responseFinalizedUntilUtc = getRequiredDateHeader(response, finalizedUntilUtcHeader);
  const exportedAtUtc = getRequiredDateHeader(response, exportedAtUtcHeader);
  const exampleCount = parseNonNegativeInteger(getRequiredHeader(response, exampleCountHeader), exampleCountHeader);
  const exampleCounts = parseExampleCounts(getRequiredHeader(response, exampleCountsHeader));
  const sha256 = getRequiredHeader(response, sha256Header);

  if (!/^[A-Fa-f0-9]{64}$/.test(sha256)) {
    throw new Error("Backend вернул некорректный SHA-256 датасета.");
  }

  const countedExamples = Object.values(exampleCounts).reduce((total, count) => total + count, 0);

  if (countedExamples !== exampleCount) {
    throw new Error("Общее количество примеров не совпадает со статистикой по типам.");
  }

  const fileName = downloadFileResponse(response, buildFallbackFileName(responseFinalizedUntilUtc));

  return {
    fileName,
    formatVersion,
    finalizedUntilUtc: responseFinalizedUntilUtc,
    exportedAtUtc,
    exampleCount,
    exampleCounts,
    sha256: sha256.toUpperCase(),
  };
}

function getRequiredHeader(response: AxiosResponse<Blob>, headerName: string): string {
  const value = response.headers[headerName];

  if (typeof value !== "string" || value.trim().length === 0) {
    throw new Error(`В ответе backend отсутствует обязательный заголовок '${headerName}'.`);
  }

  return value.trim();
}

function getRequiredDateHeader(response: AxiosResponse<Blob>, headerName: string): string {
  const value = getRequiredHeader(response, headerName);

  if (Number.isNaN(Date.parse(value))) {
    throw new Error(`Backend вернул некорректную дату в заголовке '${headerName}'.`);
  }

  return value;
}

function parseNonNegativeInteger(value: string, fieldName: string): number {
  const parsedValue = Number(value);

  if (!Number.isSafeInteger(parsedValue) || parsedValue < 0) {
    throw new Error(`Backend вернул некорректное число в поле '${fieldName}'.`);
  }

  return parsedValue;
}

function parseExampleCounts(value: string): CatalogRecognitionDatasetExampleCounts {
  const counts = createEmptyExampleCounts();

  for (const part of value.split(";")) {
    const separatorIndex = part.indexOf("=");

    if (separatorIndex <= 0) {
      throw new Error("Backend вернул некорректную статистику типов датасета.");
    }

    const exampleKind = part.slice(0, separatorIndex).trim();
    const countValue = part.slice(separatorIndex + 1).trim();

    if (!isDatasetExampleKind(exampleKind)) {
      throw new Error(`Backend вернул неизвестный тип примера '${exampleKind}'.`);
    }

    counts[exampleKind] = parseNonNegativeInteger(countValue, exampleKind);
  }

  return counts;
}

function createEmptyExampleCounts(): CatalogRecognitionDatasetExampleCounts {
  return {
    AcceptedSpan: 0,
    AcceptedValue: 0,
    CorrectedValue: 0,
    RejectedSpan: 0,
    RejectedValue: 0,
    ManualValue: 0,
    ConflictResolution: 0,
  };
}

function isDatasetExampleKind(value: string): value is CatalogRecognitionDatasetExampleKind {
  return catalogRecognitionDatasetExampleKinds.some((exampleKind) => exampleKind === value);
}

function buildFallbackFileName(finalizedUntilUtc: string): string {
  const timestamp = new Date(finalizedUntilUtc)
    .toISOString()
    .replaceAll("-", "")
    .replaceAll(":", "")
    .replace(/\.\d{3}Z$/, "Z");

  return `catalog-recognition-dataset-${timestamp}.jsonl`;
}