import { ErrorAlert } from "@/components/common/ErrorAlert.tsx";
import { ErrorPage } from "@/components/common/ErrorPage.tsx";
import { SuccessAlert } from "@/components/common/SuccessAlert.tsx";
import { TemporaryBackButton } from "@/components/common/TemporaryBackButton.tsx";
import { RequestedChangesNotice } from "@/components/RestrictionDetail/RequestedChangesNotice.tsx";
import { RestrictionActions } from "@/components/RestrictionDetail/RestrictionActions.tsx";
import { RestrictionDetails } from "@/components/RestrictionDetail/RestrictionDetails.tsx";
import { RestrictionHeader } from "@/components/RestrictionDetail/RestrictionHeader.tsx";
import { ReviewCommentCard } from "@/components/RestrictionDetail/ReviewCommentCard.tsx";
import { pageClasses } from "@/lib/pageLayout.ts";
import { loadErrorMessage } from "@/lib/utils/loadErrorMessage.ts";
import { requestChangesErrorMessage } from "@/lib/utils/requestChangesErrorMessage.ts";
import {
  approveErrorMessage,
  signErrorMessage,
  signForImplementationErrorMessage,
} from "@/lib/utils/signErrorMessages.ts";
import { LoaderCircle } from "lucide-react";
import { useState } from "react";
import { useLocation, useParams } from "react-router-dom";
import { SignatureSection } from "../components/SignatureSection";
import { useRestrictionPermissions } from "../hooks/useRestrictionPermissions";
import { useCurrentUser } from "../hooks/useCurrentUser";
import { useRestriction } from "../hooks/useRestriction";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import {
  getRestrictionById,
  approveRestriction,
  requestChanges,
  signForImplementation,
  signRestriction,
} from "../services/restrictionApi";

