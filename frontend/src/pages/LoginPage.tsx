// PAGE:
// Route-level screen component rendered by React Router for user authentication
import { useState } from "react";
import type { SubmitEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { AlertCircle } from "lucide-react";
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

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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

            <CardFooter className="pt-2">
              <Button
                type="submit"
                className="w-full cursor-pointer"
                disabled={loading || !isValidEmail(email) || !password}
              >
                {loading ? "Logger ind..." : "Log ind"}
              </Button>
            </CardFooter>
          </form>
        </Card>
      </div>
    </main>
  );
}
