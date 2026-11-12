import { useEffect, useId, useMemo, useState } from "react";
import type { SubmitEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  AlertCircle,
  Check,
  CheckCircle2,
  LoaderCircle,
  X,
} from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "../components/ui/alert";
import { Button, buttonVariants } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Label } from "../components/ui/label";
import { validatePassword } from "../lib/passwordValidation";
import { authService } from "../services/authService";
import type { UserDto } from "../types/userDto";

function RequirementItem({
  label,
  met,
  hasInput,
}: {
  label: string;
  met: boolean;
  hasInput: boolean;
}) {
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

export function SetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [tokenStatus, setTokenStatus] = useState<
    "checking" | "valid" | "invalid"
  >("checking");
  const [user, setUser] = useState<UserDto | null>(null);
  const [tokenErrorMessage, setTokenErrorMessage] = useState<string | null>(
    null,
  );

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  const newPasswordId = useId();
  const confirmPasswordId = useId();

  const isStandalone = useMemo(() => {
    if (typeof window === "undefined") return false;
    return (
      window.matchMedia("(display-mode: standalone)").matches ||
      (window.navigator as unknown as { standalone?: boolean }).standalone ===
        true
    );
  }, []);

  useEffect(() => {
    let isCancelled = false;

    async function verifyToken() {
      if (!token) {
        setTokenStatus("invalid");
        setTokenErrorMessage(
          "Der mangler et sikkerhedstoken i linket. Kontrollér linket fra din e-mail.",
        );
        return;
      }

      setTokenStatus("checking");
      setTokenErrorMessage(null);

      try {
        const validatedUser = await authService.validateToken(token);
        if (!isCancelled) {
          setUser(validatedUser);
          setTokenStatus("valid");
        }
      } catch (err) {
        if (!isCancelled) {
          setTokenStatus("invalid");
          setTokenErrorMessage(
            err instanceof Error
              ? err.message
              : "Linket er udløbet eller ugyldigt. Anmod venligst om et nyt link.",
          );
        }
      }
    }

    verifyToken();

    return () => {
      isCancelled = true;
    };
  }, [token]);

  const validation = useMemo(
    () => validatePassword(newPassword, confirmPassword),
    [newPassword, confirmPassword],
  );

  const hasPasswordInput = newPassword.length > 0;
  const hasConfirmInput = confirmPassword.length > 0;

  async function handleSubmit(e: SubmitEvent) {
    e.preventDefault();
    if (!token || !validation.isValid) return;

    setSubmitting(true);
    setSubmitError(null);

    try {
      await authService.setPassword({
        token,
        password: newPassword,
      });
      setIsSuccess(true);
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
    <main className="flex min-h-[calc(100svh-4rem)] w-full items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <header className="text-center space-y-1">
          <img
            src="/pwa-192x192.png" // should also be replaced
            alt="Metro Service logo"
            className="size-12 rounded-xl shadow-xs mx-auto mb-2"
          />
          <h1 className="text-2xl font-bold tracking-tight text-foreground font-ui">
            Metro Service
          </h1>
          <p className="text-sm text-muted-foreground">Driftsrestriktioner</p>
        </header>

        {tokenStatus === "checking" && (
          <Card className="border border-border/80 shadow-sm rounded-2xl">
            <CardContent className="flex flex-col items-center justify-center gap-3 py-12 text-center">
              <LoaderCircle className="size-8 animate-spin text-primary" />
              <p className="text-sm text-muted-foreground">
                Validerer invitationslink...
              </p>
            </CardContent>
          </Card>
        )}

        {tokenStatus === "invalid" && (
          <Card className="border border-border/80 shadow-sm rounded-2xl">
            <CardHeader className="space-y-1 text-center">
              <div className="mx-auto flex size-10 items-center justify-center rounded-full bg-destructive/10 text-destructive mb-1">
                <AlertCircle className="size-5" />
              </div>
              <CardTitle className="text-xl">
                Ugyldigt eller udløbet link
              </CardTitle>
              <CardDescription>
                {tokenErrorMessage ||
                  "Dette link til oprettelse af adgangskode er ikke længere gyldigt."}
              </CardDescription>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground text-center">
              Du kan anmode om et nyt nulstillingslink fra login-siden.
            </CardContent>
            <CardFooter className="pt-2">
              <Link
                to="/login"
                className={buttonVariants({
                  className: "w-full cursor-pointer text-center",
                })}
              >
                Gå til login
              </Link>
            </CardFooter>
          </Card>
        )}

        {tokenStatus === "valid" && !isSuccess && (
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
        )}

        {tokenStatus === "valid" && isSuccess && (
          <Card className="border border-border/80 shadow-sm rounded-2xl">
            <CardHeader className="space-y-1 text-center">
              <div className="mx-auto flex size-12 items-center justify-center rounded-full bg-green-100 text-green-600 dark:bg-green-950/40 dark:text-green-400 mb-2">
                <CheckCircle2 className="size-7" />
              </div>
              <CardTitle className="text-xl">Adgangskode oprettet</CardTitle>
              <CardDescription>
                {isStandalone
                  ? "Din adgangskode er nu gemt. Du kan fortsætte direkte til login."
                  : "Din nye adgangskode er oprettet. Du kan nu lukke denne fane og vende tilbage til Metro-appen for at logge ind."}
              </CardDescription>
            </CardHeader>

            <CardContent className="text-sm text-muted-foreground text-center">
              {isStandalone ? (
                <p>Log ind for at få adgang til driftsrestriktionerne.</p>
              ) : (
                <p>
                  Hvis du ønsker at fortsætte i denne browser, kan du også logge
                  ind her.
                </p>
              )}
            </CardContent>

            <CardFooter className="flex flex-col gap-2 pt-2">
              {isStandalone ? (
                <Button
                  type="button"
                  className="w-full cursor-pointer"
                  onClick={() => navigate("/login")}
                >
                  Gå til login
                </Button>
              ) : (
                <>
                  <Button
                    type="button"
                    variant="outline"
                    className="w-full cursor-pointer"
                    onClick={() => navigate("/login")}
                  >
                    Log ind i browseren
                  </Button>
                </>
              )}
            </CardFooter>
          </Card>
        )}
      </div>
    </main>
  );
}
