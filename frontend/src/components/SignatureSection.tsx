// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
import { formatUtc } from "../lib/dateFormat";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import type { SignatureDto } from "../types/restrictionDto";
import { RestrictionInfoCard } from "./RestrictionInfoCard";

interface SignatureSectionProps {
  signatures: SignatureDto[];
  // Used as a display-name fallback when the signature only carries an id.
  knownUsers?: Record<string, string>;
}

export function SignatureSection({
  signatures,
  knownUsers = {},
}: SignatureSectionProps) {
  return (
    <section aria-labelledby="signatures-heading" className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <h2
          id="signatures-heading"
          className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]"
        >
          {t.signaturesHeading}
        </h2>
        {signatures.length === 0 && (
          <span className="inline-flex items-center rounded-full border border-[#EF7D00] bg-[#FEF3C7] px-3 py-0.5 font-['Inter_Variable','Inter',sans-serif] text-base font-medium text-[#EF7D00]">
            {t.unsignedDraft}
          </span>
        )}
      </div>

      {signatures.length === 0 ? (
        <p className="text-[#6B6B6B]">{t.notSignedYet}</p>
      ) : (
        <ul className="space-y-2">
          {signatures.map((signature) => (
            <li
              key={`${signature.signeeId}-${signature.signedAt}`}
              data-testid="signature-block"
            >
              <RestrictionInfoCard label={t.signedBy}>
                <p>
                  {signature.signeeName ??
                    knownUsers[signature.signeeId] ??
                    signature.signeeId}
                </p>
                <p className="mt-1 text-[20px] font-normal text-[#6B6B6B]">
                  {t.role}:{" "}
                  <span className="text-black">{signature.signeeRole}</span>
                </p>
                <p className="text-[20px] font-normal text-[#6B6B6B]">
                  {t.signedAt}:{" "}
                  <span className="text-black">
                    {formatUtc(signature.signedAt)}
                  </span>
                </p>
              </RestrictionInfoCard>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
