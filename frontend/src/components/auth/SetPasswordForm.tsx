import { useId, useMemo, useState } from "react";
import type { SubmitEvent } from "react";
import { AlertCircle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "../ui/alert";
import { Button } from "../ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { validatePassword } from "../../lib/passwordValidation";
import { PasswordRequirementsList } from "./PasswordRequirementsList";
import type { UserDto } from "../../types/userDto";

interface SetPasswordFormProps {
  user: UserDto | null;
  onSubmit: (password: string) => Promise<void>;
}

export function SetPasswordForm({ user, onSubmit }: SetPasswordFormProps) {
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const newPasswordId = useId();
  const confirmPasswordId = useId();

  const validation = useMemo(
    () => validatePassword(newPassword, confirmPassword),
    [newPassword, confirmPassword],
  );

  const hasPasswordInput = newPassword.length > 0;
  const hasConfirmInput = confirmPassword.length > 0;

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault();
    if (!validation.isValid) return;

    setSubmitting(true);
    setSubmitError(null);

    try {
      await onSubmit(newPassword);
    } catch (err) {
      setSubmitError(
        err instanceof Error
          ? err.message
          : "Kunne ikke oprette adgangskode. Prøv venligst igen.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="border border-border/80 shadow-sm rounded-2xl">
      <CardHeader className="space-y-1">
        <CardTitle className="text-xl">Opret adgangskode</CardTitle>
        <CardDescription>
          {user?.email
            ? `Vælg en adgangskode til ${user.email}`
            : "Vælg en sikker adgangskode til din konto"}
        </CardDescription>
      </CardHeader>

      <form onSubmit={handleSubmit} noValidate>
        <CardContent className="space-y-4">
          {submitError && (
            <Alert variant="destructive">
              <AlertCircle className="size-4" />
              <AlertTitle>Fejl ved oprettelse</AlertTitle>
              <AlertDescription>{submitError}</AlertDescription>
            </Alert>
          )}

          <div className="space-y-2">
            <Label htmlFor={newPasswordId}>Ny adgangskode</Label>
            <Input
              id={newPasswordId}
              type="password"
              autoComplete="new-password"
              placeholder="Indtast ny adgangskode"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor={confirmPasswordId}>Bekræft adgangskode</Label>
            <Input
              id={confirmPasswordId}
              type="password"
              autoComplete="new-password"
              placeholder="Gentag ny adgangskode"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
            />
          </div>

          <PasswordRequirementsList
            validation={validation}
            hasPasswordInput={hasPasswordInput}
            hasConfirmInput={hasConfirmInput}
          />
        </CardContent>

        <CardFooter className="pt-2">
          <Button
            type="submit"
            className="w-full cursor-pointer"
            disabled={submitting || !validation.isValid}
          >
            {submitting ? "Opretter adgangskode..." : "Gem adgangskode"}
          </Button>
        </CardFooter>
      </form>
    </Card>
  );
}
