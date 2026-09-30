import { httpClient } from "@/shared/api/httpClient";
import type { ProductNeedStatus } from "../model/types";

export async function setProductNeedStatus(
  productId: string,
  needDefinitionId: string,
  status: ProductNeedStatus,
): Promise<void> {
  await httpClient.put(
    `/api/catalog/component-compatibility/products/${encodeURIComponent(productId)}/needs/${encodeURIComponent(needDefinitionId)}`,
    { status },
  );
}
