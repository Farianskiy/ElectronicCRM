import { httpClient } from "@/shared/api/httpClient";
import { downloadFileResponse } from "@/shared/api/downloadFileResponse";
import type {
  ApplyComponentCompatibilityImportResponse,
  ComponentCompatibilityImportConstraint,
  PreviewComponentCompatibilityImportResponse,
} from "../model/types";

export async function previewComponentCompatibilityImport(
  file: File,
): Promise<PreviewComponentCompatibilityImportResponse> {
  const formData = new FormData();
  formData.append("file", file);

  const response =
    await httpClient.post<PreviewComponentCompatibilityImportResponse>(
      "/api/catalog/component-compatibility/import/preview",
      formData,
    );

  return response.data;
}

export async function previewComponentCompatibilityImportBatch(
  batchId: string,
): Promise<PreviewComponentCompatibilityImportResponse> {
  const response =
    await httpClient.post<PreviewComponentCompatibilityImportResponse>(
      `/api/catalog/component-compatibility/import/batches/${batchId}/preview`,
    );

  return response.data;
}

export async function applyComponentCompatibilityImport(request: {
  rows: Array<{
    componentProductId: string;
    mainProductTypeId: string;
    needDefinitionId: string | null;
    isActive: boolean;
    constraints: ComponentCompatibilityImportConstraint[];
  }>;
  allowRulesWithoutConditions: boolean;
}): Promise<ApplyComponentCompatibilityImportResponse> {
  const response =
    await httpClient.post<ApplyComponentCompatibilityImportResponse>(
      "/api/catalog/component-compatibility/import/apply",
      request,
    );

  return response.data;
}

export async function downloadCurrentComponentCompatibilityRules(
  mainProductTypeCode: string,
  componentManufacturerIds?: string[],
): Promise<string> {
  const params = new URLSearchParams({ mainProductTypeCode });
  for (const manufacturerId of componentManufacturerIds ?? []) {
    params.append("componentManufacturerIds", manufacturerId);
  }

  const response = await httpClient.get<Blob>(
    "/api/catalog/component-compatibility/import/export",
    {
      responseType: "blob",
      params,
    },
  );

  return downloadFileResponse(
    response,
    `component-compatibility-${mainProductTypeCode}${
      componentManufacturerIds?.length ? "-selected-manufacturers" : ""
    }.xlsx`,
  );
}

export async function downloadComponentCompatibilityTemplate(
  mainProductTypeCode?: string,
  characteristicCodes?: string[],
  rangeCharacteristicCodes?: string[],
): Promise<string> {
  const params = new URLSearchParams();
  if (mainProductTypeCode) {
    params.set("mainProductTypeCode", mainProductTypeCode);
  }
  for (const code of characteristicCodes ?? []) {
    params.append("characteristicCodes", code);
  }
  if (characteristicCodes?.length === 0) {
    params.append("characteristicCodes", "");
  }
  for (const code of rangeCharacteristicCodes ?? []) {
    params.append("rangeCharacteristicCodes", code);
  }

  const response = await httpClient.get<Blob>(
    "/api/catalog/component-compatibility/import/template",
    {
      responseType: "blob",
      params,
    },
  );

  return downloadFileResponse(
    response,
    "component-compatibility-template.xlsx",
  );
}