export function RestrictionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const [signing, setSigning] = useState(false);
  const [signError, setSignError] = useState<string | null>(null);
  const [signingForImplementation, setSigningForImplementation] =
    useState(false);
  const [signForImplementationError, setSignForImplementationError] = useState<
    string | null
  >(null);
  const [signForImplementationSuccess, setSignForImplementationSuccess] =
    useState(false);
  const [requestingChanges, setRequestingChanges] = useState(false);
  const [requestChangesError, setRequestChangesError] = useState<string | null>(
    null,
  );
  const [requestChangesSuccess, setRequestChangesSuccess] = useState(false);
  const [approving, setApproving] = useState(false);
  const [approveError, setApproveError] = useState<string | null>(null);
  const [approveSuccess, setApproveSuccess] = useState(false);

  const {
    user: currentUser,
    loading: userLoading,
    error: userError,
  } = useCurrentUser();
  const {
    restriction,
    loading: restrictionLoading,
    error: restrictionError,
    setRestriction,
  } = useRestriction(id);

  const loading = restrictionLoading || userLoading;
  const loadError = !id
    ? t.notFound
    : restrictionError
      ? loadErrorMessage(restrictionError)
      : userError
        ? loadErrorMessage(userError)
        : null;

  const navNotification =
    (
      location.state as {
        notification?: string;
        successMessage?: string;
      } | null
    )?.notification ??
    (
      location.state as {
        notification?: string;
        successMessage?: string;
      } | null
    )?.successMessage;

  async function handleSign() {
    if (!restriction) return;

    setSigning(true);
    setSignError(null);

    try {
      // The response may or may not carry the updated restriction; if not,
      // re-fetch this one restriction so the signature block is shown.
      const updated =
        (await signRestriction(restriction.id)) ??
        (await getRestrictionById(restriction.id));
      setRestriction(updated);
    } catch (error) {
      // State stays Draft; the restriction in memory is left untouched.
      setSignError(signErrorMessage(error));
    } finally {
      setSigning(false);
    }
  }

  async function handleSignForImplementation() {
    if (!restriction) return;

    setSigningForImplementation(true);
    setSignForImplementationError(null);
    setSignForImplementationSuccess(false);

    try {
      const signature = await signForImplementation(restriction.id);
      setRestriction({
        ...restriction,
        status: "Active",
        signatures: [...restriction.signatures, signature],
      });
      setSignForImplementationSuccess(true);
    } catch (error) {
      setSignForImplementationError(signForImplementationErrorMessage(error));
    } finally {
      setSigningForImplementation(false);
    }
  }

  async function handleApprove() {
    if (!restriction) return;

    setApproving(true);
    setApproveError(null);
    setApproveSuccess(false);

    try {
      setRestriction(await approveRestriction(restriction.id));
      setApproveSuccess(true);
    } catch (error) {
      setApproveError(approveErrorMessage(error));
    } finally {
      setApproving(false);
    }
  }

  async function handleRequestChanges(comment: string): Promise<boolean> {
    if (!restriction) return false;

    setRequestingChanges(true);
    setRequestChangesError(null);
    setRequestChangesSuccess(false);

    try {
      setRestriction(await requestChanges(restriction.id, comment));
      setRequestChangesSuccess(true);
      return true;
    } catch (error) {
      setRequestChangesError(requestChangesErrorMessage(error));
      return false;
    } finally {
      setRequestingChanges(false);
    }
  }

  const {
    canSign,
    canApprove,
    canRequestChanges,
    canSignForImplementation,
    reviewComment,
    hasRequestedChanges,
    showReviewCommentCard,
  } = useRestrictionPermissions(restriction, currentUser);

  if (loading) {
    return (
      <div className={pageClasses} role="status">
        <p className="flex items-center gap-2 text-[#6B6B6B]">
          <LoaderCircle className="size-4 animate-spin" aria-hidden />
          {t.loading}
        </p>
      </div>
    );
  }

  if (loadError || !restriction) {
    return <ErrorPage message={loadError ?? t.loadFailed} />;
  }

  const knownUsers = currentUser ? { [currentUser.id]: currentUser.name } : {};

  return (
    <main className={pageClasses}>
      <TemporaryBackButton />

      {navNotification && (
        <SuccessAlert testId="navigation-notification">
          {navNotification}
        </SuccessAlert>
      )}

      {requestChangesSuccess && (
        <SuccessAlert testId="request-changes-success">
          {t.requestChangesSuccess}
        </SuccessAlert>
      )}

      {hasRequestedChanges && reviewComment && (
        <RequestedChangesNotice comment={reviewComment} />
      )}

      <RestrictionHeader restriction={restriction} />

      {showReviewCommentCard && reviewComment && (
        <ReviewCommentCard comment={reviewComment} />
      )}

      <RestrictionDetails restriction={restriction} />

      <SignatureSection
        signatures={restriction.signatures}
        knownUsers={knownUsers}
      />
      {approveSuccess && (
        <SuccessAlert testId="approve-success">
          {t.approveSuccess}
        </SuccessAlert>
      )}
      {signForImplementationSuccess && (
        <SuccessAlert testId="sign-for-implementation-success">
          {t.signForImplementationSuccess}
        </SuccessAlert>
      )}

      {signError && <ErrorAlert>{signError}</ErrorAlert>}
      {approveError && <ErrorAlert>{approveError}</ErrorAlert>}

      {signForImplementationError && (
        <ErrorAlert>{signForImplementationError}</ErrorAlert>
      )}

      {requestChangesError && <ErrorAlert>{requestChangesError}</ErrorAlert>}

      <RestrictionActions
        canSign={canSign}
        signing={signing}
        onSign={handleSign}
        canApprove={canApprove}
        approving={approving}
        onApprove={handleApprove}
        canRequestChanges={canRequestChanges}
        submittingChanges={requestingChanges}
        onRequestChanges={handleRequestChanges}
        canSignForImplementation={canSignForImplementation}
        signingForImplementation={signingForImplementation}
        onSignForImplementation={handleSignForImplementation}
      />
    </main>
  );
}
