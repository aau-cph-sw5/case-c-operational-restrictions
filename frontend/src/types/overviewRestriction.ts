// TYPES / MODELS:
// Types used by the overview page (register of restrictions).
import type { DRStatus } from "@/components/OverviewNotificationCard";
import type { SignatureDto } from "./restrictionDto";
import { BACKEND_RESTRICTION_STATES } from "@/constants/restrictionStates";
// Backend RestrictionState enum, in declaration order. ASP.NET serialises
// enums as their numeric index unless a JsonStringEnumConverter is registered.

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
  originatorSignature: SignatureDto | null;
  signatures: SignatureDto[];
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
