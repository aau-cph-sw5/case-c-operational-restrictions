import { Button } from "@/components/ui/button.tsx";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog.tsx";
import { requiredSignatureTexts as t } from "@/lib/requiredSignatureTexts.ts";

interface SkipRequiredSignaturesDialogProps {
  remainingCount: number;
  onCancel: () => void;
  onConfirm: () => void;
}

// Confirmation before the user continues without signing their required restrictions.
// Escape, outside clicks and "Annuller" all cancel; only "Gå videre" skips.
export function SkipRequiredSignaturesDialog({
  remainingCount,
  onCancel,
  onConfirm,
}: SkipRequiredSignaturesDialogProps) {
  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onCancel();
      }}
    >
      <DialogContent variant="alert" role="alertdialog">
        <div className="space-y-2">
          <DialogTitle>{t.skipConfirmHeading}</DialogTitle>
          <DialogDescription>
            {t.skipConfirmDescription(remainingCount)}
          </DialogDescription>
        </div>
        <div className="flex flex-col gap-2">
          <Button
            size="lg"
            className="h-12 w-full text-base"
            onClick={onConfirm}
          >
            {t.skipConfirm}
          </Button>
          <DialogClose
            render={
              <Button
                size="lg"
                variant="destructive"
                className="h-12 w-full bg-destructive text-base text-white hover:bg-destructive/80"
              />
            }
          >
            {t.skipConfirmCancel}
          </DialogClose>
        </div>
      </DialogContent>
    </Dialog>
  );
}
