// API:
// Raw HTTP calls to the .NET backend's auth endpoints.
// No side effects and no storage — just request in, response out.
import { normalizeRole } from "../lib/userRole";
import type { LoginRequest } from "../types/loginRequest";
import type { LoginResponse } from "../types/loginResponse";
import type { UserDto } from "../types/userDto";

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Invalid email or password.");
  }

  return response.json();
}

export async function getCurrentUser(token: string): Promise<UserDto> {
  const response = await fetch(
    `/api/auth/check-token/${encodeURIComponent(token)}`,
    {
      method: "POST",
    },
  );

  if (!response.ok) {
    throw new Error("Session is invalid or expired.");
  }

  // The backend serialises the role enum as a number; normalise it to its name.
  const raw = (await response.json()) as UserDto & { role: unknown };
  return { ...raw, role: normalizeRole(raw.role) };
}
