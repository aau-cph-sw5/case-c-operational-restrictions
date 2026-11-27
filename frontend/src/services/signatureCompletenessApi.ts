// API:
// Handles requests to the .NET backend's signature completeness endpoint.
// Includes the stored JWT for authentication and handles failed requests.
// Returns the signature completeness report used by the frontend.

import type { SignatureCompletenessReportDto } from "../types/signatureCompleteness";
import type { RestrictionApiError } from "../types/restrictionApiError";
import { getStoredToken } from "./tokenStorage";

function createApiError(status: number, message: string): RestrictionApiError {
  return Object.assign(new Error(message), { status });
}

export async function getSignatureCompleteness(): Promise<SignatureCompletenessReportDto> {
  const token = getStoredToken();

  const response = await fetch("/api/SignatureCompleteness", {
    method: "GET",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw createApiError(
      response.status,
      `Could not load signature overview (HTTP ${response.status}).`,
    );
  }

  return response.json();
}
