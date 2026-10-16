import { httpClient } from "@/shared/api/httpClient";
import type {
  ComponentCompatibilityComparisonOperator,
  ComponentNeedRequirementMode,
  ComponentNeed,
  ComponentOfferSummary,
  ComponentCompatibilityRule,
  ComponentCompatibilityRuleGroupInput,
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
  name: string,
  requirementMode: ComponentNeedRequirementMode,
  defaultQuantityPerUnit: number,
): Promise<ComponentNeed> {
  const response = await httpClient.post<ComponentNeed>(
    `/api/catalog/component-compatibility/product-types/${encodeURIComponent(mainProductTypeCode)}/needs`,
    { name, requirementMode, defaultQuantityPerUnit },
  );

  return response.data;
}

export async function updateComponentNeed(
  needDefinitionId: string,
  requirementMode: ComponentNeedRequirementMode,
  defaultQuantityPerUnit: number,
): Promise<ComponentNeed> {
  const response = await httpClient.put<ComponentNeed>(
    `/api/catalog/component-compatibility/needs/${encodeURIComponent(needDefinitionId)}`,
    { requirementMode, defaultQuantityPerUnit },
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
    comparisonOperator?: ComponentCompatibilityComparisonOperator;
  }>,
): Promise<void> {
  await httpClient.post(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(componentProductId)}/offers`,
    { needDefinitionId, constraints },
  );
}

export async function getComponentCompatibilityRules(
  mainProductTypeCode?: string,
): Promise<ComponentCompatibilityRule[]> {
  const response = await httpClient.get<ComponentCompatibilityRule[]>(
    "/api/catalog/component-compatibility/rules",
    {
      params: mainProductTypeCode ? { mainProductTypeCode } : undefined,
    },
  );

  return response.data;
}

export async function updateComponentCompatibilityRule(
  offerId: string,
  isActive: boolean,
  conditionGroups: ComponentCompatibilityRuleGroupInput[],
): Promise<ComponentCompatibilityRule> {
  const response = await httpClient.put<ComponentCompatibilityRule>(
    `/api/catalog/component-compatibility/offers/${encodeURIComponent(offerId)}`,
    { isActive, conditionGroups },
  );

  return response.data;
}

export async function deleteComponentCompatibilityRule(
  offerId: string,
): Promise<void> {
  await httpClient.delete(
    `/api/catalog/component-compatibility/offers/${encodeURIComponent(offerId)}`,
  );
}
