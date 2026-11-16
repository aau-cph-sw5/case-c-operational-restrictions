import { Alert, AlertDescription } from "@/components/ui/alert.tsx";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { OctagonAlert } from "lucide-react";

export function RequestedChangesNotice({ comment }: { comment: string }) {
  return (
    <Alert
      data-testid="requested-changes-notice"
      className="border-[#EF7D00] bg-[#FEF3C7] font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]"
    >
      <OctagonAlert aria-hidden />
      <AlertDescription className="text-base">
        <p className="font-bold">{t.requestedChanges}</p>
        <p>
          {t.requestedChangesComment}: {comment}
        </p>
      </AlertDescription>
    </Alert>
  );
}
