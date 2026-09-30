export type ProductNeedStatus =
  | "Unknown"
  | "Missing"
  | "Included"
  | "NotApplicable";

export interface CompatibleComponent {
  productId: string;
  article: string;
  name: string;
  priceAmount: number;
  priceCurrency: string;
}

export interface SelectedComponent {
  id: string;
  needDefinitionId: string;
  componentProductId: string;
  article: string;
  name: string;
  quantity: number;
  priceAmount: number;
  priceCurrency: string;
  lineTotalAmount: number;
}

export interface ProductNeedCompatibility {
  needDefinitionId: string;
  code: string;
  name: string;
  status: ProductNeedStatus;
  compatibleComponents: CompatibleComponent[];
  selectedComponents: SelectedComponent[];
}

export interface ProductComponentCompatibility {
  productId: string;
  needs: ProductNeedCompatibility[];
}

export interface ComponentNeed {
  id: string;
  mainProductTypeId: string;
  mainProductTypeCode: string;
  mainProductTypeName: string;
  code: string;
  name: string;
}

export interface ComponentOfferSummary {
  id: string;
  needDefinitionId: string;
  needCode: string;
  needName: string;
  mainProductTypeCode: string;
  constraintsCount: number;
}
