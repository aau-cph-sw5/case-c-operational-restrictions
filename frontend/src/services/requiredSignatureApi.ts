// API:
// Raw HTTP calls to the backend's required-signature endpoints.
// Non-2xx responses become errors carrying the HTTP status, like restrictionApi.
import type { RequiredSignatureDto } from "../types/requiredSignature";
import type { RestrictionApiError } from "../types/restrictionApiError";
import { getAuthHeaders } from "./authApi";

function createApiError(status: number): RestrictionApiError {
  return Object.assign(new Error(`HTTP ${status}`), { status });
}

// GET /api/signature/required/me
export async function getMyPendingRequiredSignatures(): Promise<
  RequiredSignatureDto[]
> {
  const response = await fetch("/api/signature/required/me", {
    headers: getAuthHeaders(),
  });

  if (!response.ok) {
    throw createApiError(response.status);
  }

  return response.json();
}

// POST /api/signature/sign/{requiresSignatureId}
export async function signRequiredSignature(
  requiresSignatureId: string,
): Promise<void> {
  const response = await fetch(
    `/api/signature/sign/${encodeURIComponent(requiresSignatureId)}`,
    { method: "POST", headers: getAuthHeaders() },
  );

  if (!response.ok) {
    throw createApiError(response.status);
  }
}
