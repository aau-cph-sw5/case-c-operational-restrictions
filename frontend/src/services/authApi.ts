// API:
// Raw HTTP calls to the .NET backend's auth endpoints.
// No side effects and no storage — just request in, response out.
import type { LoginRequest } from "../types/loginRequest";
import type { LoginResponse } from "../types/loginResponse";
import type { SetPasswordRequest } from "../types/setPasswordRequest";
import type { UserDto } from "../types/userDto";

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("Forkert e-mail eller adgangskode.");
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
    throw new Error("Sessionen er ugyldig eller udløbet.");
  }

  return response.json();
}

export async function setPassword(request: SetPasswordRequest): Promise<void> {
  const response = await fetch("/api/auth/set-password", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    const errorMessage =
      (errorBody && typeof errorBody.error === "string" && errorBody.error) ||
      "Kunne ikke oprette adgangskode. Prøv venligst igen.";
    throw new Error(errorMessage);
  }
}
