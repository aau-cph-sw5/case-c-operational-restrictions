import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { isRestrictionApiError } from "@/types/restrictionApiError.ts";

export function loadErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    if (error.status === 404) return t.notFound;
    if (error.status === 403) return t.forbiddenView;
    if (error.status === 401) return t.sessionExpired;
  }
  return t.loadFailed;
}
