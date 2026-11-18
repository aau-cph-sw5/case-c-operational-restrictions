import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Alert, AlertDescription, AlertTitle } from "../components/ui/alert";
import { Button } from "../components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Label } from "../components/ui/label";
import { acceptInvite, verifyInvite } from "../services/invitationApi";
import type { UserDto } from "../types/userDto";

const MIN_PASSWORD_LENGTH = 8;

type PageState =
  | { kind: "verifying" }
  | { kind: "invalid"; message: string }
  | { kind: "ready"; user: UserDto }
  | { kind: "done" };

export function SetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const [state, setState] = useState<PageState>({ kind: "verifying" });
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;

    if (!token) {
      setState({ kind: "invalid", message: "The link is missing its token." });
      return;
    }

    verifyInvite(token)
      .then((user) => {
        if (!cancelled) setState({ kind: "ready", user });
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setState({
            kind: "invalid",
            message:
              err instanceof Error ? err.message : "The link is invalid.",
          });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setFormError(null);

    if (password.length < MIN_PASSWORD_LENGTH) {
      setFormError(
        `The password must be at least ${MIN_PASSWORD_LENGTH} characters.`,
      );
      return;
    }
    if (password !== confirmPassword) {
      setFormError("The passwords do not match.");
      return;
    }

    setSaving(true);
    try {
      await acceptInvite({ token, password });
      setState({ kind: "done" });
    } catch (err) {
      setFormError(
        err instanceof Error ? err.message : "The password could not be saved.",
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-md px-4 py-8">
      <Card>
        <CardHeader>
          <CardTitle>Create your password</CardTitle>
          {state.kind === "ready" && (
            <CardDescription>
              Welcome {state.user.name} ({state.user.email})
            </CardDescription>
          )}
        </CardHeader>
        <CardContent>
          {state.kind === "verifying" && <p>Checking your invitation...</p>}

          {state.kind === "invalid" && (
            <Alert variant="destructive">
              <AlertTitle>Invitation not valid</AlertTitle>
              <AlertDescription>{state.message}</AlertDescription>
            </Alert>
          )}

          {state.kind === "done" && (
            <Alert>
              <AlertTitle>Password created</AlertTitle>
              <AlertDescription>
                Your account is active. <Link to="/login">Go to login</Link>
              </AlertDescription>
            </Alert>
          )}

          {state.kind === "ready" && (
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <div className="flex flex-col gap-2">
                <Label htmlFor="new-password">Password</Label>
                <Input
                  id="new-password"
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="confirm-password">Confirm password</Label>
                <Input
                  id="confirm-password"
                  type="password"
                  autoComplete="new-password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                />
              </div>

              {formError && (
                <Alert variant="destructive">
                  <AlertDescription>{formError}</AlertDescription>
                </Alert>
              )}

              <Button type="submit" disabled={saving}>
                {saving ? "Saving..." : "Create password"}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
