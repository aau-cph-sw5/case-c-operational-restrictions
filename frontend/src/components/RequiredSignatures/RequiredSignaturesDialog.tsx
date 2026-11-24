import { useState } from "react";
import { ChevronRight } from "lucide-react";
import { ErrorAlert } from "@/components/common/ErrorAlert.tsx";
import { Button } from "@/components/ui/button.tsx";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog.tsx";
import { usePendingRequiredSignatures } from "@/hooks/usePendingRequiredSignatures.ts";
import { formatDayMonthYear } from "@/lib/dateFormat.ts";
import { requiredSignatureTexts as t } from "@/lib/requiredSignatureTexts.ts";
import type { RequiredSignatureDto } from "@/types/requiredSignature.ts";
import { RequiredRestrictionDialog } from "./RequiredRestrictionDialog";
import { SkipRequiredSignaturesDialog } from "./SkipRequiredSignaturesDialog";

// Blocking dialog shown after login while the user has unsigned restrictions in force.
// Escape and outside clicks are ignored; it closes by itself once the list is empty,
// or when the user confirms "Gå videre uden at underskrive". Skipping only lasts until
// the next page load, where the check runs again. If the list cannot be loaded it
// stays open with a retry.
// Phone first: full screen with one tappable card per restriction instead of a wide table.
export function RequiredSignaturesDialog() {
  const { requiredSignatures, loading, error, reload } =
    usePendingRequiredSignatures();
  const [selected, setSelected] = useState<RequiredSignatureDto | null>(null);
  const [confirmingSkip, setConfirmingSkip] = useState(false);
  const [skipped, setSkipped] = useState(false);

  const open = !skipped && (error !== null || requiredSignatures.length > 0);

  function handleConfirmSkip() {
    setConfirmingSkip(false);
    setSkipped(true);
  }

  function handleSigned() {
    setSelected(null);
    reload();
  }

  return (
    <Dialog open={open} disablePointerDismissal onOpenChange={() => {}}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t.heading}</DialogTitle>
          <DialogDescription>{t.description}</DialogDescription>
          {error === null && (
            <p className="pt-1 text-sm font-bold text-[#EF7D00]">
              {t.remaining(requiredSignatures.length)}
            </p>
          )}
        </DialogHeader>

        {error !== null ? (
          <DialogBody>
            <ErrorAlert>{t.loadFailed}</ErrorAlert>
          </DialogBody>
        ) : (
          <DialogBody>
            <ul aria-label={t.listLabel} className="space-y-3">
              {requiredSignatures.map((requiredSignature) => (
                <li key={requiredSignature.requiresSignatureId}>
                  <button
                    type="button"
                    data-testid="required-signature-row"
                    onClick={() => setSelected(requiredSignature)}
                    className="flex min-h-12 w-full items-center gap-3 rounded-lg border border-[#2563EB] bg-[#EFF6FF] p-4 text-left transition-colors active:bg-[#DBEAFE]"
                  >
                    <span className="min-w-0 flex-1 space-y-1">
                      <span className="block text-lg leading-snug font-bold break-words text-black">
                        {requiredSignature.title}
                      </span>
                      {requiredSignature.reference && (
                        <span className="block text-sm text-[#6B6B6B]">
                          {requiredSignature.reference}
                        </span>
                      )}
                      <span className="flex flex-wrap gap-x-4 gap-y-0.5 text-sm text-[#6B6B6B]">
                        <span>
                          {t.startDate}:{" "}
                          <span className="text-black">
                            {formatDayMonthYear(requiredSignature.startDate)}
                          </span>
                        </span>
                        <span>
                          {t.endDate}:{" "}
                          <span className="text-black">
                            {formatDayMonthYear(requiredSignature.endDate)}
                          </span>
                        </span>
                      </span>
                      <span className="block pt-1 text-sm font-bold text-[#2563EB]">
                        {t.open}
                      </span>
                    </span>
                    <ChevronRight
                      aria-hidden
                      className="size-6 shrink-0 text-[#2563EB]"
                    />
                  </button>
                </li>
              ))}
            </ul>
          </DialogBody>
        )}

        <DialogFooter>
          {error !== null && (
            <Button
              size="lg"
              className="h-12 w-full text-base"
              onClick={reload}
              disabled={loading}
            >
              {t.retry}
            </Button>
          )}
          <Button
            size="lg"
            className="h-12 w-full text-base"
            onClick={() => setConfirmingSkip(true)}
          >
            {t.skip}
          </Button>
        </DialogFooter>

        {selected && (
          <RequiredRestrictionDialog
            requiredSignature={selected}
            onClose={() => setSelected(null)}
            onSigned={handleSigned}
          />
        )}

        {confirmingSkip && (
          <SkipRequiredSignaturesDialog
            remainingCount={requiredSignatures.length}
            onCancel={() => setConfirmingSkip(false)}
            onConfirm={handleConfirmSkip}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
