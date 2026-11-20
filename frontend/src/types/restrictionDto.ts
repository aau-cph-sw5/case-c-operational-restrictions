// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
export type RestrictionStatus =
  | "Draft"
  | "PendingApproval"
  | "PendingDOMApproval"
  | "PendingControlRoomSupervisorApproval"
  | "ReturnedForChanges"
  | "Approved"
  | "Active"
  | "Archived"
  | "Cancelled"
  | "Closing"
  | "OutOfForce"
  | "AwaitingOperatorSignatures"
  | "AwaitingOperatorSignOut"
  | "Unknown";

// Coarse device class the server recorded with a signature.
export type DeviceClass = "Phone" | "Tablet" | "Desktop" | "Unknown";

export interface SignatureDto {
  signeeId: string;
  // Optional: the backend may only return the id. The UI falls back to the id.
  signeeName?: string;
  signeeRole: string;
  signedAt: string;
  deviceClass?: DeviceClass;
}

// Answer to "did my signing attempt leave a signature?" after a lost connection.
export interface SigningAttemptStatusDto {
  status: "Signed" | "NotSigned";
  signature?: SignatureDto;
}

export interface RestrictionDto {
  id: string;
  title?: string;
  originatorUserId: string;
  status: RestrictionStatus;
  reviewDescription?: string | null;
  startDate: string;
  endDate: string | null;
  restrictionDescription: string;
  reference?: string | null;
  situationDescription?: string;
  cancellationDescription?: string;
  signatures: SignatureDto[];
}
