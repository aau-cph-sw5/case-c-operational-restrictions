import type { RestrictionStatus } from "../types/restrictionDto";

export const statusLabels: Record<RestrictionStatus, string> = {
  Draft: "Kladde",
  PendingApproval: "Afventer godkendelse",
  PendingDOMApproval: "Afventer DOM-godkendelse",
  PendingControlRoomSupervisorApproval: "Afventer kontrolrumschef",
  Approved: "Godkendt",
  Active: "Aktiv",
  Archived: "Arkiveret",
  Cancelled: "Annulleret",
  Closing: "Under afslutning",
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
  Approved: green,
  Active: green,
  Archived: grey,
  Cancelled: grey,
  Closing: amber,
  Unknown: grey,
};
