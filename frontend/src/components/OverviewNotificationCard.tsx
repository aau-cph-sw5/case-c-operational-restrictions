import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { formatDayMonthYear } from "@/lib/dateFormat";
import type { SignatureStatusDto } from "@/types/restrictionDto";

export type DRStatus = "aktiv" | "afventer_deaktivering" | "arkiv" | "kladde";

interface StatusStyle {
  bgHex: string;
  borderColor: string;
  statusLabel: string;
  statusTextColor: string;
}

// Color configurations per status
export const STATUS_STYLES: Record<DRStatus, StatusStyle> = {
  aktiv: {
    bgHex: "#DAFBEA", // Active green
    borderColor: "border-[#A7F3D0]",
    statusLabel: "Aktiv",
    statusTextColor: "text-[#065F46]",
  },
  afventer_deaktivering: {
    bgHex: "#FEF3C7", // Awaiting deactivation yellow
    borderColor: "border-[#FDE68A]",
    statusLabel: "Afventer deaktivering",
    statusTextColor: "text-[#92400E]",
  },
  arkiv: {
    bgHex: "#EFF6FF", // Archived blue
    borderColor: "border-[#BFDBFE]",
    statusLabel: "Arkiveret",
    statusTextColor: "text-[#1E40AF]",
  },
  kladde: {
    bgHex: "#EFF6FF", // Draft blue
    borderColor: "border-[#BFDBFE]",
    statusLabel: "Kladde",
    statusTextColor: "text-[#1E40AF]",
  },
};

export interface ORNotificationCardProps {
  statusKey: DRStatus;
  title: string;
  date: string;
  signatureStatuses?: SignatureStatusDto[];
  onSeMere?: () => void;
}

export function ORNotificationCard({
  statusKey,
  title,
  date,
  signatureStatuses = [],
  onSeMere,
}: ORNotificationCardProps) {
  const style = STATUS_STYLES[statusKey] || STATUS_STYLES.arkiv;
  const buttonText = statusKey === "kladde" ? "Rediger" : "Se mere";
  const totalSignatures = signatureStatuses.length;

  const signedCount = signatureStatuses.filter(
    (signature) => signature.status === "CompletedSignature",
  ).length;

  return (
    <Card
      style={{ backgroundColor: style.bgHex }}
      className={cn(
        "rounded-xl border shadow-xs py-3.5 px-4 gap-2.5 transition-all hover:shadow-sm",
        style.borderColor,
      )}
    >
      <div className="flex items-center justify-between text-xs font-medium">
        <span className="text-muted-foreground font-semibold">
          Dato: {formatDayMonthYear(date)}
        </span>

        {signatureStatuses.length === 0 ? (
          <span className="rounded-full px-2.5 py-0.5 font-semibold bg-gray-200 text-gray-700">
            Underskriftsstatus ukendt
          </span>
        ) : (
          <span
            className={cn(
              "rounded-full px-2.5 py-0.5 font-semibold",
              signatureStatuses.every(
                (signature) => signature.status === "CompletedSignature",
              )
                ? "bg-green-200 text-green-900"
                : "bg-red-200 text-red-900",
            )}
          >
            {signatureStatuses.every(
              (signature) => signature.status === "CompletedSignature",
            )
              ? "Alle underskrevet"
              : "Mangler underskrifter"}
          </span>
        )}

        <span className={cn("font-medium", style.statusTextColor)}>
          {style.statusLabel}
        </span>
      </div>
      {/* MET-C-017: Signature completeness summary */}
      {totalSignatures > 0 && (
        <div className="flex items-center gap-1.5 text-xs text-[#6B6B6B]">
          <span>
            {signedCount} af {totalSignatures} underskrevet
          </span>
        </div>
      )}
      <div className="flex items-center justify-between gap-3">
        <h3 className="font-heading text-base md:text-lg font-bold text-foreground leading-snug">
          {title}
        </h3>
        {/* Action Button using dark blue hex #2563EB */}
        <Button
          onClick={onSeMere}
          className="shrink-0 rounded-lg bg-[#2563EB] hover:bg-[#2563EB]/90 text-white font-semibold px-4 h-9 shadow-xs cursor-pointer"
        >
          {buttonText}
        </Button>
      </div>
    </Card>
  );
}
