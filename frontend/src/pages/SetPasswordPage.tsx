import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { LoaderCircle } from "lucide-react";
import { Card, CardContent } from "../components/ui/card";
import { AuthHeader } from "../components/auth/AuthHeader";
import { InvalidTokenCard } from "../components/auth/InvalidTokenCard";
import { SetPasswordForm } from "../components/auth/SetPasswordForm";
import { SetPasswordSuccessCard } from "../components/auth/SetPasswordSuccessCard";
import { authService } from "../services/authService";
import type { UserDto } from "../types/userDto";

export function SetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const [tokenStatus, setTokenStatus] = useState<
    "checking" | "valid" | "invalid"
  >("checking");
  const [user, setUser] = useState<UserDto | null>(null);
  const [tokenErrorMessage, setTokenErrorMessage] = useState<string | null>(
    null,
  );
  const [isSuccess, setIsSuccess] = useState(false);

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

  async function handlePasswordSubmit(password: string) {
    if (!token) return;
    await authService.setPassword({
      token,
      password,
    });
    setIsSuccess(true);
  }

  return (
    <main className="flex min-h-[calc(100svh-4rem)] w-full items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <AuthHeader />

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
          <InvalidTokenCard errorMessage={tokenErrorMessage} />
        )}

        {tokenStatus === "valid" && !isSuccess && (
          <SetPasswordForm user={user} onSubmit={handlePasswordSubmit} />
        )}

        {tokenStatus === "valid" && isSuccess && <SetPasswordSuccessCard />}
      </div>
    </main>
  );
}
