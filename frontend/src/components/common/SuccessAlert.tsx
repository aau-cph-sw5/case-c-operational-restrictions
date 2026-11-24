import { Alert, AlertDescription } from "@/components/ui/alert.tsx";
import { CheckCircle2 } from "lucide-react";
import type { ReactNode } from "react";

export function SuccessAlert({
  children,
  testId,
}: {
  children: ReactNode;
  testId?: string;
}) {
  return (
    <Alert
      data-testid={testId}
      className="border-[#37AB87] bg-[#DAFBEA] font-['Inter_Variable','Inter',sans-serif] text-base text-[#1E745B]"
    >
      <CheckCircle2 className="size-5 text-[#37AB87]" aria-hidden />
      <AlertDescription className="text-base text-[#1E745B]">
        {children}
      </AlertDescription>
    </Alert>
  );
}
