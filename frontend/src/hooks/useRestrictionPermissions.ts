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
  cancellationStep: CancellationStep | null;
  reviewComment: string | undefined;
  hasRequestedChanges: boolean;
  showReviewCommentCard: boolean;
}

// The cancellation chain (C5.1-C5.3): the originator starts it on an Active
// restriction, the DOM approves it, and the control room supervisor takes the
// restriction out of force.
export type CancellationStep = "start" | "approve" | "takeOutOfForce";

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

    const isControlRoomSupervisor =
      currentUser !== null &&
      (currentUser.roles === undefined ||
        currentUser.roles.includes("ControlroomSupervisor"));

    const canSignForImplementation =
      Boolean(isAwaitingControlRoomSupervisorApproval) &&
      isControlRoomSupervisor;

    let cancellationStep: CancellationStep | null = null;
    if (restriction?.status === "Active" && isOriginator) {
      cancellationStep = "start";
    } else if (
      restriction?.status === "Closing" &&
      !isOriginator &&
      isDutyOperationsManager
    ) {
      cancellationStep = "approve";
    } else if (
      restriction?.status === "CancellationApproved" &&
      isControlRoomSupervisor
    ) {
      cancellationStep = "takeOutOfForce";
    }

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
      cancellationStep,
      reviewComment,
      hasRequestedChanges,
      showReviewCommentCard,
    };
  }, [restriction, currentUser]);
}
