import { useEffect, useState } from "react";
import { ChevronLeft } from "lucide-react";
import { ErrorAlert } from "@/components/common/ErrorAlert.tsx";
import { RestrictionDetails } from "@/components/RestrictionDetail/RestrictionDetails.tsx";
import { Button } from "@/components/ui/button.tsx";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog.tsx";
import { requiredSignatureTexts as t } from "@/lib/requiredSignatureTexts.ts";
import { loadErrorMessage } from "@/lib/utils/loadErrorMessage.ts";
import { requiredSignatureSignErrorMessage } from "@/lib/utils/signErrorMessages.ts";
import { getRestrictionById } from "@/services/restrictionApi.ts";
import { signRequiredSignature } from "@/services/requiredSignatureApi.ts";
import type { RequiredSignatureDto } from "@/types/requiredSignature.ts";
import type { RestrictionDto } from "@/types/restrictionDto.ts";

interface RequiredRestrictionDialogProps {
  requiredSignature: RequiredSignatureDto;
  onClose: () => void;
  onSigned: () => void;
}

// Second-level dialog: shows one restriction and lets the user sign it.
// Unlike the list dialog it can be closed ("Tilbage"), so the user can go back to the list.
export function RequiredRestrictionDialog({
  requiredSignature,
  onClose,
  onSigned,
}: RequiredRestrictionDialogProps) {
  const [restriction, setRestriction] = useState<RestrictionDto | null>(null);
  const [loadError, setLoadError] = useState<unknown>(null);
  const [signError, setSignError] = useState<unknown>(null);
  const [signing, setSigning] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setRestriction(null);
    setLoadError(null);

    getRestrictionById(requiredSignature.restrictionId)
      .then((loaded) => {
        if (!cancelled) setRestriction(loaded);
      })
      .catch((error: unknown) => {
        if (!cancelled) setLoadError(error);
      });

    return () => {
      cancelled = true;
    };
  }, [requiredSignature.restrictionId]);

  async function handleSign() {
    setSigning(true);
    setSignError(null);
    try {
      await signRequiredSignature(requiredSignature.requiresSignatureId);
      onSigned();
    } catch (error) {
      setSignError(error);
    } finally {
      setSigning(false);
    }
  }

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !signing) onClose();
      }}
    >
      <DialogContent aria-busy={signing}>
        <DialogHeader>
          <DialogClose
            render={
              <Button
                variant="ghost"
                className="-ml-2 h-11 px-2 text-base text-[#2563EB]"
              />
            }
            disabled={signing}
          >
            <ChevronLeft aria-hidden className="size-5" />
            {t.back}
          </DialogClose>
          <DialogTitle className="break-words">
            {requiredSignature.title}
          </DialogTitle>
          <DialogDescription>{t.restrictionDescription}</DialogDescription>
        </DialogHeader>

        <DialogBody>
          {loadError !== null && (
            <ErrorAlert>{loadErrorMessage(loadError)}</ErrorAlert>
          )}
          {loadError === null && restriction === null && (
            <p className="text-[#6B6B6B]">{t.loadingRestriction}</p>
          )}
          {restriction && <RestrictionDetails restriction={restriction} />}
        </DialogBody>

        <DialogFooter>
          {signError !== null && (
            <ErrorAlert>
              {requiredSignatureSignErrorMessage(signError)}
            </ErrorAlert>
          )}
          <Button
            size="lg"
            className="h-12 w-full text-base"
            onClick={handleSign}
            disabled={restriction === null || signing}
          >
            {signing ? t.signing : t.sign}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
