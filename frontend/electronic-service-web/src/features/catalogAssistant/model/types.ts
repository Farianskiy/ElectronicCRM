export interface AskCatalogAssistantRequest {
  message: string;
  onlyInStock: boolean;
  minimumScore: number;
  page: number;
  pageSize: number;
}

export interface CatalogAssistantCharacteristic {
  code: string;
  name?: string | null;
  value?: string | null;
  unit?: string | null;
}

export interface CatalogAssistantParsedRequest {
  intent?: string;
  productTypeCode?: string | null;
  manufacturer?: string | null;
  textQuery?: string | null;
  characteristics?: CatalogAssistantCharacteristic[];
  clarification?: CatalogAssistantClarification | null;
}

export interface CatalogAssistantClarification {
  unknownPhrase: string;
  suggestedKind: string;
  suggestedTargetCode: string | null;
  suggestedTargetValue: string;
  confidence: number;
  question: string;
}

export interface CatalogAssistantProduct {
  id: string;
  article?: string | null;
  name: string;
  productType?: string | null;
  productTypeName?: string | null;
  manufacturer?: string | null;
  manufacturerName?: string | null;
  price?: number | null;
  stockQuantity?: number | null;
  score?: number | null;
  characteristics?: CatalogAssistantCharacteristic[];
}

export interface AskCatalogAssistantResponse {
  answer?: string | null;
  needsClarification: boolean;
  parsedRequest?: CatalogAssistantParsedRequest | null;

  // Оставил оба варианта, чтобы frontend пережил небольшие различия в response.
  products?: CatalogAssistantProduct[];
  items?: CatalogAssistantProduct[];

  page?: number;
  pageSize?: number;
  totalCount?: number;

  clarification?: CatalogAssistantClarification | null;
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