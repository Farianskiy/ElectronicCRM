import { httpClient } from "@/shared/api/httpClient";
import type {
  ComponentNeed,
  ComponentOfferSummary,
} from "../model/types";

export async function getComponentNeeds(
  mainProductTypeCode?: string,
): Promise<ComponentNeed[]> {
  const response = await httpClient.get<ComponentNeed[]>(
    "/api/catalog/component-compatibility/needs",
    {
      params: mainProductTypeCode ? { mainProductTypeCode } : undefined,
    },
  );

  return response.data;
}

export async function createComponentNeed(
  mainProductTypeCode: string,
  code: string,
  name: string,
): Promise<ComponentNeed> {
  const response = await httpClient.post<ComponentNeed>(
    `/api/catalog/component-compatibility/product-types/${encodeURIComponent(mainProductTypeCode)}/needs`,
    { code, name },
  );

  return response.data;
}

export async function getComponentOffers(
  componentProductId: string,
): Promise<ComponentOfferSummary[]> {
  const response = await httpClient.get<ComponentOfferSummary[]>(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(componentProductId)}/offers`,
  );

  return response.data;
}

export async function createComponentOffer(
  componentProductId: string,
  needDefinitionId: string,
  constraints: Array<{
    characteristicDefinitionId: string;
    value: string;
  }>,
): Promise<void> {
  await httpClient.post(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(componentProductId)}/offers`,
    { needDefinitionId, constraints },
  );
}
