// Derives what the current user may see and do with a restriction, so pages don't compute permission flags inline.
import { useMemo } from "react";
import type { RestrictionDto } from "../types/restrictionDto";
import type { UserDto } from "../types/userDto";

export interface RestrictionPermissions {
  isDraft: boolean;
  isOriginator: boolean;
  canSign: boolean;
  isAwaitingDomApproval: boolean;
  canApprove: boolean;
  canRequestChanges: boolean;
  isAwaitingControlRoomSupervisorApproval: boolean;
  canSignForImplementation: boolean;
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

    const isDutyOperationsManager =
      currentUser?.roles?.includes("DutyOperationsManager") ?? false;

    const canApprove =
      Boolean(isAwaitingDomApproval) &&
      currentUser !== null &&
      !isOriginator &&
      isDutyOperationsManager;

    const canRequestChanges =
      Boolean(isAwaitingDomApproval) &&
      currentUser !== null &&
      !isOriginator &&
      isDutyOperationsManager;

    const isAwaitingControlRoomSupervisorApproval =
      restriction?.status === "PendingControlRoomSupervisorApproval";

    const canSignForImplementation =
      Boolean(isAwaitingControlRoomSupervisorApproval) &&
      currentUser !== null &&
      (currentUser.roles === undefined ||
        currentUser.roles.includes("ControlroomSupervisor"));

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
      canApprove,
      canRequestChanges,
      isAwaitingControlRoomSupervisorApproval,
      canSignForImplementation,
      reviewComment,
      hasRequestedChanges,
      showReviewCommentCard,
    };
  }, [restriction, currentUser]);
}
