import { Check, X } from "lucide-react";
import type { PasswordValidationResult } from "../../lib/passwordValidation";

interface RequirementItemProps {
  label: string;
  met: boolean;
  hasInput: boolean;
}

function RequirementItem({ label, met, hasInput }: RequirementItemProps) {
  return (
    <li className="flex items-center gap-2 text-xs">
      {met ? (
        <Check className="size-3.5 text-green-600 dark:text-green-400 shrink-0" />
      ) : hasInput ? (
        <X className="size-3.5 text-muted-foreground/60 shrink-0" />
      ) : (
        <span className="size-1.5 rounded-full bg-muted-foreground/50 shrink-0 ml-1 mr-1" />
      )}
      <span
        className={
          met ? "text-foreground font-medium" : "text-muted-foreground"
        }
      >
        {label}
      </span>
    </li>
  );
}

interface PasswordRequirementsListProps {
  validation: PasswordValidationResult;
  hasPasswordInput: boolean;
  hasConfirmInput: boolean;
}

export function PasswordRequirementsList({
  validation,
  hasPasswordInput,
  hasConfirmInput,
}: PasswordRequirementsListProps) {
  return (
    <div className="rounded-lg border border-border/60 bg-muted/30 p-3 space-y-2">
      <p className="text-xs font-medium text-foreground">
        Krav til adgangskoden:
      </p>
      <ul className="space-y-1">
        <RequirementItem
          label="Mindst 8 tegn"
          met={validation.hasMinLength}
          hasInput={hasPasswordInput}
        />
        <RequirementItem
          label="Mindst ét stort bogstav (A-Z)"
          met={validation.hasUppercase}
          hasInput={hasPasswordInput}
        />
        <RequirementItem
          label="Mindst ét lille bogstav (a-z)"
          met={validation.hasLowercase}
          hasInput={hasPasswordInput}
        />
        <RequirementItem
          label="Mindst ét tal (0-9)"
          met={validation.hasNumber}
          hasInput={hasPasswordInput}
        />
        <RequirementItem
          label="Adgangskoderne er ens"
          met={validation.passwordsMatch}
          hasInput={hasConfirmInput}
        />
      </ul>
    </div>
  );
}
