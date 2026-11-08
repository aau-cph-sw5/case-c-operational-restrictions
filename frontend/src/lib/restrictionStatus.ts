import type { RestrictionStatus } from "../types/restrictionDto";

export const statusLabels: Record<RestrictionStatus, string> = {
  Draft: "Kladde",
  PendingApproval: "Afventer godkendelse",
  PendingDOMApproval: "Afventer DOM-godkendelse",
  PendingControlRoomSupervisorApproval: "Afventer kontrolrumsgodkendelse",
  ReturnedForChanges: "Returneret for ændringer",
  Approved: "Godkendt",
  Active: "Aktiv",
  Archived: "Arkiveret",
  Cancelled: "Annulleret",
  Closing: "Under afslutning",
  OutOfForce: "Ikke længere i kraft",
  AwaitingOperatorSignatures: "Afventer operatørunderskrifter",
  AwaitingOperatorSignOut: "Afventer operatørernes afslutning",
  Unknown: "Ukendt",
};

// Colours from the design guide: Pattens Blue / Mirror Blue, Beeswax / Gold
// Drop (needs attention), Cool Green / Royal Aquamarine (positive), Anvil Grey.
const blue = "bg-[#E0F3FF] text-[#2563EB] border-[#2563EB]";
const amber = "bg-[#FEF3C7] text-[#EF7D00] border-[#EF7D00]";
const green = "bg-[#DAFBEA] text-[#37AB87] border-[#37AB87]";
const grey = "bg-[#EFF6FF] text-[#6B6B6B] border-[#6B6B6B]";

export const statusClasses: Record<RestrictionStatus, string> = {
  Draft: blue,
  PendingApproval: amber,
  PendingDOMApproval: amber,
  PendingControlRoomSupervisorApproval: amber,
  ReturnedForChanges: amber,
  Approved: green,
  Active: green,
  Archived: grey,
  Cancelled: grey,
  Closing: amber,
  OutOfForce: grey,
  AwaitingOperatorSignatures: amber,
  AwaitingOperatorSignOut: amber,
  Unknown: grey,
};

// Backend RestrictionState enum order. ASP.NET serialises enums as numbers
// unless a JsonStringEnumConverter is registered, so accept both forms.
const numericStatuses: RestrictionStatus[] = [
  "Draft",
  "PendingDOMApproval",
  "ReturnedForChanges",
  "Cancelled",
  "Approved",
  "PendingControlRoomSupervisorApproval",
  "Active",
  "Unknown",
  "Closing",
  "OutOfForce",
  "AwaitingOperatorSignatures",
  "AwaitingOperatorSignOut",
  "Archived",
];

const backendStatusAliases: Record<string, RestrictionStatus> = {
  pendingdutyoperationsmanagerapproval: "PendingDOMApproval",
  cancelledrefused: "Cancelled",
  pendingcancellation: "Closing",
  empty: "Unknown",
};

export function normalizeStatus(raw: unknown): RestrictionStatus {
  if (typeof raw === "number") return numericStatuses[raw] ?? "Unknown";

  if (typeof raw === "string") {
    const normalized = raw.toLowerCase();
    if (backendStatusAliases[normalized]) return backendStatusAliases[normalized];

    const match = (Object.keys(statusLabels) as RestrictionStatus[]).find(
      (status) => status.toLowerCase() === normalized,
    );
    return match ?? "Unknown";
  }

  return "Unknown";
}
