import { Alert, AlertDescription } from "@/components/ui/alert.tsx";
import { OctagonAlert } from "lucide-react";
import type { ReactNode } from "react";

export function ErrorAlert({ children }: { children: ReactNode }) {
  return (
    <Alert className="border-[#EF7D00] bg-[#FEF3C7] font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]">
      <OctagonAlert aria-hidden />
      <AlertDescription className="text-base">{children}</AlertDescription>
    </Alert>
  );
}
