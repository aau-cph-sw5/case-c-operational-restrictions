import type { DRStatus } from "@/components/OverviewNotificationCard";
import type { RestrictionDto } from "@/types/restrictionDto";

export interface DrItem {
  id: string;
  referenceId: string;
  statusKey: DRStatus;
  title: string;
  date: string;
  category: "kladde" | "alle" | "aktiv" | "arkiv";
}

export function toDrItem(restriction: RestrictionDto): DrItem {
  const status = restriction.status;

  const category: DrItem["category"] =
    status === "Draft" || status === "ReturnedForChanges"
      ? "kladde"
      : status === "Archived" ||
          status === "Cancelled" ||
          status === "OutOfForce"
        ? "arkiv"
        : "aktiv";

  const statusKey: DRStatus =
    category === "kladde"
      ? "kladde"
      : category === "arkiv"
        ? "arkiv"
        : status === "Closing" || status === "AwaitingOperatorSignOut"
          ? "afventer_deaktivering"
          : "aktiv";

  return {
    id: restriction.id,
    referenceId: restriction.reference ?? restriction.id,
    statusKey,
    title: restriction.restrictionDescription,
    date: restriction.startDate,
    category,
  };
}
