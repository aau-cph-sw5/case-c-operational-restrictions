import { RestrictionActionButton } from "@/components/RestrictionActionButton.tsx";
import { ErrorAlert } from "@/components/common/ErrorAlert.tsx";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";

interface SigningOutcomeNoticeProps {
  checking: boolean;
  onCheck: () => void;
}

// Shown when the connection dropped and it is not known whether the signature
// was recorded. No sign action is offered until the server has answered.
export function SigningOutcomeNotice({
  checking,
  onCheck,
}: SigningOutcomeNoticeProps) {
  return (
    <div className="space-y-3 pb-4" data-testid="signing-outcome-unknown">
      <ErrorAlert>{t.signOutcomeUnknown}</ErrorAlert>
      <RestrictionActionButton
        onClick={onCheck}
        loading={checking}
        loadingLabel={t.signChecking}
      >
        {t.signCheckAgain}
      </RestrictionActionButton>
    </div>
  );
}
