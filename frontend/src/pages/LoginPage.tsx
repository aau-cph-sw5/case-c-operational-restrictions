// PAGE:
// Route-level screen component rendered by React Router for the login and password reset request flows
import { useState } from "react";
import type { SubmitEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "../components/ui/alert";
import { Button } from "../components/ui/button";
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
import { isValidEmail } from "../lib/emailValidation";
import { authService } from "../services/authService";

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();

  const [mode, setMode] = useState<"login" | "forgot-password">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [resetEmail, setResetEmail] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [resetSent, setResetSent] = useState(false);

  function switchMode(newMode: "login" | "forgot-password") {
    setMode(newMode);
    setError(null);
    setResetSent(false);
    if (newMode === "forgot-password" && email && !resetEmail) {
      setResetEmail(email);
    }
  }

  async function handleLogin(e: SubmitEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await authService.login({ email: email.trim(), password });
      const destination =
        (location.state as { from?: { pathname?: string } })?.from?.pathname ||
        "/home";
      navigate(destination, { replace: true });
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Forkert e-mail eller adgangskode.",
      );
    } finally {
      setLoading(false);
    }
  }

  async function handleForgotPassword(e: SubmitEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setResetSent(false);

    try {
      await authService.requestPasswordReset({ email: resetEmail.trim() });
      setResetSent(true);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Kunne ikke sende link til nulstilling. Prøv venligst igen.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-[calc(100svh-4rem)] w-full items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6">
        <header className="text-center space-y-1">
          <img
            src="/pwa-192x192.png" // replace once we have their actual logo
            alt="Metro Service logo"
            className="size-12 rounded-xl shadow-xs mx-auto mb-2"
          />
          <h1 className="text-2xl font-bold tracking-tight text-foreground font-ui">
            Metro Service
          </h1>
          <p className="text-sm text-muted-foreground">Driftsrestriktioner</p>
        </header>

        <Card className="border border-border/80 shadow-sm rounded-2xl">
          {mode === "login" ? (
            <form onSubmit={handleLogin} noValidate>
              <CardHeader className="space-y-1">
                <CardTitle className="text-xl">Log ind</CardTitle>
                <CardDescription>
                  Indtast din e-mail og adgangskode for at få adgang.
                </CardDescription>
              </CardHeader>

              <CardContent className="space-y-4">
                {error && (
                  <Alert variant="destructive">
                    <AlertCircle className="size-4" />
                    <AlertTitle>Fejl ved login</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                <div className="space-y-2">
                  <Label htmlFor="login-email">E-mail</Label>
                  <Input
                    id="login-email"
                    type="email"
                    name="email"
                    autoComplete="email"
                    placeholder="f.eks. bruger@metroservice.dk"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="login-password">Adgangskode</Label>
                  <Input
                    id="login-password"
                    type="password"
                    name="password"
                    autoComplete="current-password"
                    placeholder="Indtast din adgangskode"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                </div>
              </CardContent>

              <CardFooter className="flex flex-col gap-2 pt-2">
                <Button
                  type="submit"
                  className="w-full cursor-pointer"
                  disabled={loading || !isValidEmail(email) || !password}
                >
                  {loading ? "Logger ind..." : "Log ind"}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full cursor-pointer text-muted-foreground hover:text-foreground"
                  onClick={() => switchMode("forgot-password")}
                >
                  Glemt adgangskode?
                </Button>
              </CardFooter>
            </form>
          ) : (
            <form onSubmit={handleForgotPassword} noValidate>
              <CardHeader className="space-y-1">
                <CardTitle className="text-xl">Glemt adgangskode</CardTitle>
                <CardDescription>
                  Indtast din e-mail for at modtage et nulstillingslink.
                </CardDescription>
              </CardHeader>

              <CardContent className="space-y-4">
                {error && (
                  <Alert variant="destructive">
                    <AlertCircle className="size-4" />
                    <AlertTitle>Fejl</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                {resetSent && (
                  <Alert className="border-green-600/30 bg-green-50/50 text-green-900 dark:bg-green-950/20 dark:text-green-300">
                    <CheckCircle2 className="size-4 text-green-600 dark:text-green-400" />
                    <AlertTitle>Link afsendt</AlertTitle>
                    <AlertDescription>
                      Hvis e-mailadressen findes, har vi sendt et link.
                    </AlertDescription>
                  </Alert>
                )}

                <div className="space-y-2">
                  <Label htmlFor="reset-email">E-mail</Label>
                  <Input
                    id="reset-email"
                    type="email"
                    name="email"
                    autoComplete="email"
                    placeholder="f.eks. bruger@metroservice.dk"
                    value={resetEmail}
                    onChange={(e) => setResetEmail(e.target.value)}
                    required
                  />
                </div>

                <div className="flex h-14.5 items-center rounded-md border border-border/60 bg-muted/30 px-3 text-xs text-muted-foreground">
                  <p>
                    Vi sender et link til din indbakke til at nulstille din
                    adgangskode.
                  </p>
                </div>
              </CardContent>

              <CardFooter className="flex flex-col gap-2 pt-2">
                <Button
                  type="submit"
                  className="w-full cursor-pointer"
                  disabled={loading || !isValidEmail(resetEmail)}
                >
                  {loading ? "Sender link..." : "Send nulstillingslink"}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full cursor-pointer text-muted-foreground hover:text-foreground"
                  onClick={() => switchMode("login")}
                >
                  Tilbage til login
                </Button>
              </CardFooter>
            </form>
          )}
        </Card>
      </div>
    </main>
  );
}
