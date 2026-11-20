import { useState } from "react";
import type { FormEvent } from "react";
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
import { inviteUser } from "../services/invitationApi";
import { USER_ROLE_OPTIONS } from "../types/invitation";
import type { UserRoleValue } from "../types/invitation";

export function InviteUserPage() {
  const [email, setEmail] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [role, setRole] = useState<UserRoleValue>(USER_ROLE_OPTIONS[0].value);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [invitedEmail, setInvitedEmail] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError(null);
    setInvitedEmail(null);

    try {
      const user = await inviteUser({ email, firstName, lastName, role });
      setInvitedEmail(user.email);
      setEmail("");
      setFirstName("");
      setLastName("");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Invitation failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto w-full max-w-md px-4 py-8">
      <Card>
        <CardHeader>
          <CardTitle>Invite user</CardTitle>
          <CardDescription>
            The user gets an email with a link to create their password.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="invite-email">Email</Label>
              <Input
                id="invite-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="invite-first-name">First name</Label>
              <Input
                id="invite-first-name"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                maxLength={100}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="invite-last-name">Last name</Label>
              <Input
                id="invite-last-name"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                maxLength={100}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="invite-role">Role</Label>
              <select
                id="invite-role"
                value={role}
                onChange={(e) =>
                  setRole(Number(e.target.value) as UserRoleValue)
                }
                className="h-9 rounded-md border border-input bg-transparent px-3 text-sm"
              >
                {USER_ROLE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            <Button type="submit" disabled={loading}>
              {loading ? "Sending invite..." : "Send invite"}
            </Button>
          </form>

          {error && (
            <Alert variant="destructive" className="mt-4">
              <AlertTitle>Could not invite user</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {invitedEmail && (
            <Alert className="mt-4">
              <AlertTitle>Invite sent</AlertTitle>
              <AlertDescription>
                An invitation was sent to {invitedEmail}.
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
