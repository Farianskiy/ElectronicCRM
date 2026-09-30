import { httpClient } from "@/shared/api/httpClient";
import type { SelectedComponent } from "../model/types";

export async function selectProductComponent(
  productId: string,
  needDefinitionId: string,
  componentProductId: string,
  quantity: number,
): Promise<SelectedComponent> {
  const response = await httpClient.post<SelectedComponent>(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(productId)}/selected-components`,
    { needDefinitionId, componentProductId, quantity },
  );

  return response.data;
}

export async function changeSelectedComponentQuantity(
  productId: string,
  selectionId: string,
  quantity: number,
): Promise<SelectedComponent> {
  const response = await httpClient.put<SelectedComponent>(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(productId)}/selected-components/${encodeURIComponent(selectionId)}`,
    { quantity },
  );

  return response.data;
}

export async function removeSelectedComponent(
  productId: string,
  selectionId: string,
): Promise<void> {
  await httpClient.delete(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(productId)}/selected-components/${encodeURIComponent(selectionId)}`,
  );
}
