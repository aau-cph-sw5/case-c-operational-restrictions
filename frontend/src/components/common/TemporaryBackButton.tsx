import { Button } from "@/components/ui/button.tsx";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { ChevronLeft } from "lucide-react";

// TEMPORARY: stands in for the app header's back chevron until the shared
// header/navigation exists.
import { useNavigate } from "react-router-dom";

export function TemporaryBackButton() {
  const navigate = useNavigate();
  return (
    <Button
      variant="ghost"
      onClick={() => navigate("/home")}
      className="-ml-2 gap-1 self-start font-['Inter_Variable','Inter',sans-serif] text-base font-medium text-[#2563EB] hover:text-[#2563EB]"
    >
      <ChevronLeft aria-hidden />
      {t.back}
    </Button>
  );
}
