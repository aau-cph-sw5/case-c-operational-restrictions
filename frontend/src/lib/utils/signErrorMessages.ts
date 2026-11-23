import { restrictionTexts as t } from "@/lib/restrictionTexts.ts";
import { isRestrictionApiError } from "@/types/restrictionApiError.ts";

export function signErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    switch (error.status) {
      case 400:
        return t.signBadRequest;
      case 403:
        return t.signForbidden;
      case 404:
        return t.notFound;
      case 409:
        return t.signConflict;
      case 401:
        return t.sessionExpired;
    }
  }
  return t.signFailed;
}

// Same status mapping as signErrorMessage, but for the control room
// supervisor's sign-for-implementation endpoint.
export function signForImplementationErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    switch (error.status) {
      case 403:
        return t.signForImplementationForbidden;
      case 404:
        return t.notFound;
      case 409:
        return t.signForImplementationConflict;
      case 401:
        return t.sessionExpired;
    }
  }
  return t.signFailed;
}

export function approveErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    switch (error.status) {
      case 401:
        return t.sessionExpired;
      case 403:
        return t.approveForbidden;
      case 404:
        return t.notFound;
      case 409:
        return t.approveConflict;
    }
  }
  return t.approveFailed;
}
