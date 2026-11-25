// API:
// Raw HTTP calls to the .NET backend's auth endpoints.
// No side effects and no storage — just request in, response out.
import type { LoginRequest } from "../types/loginRequest";
import type { LoginResponse } from "../types/loginResponse";
import type { UserDto } from "../types/userDto";
import { getStoredToken } from "./tokenStorage";

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

  return response.json();
}

export function getAuthHeaders(): HeadersInit {
  const token = getStoredToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

// A signing request carries the key of the attempt, so a retry after a lost
// response gets the committed signature back instead of signing twice.
export function getSigningHeaders(attemptKey?: string): HeadersInit {
  return attemptKey
    ? { ...getAuthHeaders(), "Idempotency-Key": attemptKey }
    : getAuthHeaders();
}

export async function getCurrentUserId(): Promise<string | null> {
  const token = getStoredToken();
  if (!token) return null;
  const user = await getCurrentUser(token);
  return user?.id ?? null;
}
