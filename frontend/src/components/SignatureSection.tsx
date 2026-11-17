// COMPONENT:
// Reusable UI building blocks (e.g. Buttons, Cards)
import { formatUtc } from "../lib/dateFormat";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import type { SignatureDto } from "../types/restrictionDto";
import { RestrictionInfoCard } from "./RestrictionInfoCard";
import { Badge } from "./ui/badge";
import type { SignatureStatusDto } from "../services/restrictionApi";
import { CheckCircle2, CircleX, AlertTriangle } from "lucide-react";

interface SignatureSectionProps {
  signatures: SignatureDto[];
  signatureStatuses?: SignatureStatusDto[];
  isDraft: boolean;
  // Used as a display-name fallback when the signature only carries an id.
  knownUsers?: Record<string, string>;
}

export function SignatureSection({
  signatures,
  signatureStatuses = [],
  knownUsers = {},
  isDraft,
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
          <Badge
            variant="outline"
            className="h-auto border-[#EF7D00] bg-[#FEF3C7] px-3 py-0.5 font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]"
          >
            {isDraft ? t.unsignedDraft : t.notSignedYet}
          </Badge>
        )}
      </div>

      {signatures.length > 0 && (
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
      {signatureStatuses.length === 0 ? (
        <p className="mt-4 text-sm text-gray-500">Underskriftsstatus ukendt</p>
      ) : (
        <div className="mt-4 space-y-2">
          <h3 className="font-bold">Påkrævede underskrifter</h3>

          {signatureStatuses.map((signature) => {
            const isSigned = signature.status === "CompletedSignature";
            const isInvalid = signature.status === "Invalid";

            return (
              <div
                key={signature.requiresSignatureId}
                className="flex items-center gap-3 rounded-lg border p-3"
              >
                {isSigned ? (
                  <CheckCircle2 className="size-5 text-green-600" />
                ) : isInvalid ? (
                  <AlertTriangle className="size-5 text-amber-600" />
                ) : (
                  <CircleX className="size-5 text-red-600" />
                )}

                <div className="flex-1">
                  <p className="font-semibold">{signature.signeeName}</p>
                  <p className="text-sm text-gray-500">
                    {signature.signeeRole}
                  </p>
                </div>

                <Badge
                  variant="outline"
                  className={
                    isSigned
                      ? "border-green-600 text-green-700"
                      : isInvalid
                        ? "border-amber-600 text-amber-700"
                        : "border-red-600 text-red-700"
                  }
                >
                  {isSigned
                    ? "Underskrevet"
                    : isInvalid
                      ? "Ugyldig"
                      : "Mangler"}
                </Badge>
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
