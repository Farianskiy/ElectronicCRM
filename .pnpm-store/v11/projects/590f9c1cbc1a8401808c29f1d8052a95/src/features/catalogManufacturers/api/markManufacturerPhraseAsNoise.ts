import { httpClient } from "@/shared/api/httpClient";
import type {
  MarkManufacturerPhraseAsNoiseRequest,
  MarkManufacturerPhraseAsNoiseResponse,
} from "../model/types";

export async function markManufacturerPhraseAsNoise(
  request: MarkManufacturerPhraseAsNoiseRequest,
): Promise<MarkManufacturerPhraseAsNoiseResponse> {
  const response =
    await httpClient.post<MarkManufacturerPhraseAsNoiseResponse>(
      "/api/catalog/manufacturer-noise-phrases",
      request,
    );

  return response.data;
}