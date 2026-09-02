import { httpClient } from "@/shared/api/httpClient";
import type {
  CreateManufacturerFromUnresolvedPhraseRequest,
  CreateManufacturerFromUnresolvedPhraseResponse,
} from "../model/types";

export async function createManufacturerFromUnresolvedPhrase(
  request: CreateManufacturerFromUnresolvedPhraseRequest,
): Promise<CreateManufacturerFromUnresolvedPhraseResponse> {
  const response =
    await httpClient.post<CreateManufacturerFromUnresolvedPhraseResponse>(
      "/api/catalog/manufacturers",
      request,
    );

  return response.data;
}