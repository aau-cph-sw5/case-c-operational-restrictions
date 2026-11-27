import { RequestChangesForm } from "@/components/RequestChangesForm.tsx";
import { RestrictionActionButton } from "@/components/RestrictionActionButton.tsx";
import type { CancellationStep } from "@/hooks/useRestrictionPermissions.ts";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { BadgeCheck, Ban, FilePen, LogOut, Pencil } from "lucide-react";
import type { ReactNode } from "react";

interface RestrictionActionsProps {
  canSign: boolean;
  signing: boolean;
  onSign: () => void;
  canApprove: boolean;
  approving: boolean;
  onApprove: () => void;
  canRequestChanges: boolean;
  submittingChanges: boolean;
  onRequestChanges: (comment: string) => Promise<boolean>;
  canSignForImplementation: boolean;
  signingForImplementation: boolean;
  onSignForImplementation: () => void;
  cancellationStep: CancellationStep | null;
  cancelling: boolean;
  onCancellationStep: () => void;
}

const cancellationButtons: Record<
  CancellationStep,
  { label: string; loadingLabel: string; icon: ReactNode; title: string }
> = {
  start: {
    label: t.startCancellation,
    loadingLabel: t.startingCancellation,
    icon: <Ban className="size-4" aria-hidden />,
    title: "Sign to move the restriction to pending cancellation",
  },
  approve: {
    label: t.approveCancellation,
    loadingLabel: t.approvingCancellation,
    icon: <BadgeCheck className="size-4" aria-hidden />,
    title: "Confirm the cause is resolved and sign the cancellation",
  },
  takeOutOfForce: {
    label: t.takeOutOfForce,
    loadingLabel: t.takingOutOfForce,
    icon: <LogOut className="size-4" aria-hidden />,
    title: "Sign to take the restriction out of force",
  },
};

export function RestrictionActions({
  canSign,
  signing,
  onSign,
  canApprove,
  approving,
  onApprove,
  canRequestChanges,
  submittingChanges,
  onRequestChanges,
  canSignForImplementation,
  signingForImplementation,
  onSignForImplementation,
  cancellationStep,
  cancelling,
  onCancellationStep,
}: RestrictionActionsProps) {
  return (
    <>
      {canApprove && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={onApprove}
            loading={approving}
            loadingLabel={t.approving}
          >
            {t.approve}
          </RestrictionActionButton>
        </div>
      )}

      {canRequestChanges && (
        <RequestChangesForm
          submitting={submittingChanges}
          onSubmit={onRequestChanges}
        />
      )}

      {canSign && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={onSign}
            loading={signing}
            loadingLabel={t.signing}
            icon={<Pencil className="size-4" aria-hidden />}
            title="Sign and Submit for Approval"
          >
            {t.sign}
          </RestrictionActionButton>

          {/* TEMPORARY: edit flow is not implemented yet. */}
          <RestrictionActionButton
            disabled
            title={t.editComingSoon}
            icon={<FilePen className="size-4" aria-hidden />}
          >
            {t.edit}
          </RestrictionActionButton>
        </div>
      )}

      {canSignForImplementation && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={onSignForImplementation}
            loading={signingForImplementation}
            loadingLabel={t.signing}
            title="Sign for Implementation"
          >
            {t.signForImplementation}
          </RestrictionActionButton>
        </div>
      )}

      {cancellationStep && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={onCancellationStep}
            loading={cancelling}
            loadingLabel={cancellationButtons[cancellationStep].loadingLabel}
            icon={cancellationButtons[cancellationStep].icon}
            title={cancellationButtons[cancellationStep].title}
          >
            {cancellationButtons[cancellationStep].label}
          </RestrictionActionButton>
        </div>
      )}
    </>
  );
}
