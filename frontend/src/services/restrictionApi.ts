// API:
// Raw HTTP calls to the .NET backend's restriction endpoints.
// Attaches the stored JWT and turns non-2xx responses into errors that carry the HTTP status, so the UI can show a message per status code.
import { normalizeStatus } from "../lib/restrictionStatus";
import type { RestrictionApiError } from "../types/restrictionApiError";
import type { RestrictionDto, SignatureDto } from "../types/restrictionDto";
import { getStoredToken } from "./tokenStorage";

function createApiError(status: number, message: string): RestrictionApiError {
  return Object.assign(new Error(message), { status });
}

function authHeaders(): HeadersInit {
  const token = getStoredToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

// Backend enum order for UserRole (Originator, Dom, Operator).
const numericRoles = ["Originator", "Dom", "Operator"];

function toSignature(raw: Record<string, unknown>): SignatureDto {
  const role = raw.signeeRole;
  return {
    signeeId: String(raw.signeeId ?? ""),
    signeeName: typeof raw.signeeName === "string" ? raw.signeeName : undefined,
    signeeRole:
      typeof role === "number"
        ? (numericRoles[role] ?? "Unknown")
        : String(role ?? "Unknown"),
    signedAt: String(raw.signedAt ?? ""),
  };
}

function toRestriction(raw: Record<string, unknown>): RestrictionDto {
  const signatures = Array.isArray(raw.signatures)
    ? raw.signatures
    : raw.originatorSignature
      ? [raw.originatorSignature]
      : [];

  return {
    id: String(raw.id),
    originatorUserId: String(raw.originatorUserId),
    status: normalizeStatus(raw.status),
    startDate: String(raw.startDate),
    endDate: String(raw.endDate),
    restrictionDescription: String(raw.restrictionDescription ?? ""),
    signatures: signatures.map((s) =>
      toSignature(s as Record<string, unknown>),
    ),
  };
}

// GET /api/restrictions/{id}
export async function getRestrictionById(id: string): Promise<RestrictionDto> {
  const response = await fetch(`/api/restrictions/${encodeURIComponent(id)}`, {
    headers: authHeaders(),
  });

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return toRestriction(await response.json());
}

// POST /api/restrictions/{id}/sign
// Resolves with the updated restriction when the response body contains one, otherwise null (the caller then re-fetches the restriction).
export async function signRestriction(
  id: string,
): Promise<RestrictionDto | null> {
  const response = await fetch(
    `/api/restrictions/${encodeURIComponent(id)}/sign`,
    { method: "POST", headers: authHeaders() },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  try {
    const body: unknown = await response.json();
    if (body && typeof body === "object" && "id" in body) {
      return toRestriction(body as Record<string, unknown>);
    }
  } catch {
    // Empty or non-JSON body: fall through.
  }

  return null;
}
