// COMPONENT:
// DOM form for returning a pending restriction to its originator.
// The comment is mandatory; the parent performs the API call.
import { Undo2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import { RestrictionActionButton } from "./RestrictionActionButton";
import { Label } from "./ui/label";
import { Textarea } from "./ui/textarea";

interface RequestChangesFormProps {
  submitting: boolean;
  onSubmit: (comment: string) => Promise<boolean>;
}

export function RequestChangesForm({
  submitting,
  onSubmit,
}: RequestChangesFormProps) {
  const [comment, setComment] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    if (!comment.trim()) {
      setValidationError(t.requestChangesCommentRequired);
      return;
    }

    setValidationError(null);
    if (await onSubmit(comment)) setComment("");
  }

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      aria-label={t.requestChangesHeading}
      className="space-y-2 pb-4"
    >
      <h2 className="font-['Instrument_Sans',sans-serif] text-lg font-bold">
        {t.requestChangesHeading}
      </h2>
      <Label
        htmlFor="request-changes-comment"
        className="font-['Inter_Variable','Inter',sans-serif] text-base"
      >
        {t.requestChangesLabel}
      </Label>
      <p
        id="request-changes-helper"
        className="font-['Inter_Variable','Inter',sans-serif] text-sm text-[#6B6B6B]"
      >
        {t.requestChangesHelper}
      </p>
      <Textarea
        id="request-changes-comment"
        value={comment}
        disabled={submitting}
        aria-invalid={validationError !== null}
        aria-describedby={
          validationError
            ? "request-changes-helper request-changes-error"
            : "request-changes-helper"
        }
        onChange={(event) => {
          setComment(event.target.value);
          if (validationError) setValidationError(null);
        }}
      />
      {validationError && (
        <p
          id="request-changes-error"
          className="font-['Inter_Variable','Inter',sans-serif] text-sm text-destructive"
        >
          {validationError}
        </p>
      )}
      <RestrictionActionButton
        type="submit"
        loading={submitting}
        loadingLabel={t.requestingChanges}
        icon={<Undo2 className="size-4" aria-hidden />}
      >
        {t.requestChanges}
      </RestrictionActionButton>
    </form>
  );
}
