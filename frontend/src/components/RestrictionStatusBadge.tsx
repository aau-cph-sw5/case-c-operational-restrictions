// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
import { cn } from "@/lib/utils";
import { Badge } from "./ui/badge";
import { statusClasses, statusLabels } from "../lib/restrictionStatus";
import type { RestrictionStatus } from "../types/restrictionDto";

export function RestrictionStatusBadge({
  status,
}: {
  status: RestrictionStatus;
}) {
  return (
    <Badge
      variant="outline"
      data-testid="status-badge"
      className={cn(
        "h-auto px-3 py-0.5 font-['Inter_Variable','Inter',sans-serif] text-base",
        statusClasses[status],
      )}
    >
      {statusLabels[status]}
    </Badge>
  );
}
