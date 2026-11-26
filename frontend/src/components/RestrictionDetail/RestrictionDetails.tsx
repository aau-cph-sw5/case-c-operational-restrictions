import { RestrictionInfoCard } from "@/components/RestrictionInfoCard.tsx";
import { formatLocal, formatUtc } from "@/lib/dateFormat.ts";
import { normalizeStatus, statusLabels } from "@/lib/restrictionStatus.ts";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import type { RestrictionDto } from "@/types/restrictionDto.ts";

const historyActionLabels: Record<string, string> = {
  CreateDraft: "Kladde oprettet",
  PendingDutyOperationsManagerApproval: "Indsendt til godkendelse",
  ReturnedForChanges: "Returneret til originator med ændringsanmodning",
  Resubmit: "Genindsendt til godkendelse",
  Approved: "Godkendt af Duty Operations Manager",
  PendingControlRoomSupervisorApproval: "Sendt videre til implementering",
  CancelledRefused: "Afvist",
  OriginatorStartCancellation: "Annullering anmodet af originator",
  DutyOperationsManagerApproveCancellation:
    "Annullering godkendt af Duty Operations Manager",
  ControlRoomCancellation: "Taget ud af kraft af kontrolrummet",
};

export function RestrictionDetails({
  restriction,
}: {
  restriction: RestrictionDto;
}) {
  return (
    <section aria-label="Detaljer" className="space-y-2">
      {restriction.title && (
        <RestrictionInfoCard label={t.title}>
          <p data-testid="restriction-title">{restriction.title}</p>
        </RestrictionInfoCard>
      )}
      {restriction.situationDescription && (
        <RestrictionInfoCard label={t.situation}>
          <p className="whitespace-pre-wrap">
            {restriction.situationDescription}
          </p>
        </RestrictionInfoCard>
      )}

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

      {restriction.cancellationDescription && (
        <RestrictionInfoCard label={t.cancellationDescription}>
          <p className="whitespace-pre-wrap">
            {restriction.cancellationDescription}
          </p>
        </RestrictionInfoCard>
      )}

      <section
        aria-labelledby="restriction-history-heading"
        className="space-y-2"
      >
        <h2
          id="restriction-history-heading"
          className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]"
        >
          {t.historyHeading}
        </h2>
        {(restriction.history ?? []).length === 0 ? (
          <p className="text-[#6B6B6B]">{t.historyEmpty}</p>
        ) : (
          <ol className="space-y-2">
            {(restriction.history ?? []).map((event, index) => (
              <li
                key={`${event.action}-${event.occurredAt}-${index}`}
                data-testid="restriction-history-entry"
                className="rounded-lg border border-[#2563EB] bg-[#EFF6FF] p-3"
              >
                <p className="font-bold">
                  {historyActionLabels[event.action] ?? event.action}
                </p>
                <p className="text-sm text-[#6B6B6B]">
                  {t.historyActor}: {event.actorId}
                </p>
                <p className="text-sm text-[#6B6B6B]">
                  {event.fromState
                    ? statusLabels[normalizeStatus(event.fromState)]
                    : t.historyCreated}
                  {" → "}
                  {statusLabels[normalizeStatus(event.toState)]}
                </p>
                <p className="text-sm text-[#6B6B6B]">
                  {formatUtc(event.occurredAt)}
                </p>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
