export interface AskCatalogAssistantRequest {
  message: string;
  onlyInStock: boolean;
  minimumScore: number;
  page: number;
  pageSize: number;
  selectedManufacturer: string | null;
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
  canCreateSuggestion: boolean;
}

export interface CatalogAssistantManufacturerRecognitionCandidate {
  manufacturerName: string;
  rawValue: string;
  normalizedValue: string;
  confidence: number;
  source: string;
  startIndex: number;
  length: number;
  endIndex: number;
}

export interface CatalogAssistantManufacturerRecognition {
  productName: string;
  status: string;
  isResolved: boolean;
  isConflict: boolean;
  selectedCandidate: CatalogAssistantManufacturerRecognitionCandidate | null;
  candidates: CatalogAssistantManufacturerRecognitionCandidate[];
}

export interface CatalogAssistantParsedRequest {
  intent: string;
  search: string | null;
  productTypeCode: string | null;
  manufacturer: string | null;
  characteristics: CatalogAssistantCharacteristic[];
  clarification: CatalogAssistantClarification | null;
  manufacturerRecognition: CatalogAssistantManufacturerRecognition;
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

export interface PreviewCatalogAssistantBatchRequest {
  message: string;
  onlyInStock: boolean;
  matchesPerLine: number;
}

export interface CatalogAssistantBatchLine {
  lineNumber: number;
  sourceText: string;
  searchText: string;
  quantity: number | null;
  status:
    | "Matched"
    | "MultipleMatches"
    | "NotFound"
    | "NeedsClarification"
    | "MissingQuantity"
    | "Invalid";
  message: string;
  manufacturer: string | null;
  characteristics: CatalogAssistantCharacteristic[];
  products: CatalogAssistantProduct[];
}

export interface PreviewCatalogAssistantBatchResponse {
  commonText: string | null;
  totalLines: number;
  matchedLines: number;
  requiresAttentionLines: number;
  lines: CatalogAssistantBatchLine[];
}