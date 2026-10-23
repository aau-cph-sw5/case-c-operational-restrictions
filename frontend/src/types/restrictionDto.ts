// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
export type RestrictionStatus =
  | "Draft"
  | "PendingApproval"
  | "PendingDOMApproval"
  | "Approved"
  | "Active"
  | "Archived"
  | "Cancelled"
  | "Closing"
  | "Unknown";

export interface SignatureDto {
  signeeId: string;
  signeeName?: string;
  signeeRole: string;
  signedAt: string;
}

export interface RestrictionDto {
  id: string;
  originatorUserId: string;
  status: RestrictionStatus;
  startDate: string;
  endDate: string;
  restrictionDescription: string;
  signatures: SignatureDto[];
}
