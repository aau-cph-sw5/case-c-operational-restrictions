import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { isRestrictionApiError } from "@/types/restrictionApiError.ts";

export function requestChangesErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    switch (error.status) {
      case 400:
        return t.requestChangesBadRequest;
      case 403:
        return t.requestChangesForbidden;
      case 404:
        return t.notFound;
      case 409:
        return t.requestChangesConflict;
      case 401:
        return t.sessionExpired;
    }
  }
  return t.requestChangesFailed;
}
