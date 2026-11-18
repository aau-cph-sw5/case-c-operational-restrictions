import { RestrictionInfoCard } from "@/components/RestrictionInfoCard.tsx";
import { formatLocal, formatUtc } from "@/lib/dateFormat.ts";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import type { RestrictionDto } from "@/types/restrictionDto.ts";

export function RestrictionDetails({
  restriction,
}: {
  restriction: RestrictionDto;
}) {
  return (
    <section aria-label="Detaljer" className="space-y-2">
      <RestrictionInfoCard label={t.startDate}>
        <p data-testid="start-local">{formatLocal(restriction.startDate)}</p>
        <p
          data-testid="start-utc"
          className="text-[20px] font-normal text-[#6B6B6B]"
        >
          {formatUtc(restriction.startDate)}
        </p>
      </RestrictionInfoCard>

      <RestrictionInfoCard label={t.endDate}>
        <p data-testid="end-local">
          {restriction.endDate ? formatLocal(restriction.endDate) : t.noEndDate}
        </p>
        {restriction.endDate && (
          <p
            data-testid="end-utc"
            className="text-[20px] font-normal text-[#6B6B6B]"
          >
            {formatUtc(restriction.endDate)}
          </p>
        )}
      </RestrictionInfoCard>

      <RestrictionInfoCard label={t.description}>
        <p className="whitespace-pre-wrap">
          {restriction.restrictionDescription || t.noDescription}
        </p>
      </RestrictionInfoCard>
    </section>
  );
}
