// API:
// Fetches the restrictions shown on the overview page from the .NET backend
// (GET /api/restrictions) and maps them to the card view model.
import type { DRStatus } from "@/components/OverviewNotificationCard";
import {
  BACKEND_RESTRICTION_STATES,
  type BackendRestrictionState,
  type DrItem,
  type OverviewCategory,
  type OverviewRestrictionDto,
} from "../types/overviewRestriction";
import type { RestrictionApiError } from "../types/restrictionApiError";
import { getStoredToken } from "./tokenStorage";
import { getCurrentUser } from "./authApi";

function createApiError(status: number, message: string): RestrictionApiError {
  return Object.assign(new Error(message), { status });
}

function authHeaders(): HeadersInit {
  const token = getStoredToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function getCurrentUserId(): Promise<string | null> {
  const token = getStoredToken();
  if (!token) return null;
  const user = await getCurrentUser(token);
  return user?.id ?? null;
}

function normalizeBackendState(raw: unknown): BackendRestrictionState {
  if (typeof raw === "number") {
    return BACKEND_RESTRICTION_STATES[raw] ?? "Unknown";
  }
  if (typeof raw === "string") {
    const match = BACKEND_RESTRICTION_STATES.find(
      (state) => state.toLowerCase() === raw.toLowerCase(),
    );
    return match ?? "Unknown";
  }
  return "Unknown";
}

// Which tab a restriction belongs to and which card style it uses.
// Restrictions in an approval step only appear under "Alle".
const CARD_STATUS: Record<
  BackendRestrictionState,
  { statusKey: DRStatus; category: OverviewCategory }
> = {
  Draft: { statusKey: "kladde", category: "kladde" },
  ReturnedForChanges: { statusKey: "kladde", category: "kladde" },
  Empty: { statusKey: "kladde", category: "kladde" },
  Active: { statusKey: "aktiv", category: "aktiv" },
  AwaitingOperatorSignatures: { statusKey: "aktiv", category: "aktiv" },
  PendingCancellation: {
    statusKey: "afventer_deaktivering",
    category: "aktiv",
  },
  AwaitingOperatorSignOut: {
    statusKey: "afventer_deaktivering",
    category: "aktiv",
  },
  Archived: { statusKey: "arkiv", category: "arkiv" },
  OutOfForce: { statusKey: "arkiv", category: "arkiv" },
  CancelledRefused: { statusKey: "arkiv", category: "arkiv" },
  PendingDutyOperationsManagerApproval: {
    statusKey: "afventer_godkendelse",
    category: "alle",
  },
  PendingControlRoomSupervisorApproval: {
    statusKey: "afventer_godkendelse",
    category: "alle",
  },
  Approved: { statusKey: "afventer_godkendelse", category: "alle" },
  Unknown: { statusKey: "arkiv", category: "alle" },
};

function toDrItem(dto: OverviewRestrictionDto, currentUserId: string | null): DrItem {
  const { statusKey, category } = CARD_STATUS[normalizeBackendState(dto.state)];

  return {
    id: dto.id,
    referenceId: dto.reference ?? "",
    title: dto.title,
    date: dto.startDate,
    signed: currentUserId !== null && dto.signatures.some(signature => signature.signeeId === currentUserId),
    statusKey,
    category,
  };
}

// GET /api/restrictions
// Returns all non-draft restrictions plus the logged-in user's own drafts.
export async function getOverviewRestrictions(
  signal?: AbortSignal,
): Promise<DrItem[]> {
  const response = await fetch("/api/restrictions", {
    headers: authHeaders(),
    signal,
  });

  if (!response.ok) {
    throw createApiError(response.status, `HTTP ${response.status}`);
  }

  
  const currentUserId = await getCurrentUserId();
  const body = (await response.json()) as OverviewRestrictionDto[];
  return body.map((dto) => toDrItem(dto, currentUserId));
}
