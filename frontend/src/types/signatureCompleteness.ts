// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)

export interface SignatureCellDto {
  restrictionId: string;
  isSigned: boolean;
  signedAt: string | null;
}

export interface OperatorSignatureStatusDto {
  operatorId: string;
  name: string;
  cells: SignatureCellDto[];
}

export interface RestrictionInForceDto {
  restrictionId: string;
  title: string;
  reference: string | null;
}

export interface SignatureCompletenessReportDto {
  computedAt: string;
  outstandingSignatureCount: number;
  restrictions: RestrictionInForceDto[];
  operators: OperatorSignatureStatusDto[];
}
