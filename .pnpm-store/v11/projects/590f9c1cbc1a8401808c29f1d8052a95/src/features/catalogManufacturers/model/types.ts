export interface CreateApprovedManufacturerAliasRequest {
  manufacturerId: string;
  phrase: string;
}

export interface CreateApprovedManufacturerAliasResponse {
  manufacturerAliasId: string;
  manufacturerId: string;
  manufacturerName: string;
  phrase: string;
  normalizedPhrase: string;
  status: string;
  source: string;
}

export interface CreateManufacturerFromUnresolvedPhraseRequest {
  canonicalName: string;
  sourcePhrase: string;
}

export type CreatedManufacturerResolutionSource =
  | "ExactName"
  | "ApprovedAlias";

export interface CreateManufacturerFromUnresolvedPhraseResponse {
  manufacturerId: string;
  manufacturerName: string;
  normalizedManufacturerName: string;
  manufacturerAliasId: string | null;
  aliasPhrase: string | null;
  normalizedAliasPhrase: string | null;
  aliasStatus: string | null;
  aliasSource: string | null;
  resolutionSource: CreatedManufacturerResolutionSource;
}

export interface MarkManufacturerPhraseAsNoiseRequest {
  phrase: string;
  reason?: string | null;
}

export type MarkManufacturerPhraseAsNoiseAction =
  | "Created"
  | "Reactivated"
  | "AlreadyActive";

export interface MarkManufacturerPhraseAsNoiseResponse {
  manufacturerNoisePhraseId: string;
  phrase: string;
  normalizedPhrase: string;
  reason?: string | null;
  isActive: boolean;
  createdByUserId: string;
  updatedByUserId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  deactivatedAtUtc?: string | null;
  action: MarkManufacturerPhraseAsNoiseAction;
}