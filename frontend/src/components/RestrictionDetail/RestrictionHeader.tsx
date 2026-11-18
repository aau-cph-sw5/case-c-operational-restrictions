import { RestrictionStatusBadge } from "@/components/RestrictionStatusBadge.tsx";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import type { RestrictionDto } from "@/types/restrictionDto.ts";

export function RestrictionHeader({
  restriction,
}: {
  restriction: RestrictionDto;
}) {
  return (
    <header className="space-y-1">
      <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#2563EB]">
        {t.screenLabel}
      </p>
      <h1 className="font-['Instrument_Sans',sans-serif] text-2xl font-bold break-all text-black">
        {restriction.id}
      </h1>
      {restriction.reference && (
        <p className="font-['Instrument_Sans',sans-serif] text-base text-[#6B6B6B]">
          Ref: {restriction.reference}
        </p>
      )}
      <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]">
        {t.idLabel}
      </p>
      <div className="pt-1">
        <RestrictionStatusBadge status={restriction.status} />
      </div>
    </header>
  );
}
