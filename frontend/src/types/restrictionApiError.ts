// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
export type RestrictionApiError = Error & { status: number };

export function isRestrictionApiError(
  error: unknown,
): error is RestrictionApiError {
  return (
    error instanceof Error &&
    typeof (error as { status?: unknown }).status === "number"
  );
}
