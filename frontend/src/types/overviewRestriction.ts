// TYPES / MODELS:
// Types used by the overview page (register of restrictions).
import type { DRStatus } from "@/components/OverviewNotificationCard";

// Backend RestrictionState enum, in declaration order. ASP.NET serialises
// enums as their numeric index unless a JsonStringEnumConverter is registered.
export const BACKEND_RESTRICTION_STATES = [
  "Draft",
  "PendingDutyOperationsManagerApproval",
  "ReturnedForChanges",
  "CancelledRefused",
  "Approved",
  "PendingControlRoomSupervisorApproval",
  "Active",
  "Empty",
  "PendingCancellation",
  "OutOfForce",
  "AwaitingOperatorSignatures",
  "AwaitingOperatorSignOut",
  "Archived",
] as const;

export type BackendRestrictionState =
  (typeof BACKEND_RESTRICTION_STATES)[number] | "Unknown";

// Subset of the backend OperationalRestrictionDto that the overview needs.
export interface OverviewRestrictionDto {
  id: string;
  title: string;
  reference: string | null;
  state: number | string;
  startDate: string;
  endDate: string | null;
  originatorSignature: unknown | null;
  signatures: unknown[];
}

export type OverviewCategory = "kladde" | "alle" | "aktiv" | "arkiv";

// View model rendered by ORNotificationCard.
export interface DrItem {
  id: string;
  referenceId: string;
  statusKey: DRStatus;
  title: string;
  date: string;
  signed: boolean;
  category: OverviewCategory;
}
