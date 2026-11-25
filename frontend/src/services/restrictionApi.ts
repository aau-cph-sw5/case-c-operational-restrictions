// API:
// Raw HTTP calls to the .NET backend's restriction endpoints.
// Attaches the stored JWT and turns non-2xx responses into errors that carry
// the HTTP status, so the UI can show a message per status code.
import { normalizeStatus } from "../lib/restrictionStatus";
import type {
  CreateRestrictionRequest,
  RestrictionDto as CreatedRestrictionDto,
} from "../types/restriction";
import type { RestrictionApiError } from "../types/restrictionApiError";
import type {
  RestrictionDto,
  SignatureDto,
  SignatureStatusDto,
} from "../types/restrictionDto";
import { getStoredToken } from "./tokenStorage";
import { getAuthHeaders } from "./authApi";

// 400 Bad Request so the UI can show every server message.
export class RestrictionValidationError extends Error {
  messages: string[];

  constructor(messages: string[]) {
    super(messages.join(" "));
    this.name = "RestrictionValidationError";
    this.messages = messages;
  }
}

// Reads ASP.NET ProblemDetails.
async function readValidationMessages(response: Response): Promise<string[]> {
  try {
    const body = await response.json();
    if (body?.errors && typeof body === "object") {
      const messages = Object.values(body.errors).flat() as string[];
      if (messages.length > 0) return messages;
    }
    if (typeof body?.detail === "string") return [body.detail];
    if (typeof body?.title === "string") return [body.title];
  } catch {
    // body was not JSON, fall through
  }
  return ["The server rejected the restriction as invalid."];
}

function createApiError(status: number, message: string): RestrictionApiError {
  return Object.assign(new Error(message), { status });
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

function toHistoryEntry(raw: Record<string, unknown>) {
  return {
    action: String(raw.action ?? ""),
    fromState: typeof raw.fromState === "string" ? raw.fromState : null,
    toState: String(raw.toState ?? ""),
    actorId: String(raw.actorId ?? ""),
    occurredAt: String(raw.occurredAt ?? ""),
  };
}

function toRestriction(raw: Record<string, unknown>): RestrictionDto {
  const signatures = Array.isArray(raw.signatures)
    ? raw.signatures
    : raw.originatorSignature
      ? [raw.originatorSignature]
      : [];

  return {
    id: String(raw.id ?? ""),
    title: typeof raw.title === "string" ? raw.title : undefined,
    originatorUserId: String(raw.originatorUserId ?? ""),
    status: normalizeStatus(raw.status ?? raw.state),
    reviewDescription:
      typeof raw.reviewDescription === "string" ? raw.reviewDescription : null,
    startDate: String(raw.startDate ?? ""),
    endDate: raw.endDate ? String(raw.endDate) : null,
    restrictionDescription: String(
      raw.restrictionDescription ?? raw.description ?? "",
    ),
    reference: raw.reference ? String(raw.reference) : null,
    situationDescription: raw.situationDescription
      ? String(raw.situationDescription)
      : undefined,
    cancellationDescription: raw.cancellationDescription
      ? String(raw.cancellationDescription)
      : undefined,
    signatures: signatures.map((s) =>
      toSignature(s as Record<string, unknown>),
    ),
    history: Array.isArray(raw.history)
      ? raw.history.map((entry) =>
          toHistoryEntry(entry as Record<string, unknown>),
        )
      : [],
  };
}

export async function createRestriction(
  request: CreateRestrictionRequest,
  token?: string,
): Promise<CreatedRestrictionDto> {
  const authToken = token ?? getStoredToken();
  const response = await fetch("/api/restrictions", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
    },
    body: JSON.stringify(request),
  });

  if (response.status === 400) {
    throw new RestrictionValidationError(
      await readValidationMessages(response),
    );
  }

  if (!response.ok) {
    throw new Error(`Could not create restriction (HTTP ${response.status}).`);
  }

  return response.json();
}

// GET /api/restrictions/{id}
export async function getRestrictionById(id: string): Promise<RestrictionDto> {
  const response = await fetch(`/api/restrictions/${encodeURIComponent(id)}`, {
    headers: getAuthHeaders(),
  });

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return toRestriction(await response.json());
}

export async function getRestrictions(): Promise<RestrictionDto[]> {
  const response = await fetch("/api/restrictions", {
    headers: getAuthHeaders(),
  });

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  const rawRestrictions: unknown = await response.json();
  if (!Array.isArray(rawRestrictions)) {
    throw new Error("The server returned an invalid restrictions list.");
  }

  return rawRestrictions.map((restriction) => {
    if (restriction === null || typeof restriction !== "object") {
      throw new Error("The server returned an invalid restriction.");
    }

    return toRestriction(restriction as Record<string, unknown>);
  });
}

// POST /api/restrictions/{id}/request-changes
// DOM returns a pending restriction to its originator with a mandatory comment.
// Resolves with the new ReturnedForChanges version.
export async function requestChanges(
  id: string,
  comment: string,
): Promise<RestrictionDto> {
  const response = await fetch(
    `/api/restrictions/${encodeURIComponent(id)}/request-changes`,
    {
      method: "POST",
      headers: { ...getAuthHeaders(), "Content-Type": "application/json" },
      body: JSON.stringify({ comment: comment.trim() }),
    },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return toRestriction(await response.json());
}

// POST /api/restrictions/{id}/approve
// Returns the signed restriction after it advances to implementation approval.
export async function approveRestriction(id: string): Promise<RestrictionDto> {
  const response = await fetch(
    `/api/restrictions/${encodeURIComponent(id)}/approve`,
    { method: "POST", headers: getAuthHeaders() },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return toRestriction(await response.json());
}

// POST /api/restrictions/{id}/sign
// Resolves with the updated restriction when the response body contains one,
// otherwise null (the caller then re-fetches the restriction).
export async function signRestriction(
  id: string,
): Promise<RestrictionDto | null> {
  const response = await fetch(
    `/api/restrictions/${encodeURIComponent(id)}/sign`,
    { method: "POST", headers: getAuthHeaders() },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  try {
    const body: unknown = await response.json();
    if (body && typeof body === "object" && "id" in body && "status" in body) {
      return toRestriction(body as Record<string, unknown>);
    }
  } catch {
    // Empty or non-JSON body: fall through.
  }

  return null;
}

// POST /api/restrictions/{id}/sign-for-implementation
export async function signForImplementation(id: string): Promise<SignatureDto> {
  const response = await fetch(
    `/api/restrictions/${encodeURIComponent(id)}/sign-for-implementation`,
    { method: "POST", headers: getAuthHeaders() },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return toSignature((await response.json()) as Record<string, unknown>);
}

// GET /api/Signature/restriction/{restrictionId}/status
export async function getSignatureStatuses(
  restrictionId: string,
): Promise<SignatureStatusDto[]> {
  const response = await fetch(
    `/api/Signature/restriction/${encodeURIComponent(restrictionId)}/status`,
    {
      headers: getAuthHeaders(),
    },
  );

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  return (await response.json()) as SignatureStatusDto[];
}
