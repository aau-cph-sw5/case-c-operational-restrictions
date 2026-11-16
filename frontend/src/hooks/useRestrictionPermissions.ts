// Derives what the current user may see and do with a restriction, so pages don't compute permission flags inline.
import { useMemo } from "react";
import type { RestrictionDto } from "../types/restrictionDto";
import type { UserDto } from "../types/userDto";

export interface RestrictionPermissions {
  isDraft: boolean;
  isOriginator: boolean;
  canSign: boolean;
  isAwaitingDomApproval: boolean;
  canRequestChanges: boolean;
  reviewComment: string | undefined;
  hasRequestedChanges: boolean;
  showReviewCommentCard: boolean;
}

export function useRestrictionPermissions(
  restriction: RestrictionDto | null,
  currentUser: UserDto | null,
): RestrictionPermissions {
  return useMemo(() => {
    const isDraft = restriction?.status === "Draft";

    const isOriginator =
      restriction !== null &&
      currentUser !== null &&
      currentUser.id === restriction.originatorUserId;

    const canSign = isDraft && isOriginator;

    const isAwaitingDomApproval =
      restriction?.status === "PendingDOMApproval" ||
      restriction?.status === "PendingApproval";

    const canRequestChanges =
      Boolean(isAwaitingDomApproval) &&
      currentUser !== null &&
      !isOriginator &&
      (currentUser.roles === undefined ||
        currentUser.roles.includes("DutyOperationsManager"));

    const reviewComment =
      restriction?.status === "ReturnedForChanges"
        ? restriction.reviewDescription?.trim()
        : undefined;

    const hasRequestedChanges = Boolean(reviewComment) && isOriginator;

    const showReviewCommentCard = Boolean(reviewComment) && !isOriginator;

    return {
      isDraft,
      isOriginator,
      canSign,
      isAwaitingDomApproval,
      canRequestChanges,
      reviewComment,
      hasRequestedChanges,
      showReviewCommentCard,
    };
  }, [restriction, currentUser]);
}
