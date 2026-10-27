// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
// A labelled card in the style of the review screen ("Gennemse og opret"):
// Zumthor background, Mirror Blue border, grey label above a bold value.
import type { ReactNode } from "react";
import { Card, CardContent, CardDescription, CardHeader } from "./ui/card";

export function RestrictionInfoCard({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <Card className="gap-0 rounded-lg border border-[#2563EB] bg-[#EFF6FF] py-2 ring-0">
      <CardHeader className="px-3">
        <CardDescription className="text-[20px] text-[#6B6B6B]">
          {label}
        </CardDescription>
      </CardHeader>
      <CardContent className="px-3 text-[22px] font-bold text-black">
        {children}
      </CardContent>
    </Card>
  );
}