import type {
  AcceptInviteRequest,
  InviteUserRequest,
} from "../types/invitation";
import type { UserDto } from "../types/userDto";
import { getStoredToken } from "./tokenStorage";

async function readErrorMessage(
  response: Response,
  fallback: string,
): Promise<string> {
  try {
    const body = await response.json();
    if (typeof body?.error === "string") return body.error;
    if (body?.errors && typeof body.errors === "object") {
      const messages = Object.values(body.errors).flat() as string[];
      if (messages.length > 0) return messages.join(" ");
    }
    if (typeof body?.detail === "string") return body.detail;
    if (typeof body?.title === "string") return body.title;
  } catch {
    return fallback;
  }
  return fallback;
}

export async function inviteUser(request: InviteUserRequest): Promise<UserDto> {
  const token = getStoredToken();
  const response = await fetch("/api/invitations", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, "The invitation could not be sent."),
    );
  }

  return response.json();
}

export async function verifyInvite(token: string): Promise<UserDto> {
  const response = await fetch("/api/invitations/verify", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token }),
  });

  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, "The invitation link is invalid."),
    );
  }

  return response.json();
}

export async function acceptInvite(
  request: AcceptInviteRequest,
): Promise<void> {
  const response = await fetch("/api/invitations/accept", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, "The password could not be saved."),
    );
  }
}
