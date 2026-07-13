export interface AskCatalogAssistantRequest {
  message: string;
  onlyInStock: boolean;
  minimumScore: number;
  page: number;
  pageSize: number;
}

export interface CatalogAssistantCharacteristic {
  code: string;
  value: string;
}

export interface CatalogAssistantClarification {
  unknownPhrase: string;
  suggestedKind: string;
  suggestedTargetCode: string | null;
  suggestedTargetValue: string;
  confidence: number;
  question: string;
}

export interface CatalogAssistantParsedRequest {
  intent: string;
  search: string | null;
  productTypeCode: string | null;
  manufacturer: string | null;
  characteristics: CatalogAssistantCharacteristic[];
  clarification: CatalogAssistantClarification | null;
}

export interface CatalogAssistantProduct {
  id: string;
  article: string;
  name: string;
  productTypeCode: string;
  productTypeName: string;
  manufacturerName: string;
  priceAmount: number;
  priceCurrency: string;
  stockQuantity: number;
}

export interface CatalogAssistantReplacement extends CatalogAssistantProduct {
  replacementScore: number;
}

export interface AskCatalogAssistantResponse {
  intent: string;
  needsClarification: boolean;
  answer: string;
  parsedRequest: CatalogAssistantParsedRequest;
  products: CatalogAssistantProduct[];
  sourceProduct: CatalogAssistantProduct | null;
  replacements: CatalogAssistantReplacement[];
}

export interface CreateDictionarySuggestionRequest {
  originalMessage: string;
  unknownPhrase: string;
  suggestedKind: string;
  suggestedTargetCode: string | null;
  suggestedTargetValue: string;
  confidence: number;
}

export interface CreateDictionarySuggestionResponse {
  id: string;
  status: string;
  message: string;
}