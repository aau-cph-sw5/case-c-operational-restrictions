// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
import { cn } from "@/lib/utils";
import { statusClasses, statusLabels } from "../lib/restrictionStatus";
import type { RestrictionStatus } from "../types/restrictionDto";

export function RestrictionStatusBadge({
  status,
}: {
  status: RestrictionStatus;
}) {
  return (
    <span
      data-testid="status-badge"
      className={cn(
        "inline-flex items-center rounded-full border px-3 py-0.5 font-['Inter_Variable','Inter',sans-serif] text-base font-medium",
        statusClasses[status],
      )}
    >
      {statusLabels[status]}
    </span>
  );
}
