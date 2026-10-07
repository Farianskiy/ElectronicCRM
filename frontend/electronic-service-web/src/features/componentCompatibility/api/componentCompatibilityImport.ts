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

export async function applyComponentCompatibilityImport(rows: Array<{
  componentProductId: string;
  needDefinitionId: string;
  constraints: ComponentCompatibilityImportConstraint[];
}>): Promise<ApplyComponentCompatibilityImportResponse> {
  const response =
    await httpClient.post<ApplyComponentCompatibilityImportResponse>(
      "/api/catalog/component-compatibility/import/apply",
      { rows },
    );

  return response.data;
}

export async function downloadComponentCompatibilityTemplate(): Promise<string> {
  const response = await httpClient.get<Blob>(
    "/api/catalog/component-compatibility/import/template",
    { responseType: "blob" },
  );

  return downloadFileResponse(
    response,
    "component-compatibility-template.xlsx",
  );
}
