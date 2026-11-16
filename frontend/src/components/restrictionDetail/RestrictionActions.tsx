import { RequestChangesForm } from "@/components/RequestChangesForm.tsx";
import { RestrictionActionButton } from "@/components/RestrictionActionButton.tsx";
import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { FilePen, Pencil } from "lucide-react";

interface RestrictionActionsProps {
  canSign: boolean;
  signing: boolean;
  onSign: () => void;
  canRequestChanges: boolean;
  submittingChanges: boolean;
  onRequestChanges: (comment: string) => Promise<boolean>;
}

export function RestrictionActions({
  canSign,
  signing,
  onSign,
  canRequestChanges,
  submittingChanges,
  onRequestChanges,
}: RestrictionActionsProps) {
  return (
    <>
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
    </>
  );
}
