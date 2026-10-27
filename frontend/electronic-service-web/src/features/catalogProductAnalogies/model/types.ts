export interface CatalogProductAnalogyMatch {
  productId: string;
  article: string;
  name: string;
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  matchedName: string;
  exactArticleMatch: boolean;
  exactNameMatch: boolean;
  sharedNameTokenCount: number;
  queryNameTokenCoverage: number;
  matchingCharacteristicCount: number;
  conflictingCharacteristicCount: number;
  sharedNameTokens: string[];
  matchingCharacteristicCodes: string[];
  conflictingCharacteristicCodes: string[];
  characteristics: Record<string, string>;
  matchedFamilyAnchor?: string | null;
  candidateFamilyAnchors: string[];
}

export interface CatalogProductAnalogyCandidateExclusion {
  productId: string;
  article: string;
  name: string;
  reasonCode: string;
  reason: string;
  candidateFamilyAnchors: string[];
  sharedNameTokens: string[];
  matchingCharacteristicCount: number;
  conflictingCharacteristicCount: number;
}

export interface CatalogProductAnalogiesPreview {
  candidateSnapshotId: string;
  candidateSnapshotCreatedAtUtc: string;
  evaluatedCandidatesCount: number;
  matches: CatalogProductAnalogyMatch[];
  excludedCandidatesCount: number;
  exclusionDiagnosticsTruncated: boolean;
  excludedCandidates: CatalogProductAnalogyCandidateExclusion[];
  characteristicProposals: CatalogProductAnalogyCharacteristicProposal[];
  productTypeProposals: CatalogProductAnalogyProductTypeProposal[];
}

export interface CatalogProductAnalogyProductTypeProposal {
  productTypeId: string;
  productTypeCode: string;
  productTypeName: string;
  supportingProductCount: number;
  eligibleProductCount: number;
  supportRatio: number;
  isSuggested: boolean;
  blockingReasons: string[];
  supportingProductIds: string[];
}

export interface CatalogProductAnalogyCharacteristicProposal {
  characteristicCode: string;
  proposedValue: string;
  supportingProductCount: number;
  productsWithCharacteristicCount: number;
  supportRatio: number;
  isSuggested: boolean;
  blockingReasons: string[];
  supportingProductIds: string[];
  alternativeValues: Record<string, number>;
  feedbackId?: string | null;
  mode: "Off" | "Shadow" | "Suggest" | "AutoFill";
  isAutoFillAllowed: boolean;
  autoFillBlockingReason?: string | null;
}

export interface PreviewCatalogProductAnalogiesParameters {
  productName: string;
  article?: string | null;
  manufacturerId: string;
  productTypeId?: string | null;
  knownCharacteristics: Record<string, string>;
  maximumResults?: number;
  importBatchId?: string | null;
  importRowId?: string | null;
}
