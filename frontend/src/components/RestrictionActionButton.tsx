// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
// "Operational Restriction Button" from the design guide: Mirror Blue pill,
// bold white label and an icon (e.g. Underskriv with a pencil).
import { LoaderCircle } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";

interface RestrictionActionButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
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
  ...props
}: RestrictionActionButtonProps) {
  return (
    <button
      type="button"
      disabled={disabled || loading}
      className="inline-flex h-11 items-center justify-center gap-2 rounded-full bg-[#2563EB] px-5 font-['Inter_Variable','Inter',sans-serif] text-base font-bold text-white transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
      {...props}
    >
      {loading ? (
        <>
          <LoaderCircle className="size-4 animate-spin" aria-hidden />
          {loadingLabel ?? children}
        </>
      ) : (
        <>
          {children}
          {icon}
        </>
      )}
    </button>
  );
}
