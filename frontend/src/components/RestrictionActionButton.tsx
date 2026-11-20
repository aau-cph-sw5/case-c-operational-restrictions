// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
// "Operational Restriction Button" from the design guide: a shadcn Button
// styled as a Mirror Blue pill with a bold white label and an icon
// (e.g. Underskriv with a pencil).
import { cn } from "@/lib/utils";
import { LoaderCircle } from "lucide-react";
import type { ComponentProps, ReactNode } from "react";
import { Button } from "./ui/button";

interface RestrictionActionButtonProps extends ComponentProps<typeof Button> {
  icon?: ReactNode;
  loading?: boolean;
  loadingLabel?: string;
}

export function RestrictionActionButton({
  icon,
  loading = false,
  loadingLabel,
  children,
  disabled,
  className,
  ...props
}: RestrictionActionButtonProps) {
  return (
    <Button
      disabled={disabled || loading}
      className={cn(
        // min-h-11 keeps the 44px tap target; the label may wrap on a narrow screen.
        "h-auto min-h-11 max-w-full gap-2 rounded-full bg-[#2563EB] px-5 py-2 text-center font-['Inter_Variable','Inter',sans-serif] text-base font-bold whitespace-normal text-white hover:bg-[#2563EB]/90",
        className,
      )}
      {...props}
    >
      {loading ? (
        <>
          <LoaderCircle className="animate-spin" aria-hidden />
          {loadingLabel ?? children}
        </>
      ) : (
        <>
          {children}
          {icon}
        </>
      )}
    </Button>
  );
}
