// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
// A labelled card in the style of the review screen ("Gennemse og opret"):
// Zumthor background, Mirror Blue border, grey label above a bold value.
import type { ReactNode } from "react";

export function RestrictionInfoCard({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="rounded-lg border border-[#2563EB] bg-[#EFF6FF] px-3 py-2">
      <p className="text-[20px] text-[#6B6B6B]">{label}</p>
      <div className="text-[22px] font-bold text-black">{children}</div>
    </div>
  );
}
