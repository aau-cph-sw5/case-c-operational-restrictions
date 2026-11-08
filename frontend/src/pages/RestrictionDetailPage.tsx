// PAGE:
// Route-level screen component rendered by React Router for a specific URL
import {
  CheckCircle2,
  ChevronLeft,
  FilePen,
  LoaderCircle,
  MessageSquareWarning,
  OctagonAlert,
  Pencil,
} from "lucide-react";
import { useEffect, useState, type ReactNode } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { Alert, AlertDescription } from "../components/ui/alert";
import { Button, buttonVariants } from "../components/ui/button";
import { RestrictionActionButton } from "../components/RestrictionActionButton";
import { RestrictionInfoCard } from "../components/RestrictionInfoCard";
import { RestrictionStatusBadge } from "../components/RestrictionStatusBadge";
import { RequestChangesForm } from "../components/RequestChangesForm";
import { SignatureSection } from "../components/SignatureSection";
import { formatLocal, formatUtc } from "../lib/dateFormat";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import { authService } from "../services/authService";
import {
  getRestrictionById,
  requestChanges,
  signRestriction,
} from "../services/restrictionApi";
import { isRestrictionApiError } from "../types/restrictionApiError";
import type { RestrictionDto } from "../types/restrictionDto";
import type { UserDto } from "../types/userDto";

function loadErrorMessage(error: unknown): string {
  if (isRestrictionApiError(error)) {
    if (error.status === 404) return t.notFound;
    if (error.status === 403) return t.forbiddenView;
    if (error.status === 401) return t.sessionExpired;
  }
  return t.loadFailed;
}

function signErrorMessage(error: unknown): string {
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

function requestChangesErrorMessage(error: unknown): string {
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

// Important-notification style from the design guide: Beeswax background,
// Gold Drop text, octagon alert symbol.
function ErrorAlert({ children }: { children: ReactNode }) {
  return (
    <Alert className="border-[#EF7D00] bg-[#FEF3C7] font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]">
      <OctagonAlert aria-hidden />
      <AlertDescription className="text-base">{children}</AlertDescription>
    </Alert>
  );
}

// TEMPORARY: stands in for the app header's back chevron until the shared
// header/navigation exists.
function TemporaryBackButton() {
  const navigate = useNavigate();
  return (
    <Button
      variant="ghost"
      onClick={() => navigate("/home")}
      className="-ml-2 gap-1 self-start font-['Inter_Variable','Inter',sans-serif] text-base font-medium text-[#2563EB] hover:text-[#2563EB]"
    >
      <ChevronLeft aria-hidden />
      {t.back}
    </Button>
  );
}

const pageClasses =
  "mx-auto flex min-h-screen max-w-md flex-col gap-4 bg-white p-4 text-left font-['Inria_Serif',serif] text-[20px] text-black";

export function RestrictionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const [restriction, setRestriction] = useState<RestrictionDto | null>(null);
  const [currentUser, setCurrentUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [signing, setSigning] = useState(false);
  const [signError, setSignError] = useState<string | null>(null);
  const [requestingChanges, setRequestingChanges] = useState(false);
  const [requestChangesError, setRequestChangesError] = useState<string | null>(
    null,
  );
  const [requestChangesSuccess, setRequestChangesSuccess] = useState(false);

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

  useEffect(() => {
    if (!id) {
      setLoadError(t.notFound);
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setLoadError(null);

    // Only the requested restriction and the current user are fetched.
    Promise.all([getRestrictionById(id), authService.getCurrentUser()])
      .then(([loadedRestriction, user]) => {
        if (cancelled) return;
        setRestriction(loadedRestriction);
        setCurrentUser(user);
      })
      .catch((error: unknown) => {
        if (!cancelled) setLoadError(loadErrorMessage(error));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

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
    return (
      <div className={pageClasses}>
        <TemporaryBackButton />
        <ErrorAlert>{loadError ?? t.loadFailed}</ErrorAlert>
        <Link
          to="/home"
          className={buttonVariants({
            variant: "link",
            className:
              "self-start font-['Inter_Variable','Inter',sans-serif] text-base text-[#2563EB]",
          })}
        >
          {t.backToHome}
        </Link>
      </div>
    );
  }

  const isDraft = restriction.status === "Draft";
  const isOriginator =
    currentUser !== null && currentUser.id === restriction.originatorUserId;
  const canSign = isDraft && isOriginator;
  // The backend enforces the DOM role; the role check here only hides the form
  // once the current-user response includes a role.
  const isAwaitingDomApproval =
    restriction.status === "PendingDOMApproval" ||
    restriction.status === "PendingApproval";
  const canRequestChanges =
    isAwaitingDomApproval &&
    currentUser !== null &&
    !isOriginator &&
    (currentUser.role === undefined ||
      currentUser.role === "DutyOperationsManager");
  const reviewComment =
    restriction.status === "ReturnedForChanges"
      ? restriction.reviewDescription?.trim()
      : undefined;
  // The originator sees the comment in the action notice above instead.
  const hasRequestedChanges = Boolean(reviewComment) && isOriginator;
  const showReviewCommentCard = Boolean(reviewComment) && !isOriginator;

  const knownUsers = currentUser ? { [currentUser.id]: currentUser.name } : {};

  return (
    <main className={pageClasses}>
      <TemporaryBackButton />

      {navNotification && (
        <Alert
          data-testid="navigation-notification"
          className="border-[#37AB87] bg-[#DAFBEA] font-['Inter_Variable','Inter',sans-serif] text-base text-[#1E745B]"
        >
          <CheckCircle2 className="size-5 text-[#37AB87]" aria-hidden />
          <AlertDescription className="text-base text-[#1E745B]">
            {navNotification}
          </AlertDescription>
        </Alert>
      )}

      {requestChangesSuccess && (
        <Alert
          data-testid="request-changes-success"
          className="border-[#37AB87] bg-[#DAFBEA] font-['Inter_Variable','Inter',sans-serif] text-base text-[#1E745B]"
        >
          <CheckCircle2 className="size-5 text-[#37AB87]" aria-hidden />
          <AlertDescription className="text-base text-[#1E745B]">
            {t.requestChangesSuccess}
          </AlertDescription>
        </Alert>
      )}

      {hasRequestedChanges && (
        <Alert
          data-testid="requested-changes-notice"
          className="border-[#EF7D00] bg-[#FEF3C7] font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]"
        >
          <OctagonAlert aria-hidden />
          <AlertDescription className="text-base">
            <p className="font-bold">{t.requestedChanges}</p>
            <p>
              {t.requestedChangesComment}: {restriction.reviewDescription}
            </p>
          </AlertDescription>
        </Alert>
      )}

      <header className="space-y-1">
        <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#2563EB]">
          {t.screenLabel}
        </p>
        <h1 className="font-['Instrument_Sans',sans-serif] text-2xl font-bold break-all text-black">
          {restriction.id}
        </h1>
        {restriction.reference && (
          <p className="font-['Instrument_Sans',sans-serif] text-base text-[#6B6B6B]">
            Ref: {restriction.reference}
          </p>
        )}
        <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]">
          {t.idLabel}
        </p>
        <div className="pt-1">
          <RestrictionStatusBadge status={restriction.status} />
        </div>
      </header>

      {showReviewCommentCard && (
        <section
          data-testid="review-comment-callout"
          aria-label={t.requestedChangesComment}
          className="rounded-lg border-2 border-l-8 border-[#EF7D00] bg-[#FEF3C7] p-4 font-['Inter_Variable','Inter',sans-serif] shadow-md"
        >
          <h2 className="flex items-center gap-2 text-lg font-bold text-[#EF7D00]">
            <MessageSquareWarning className="size-6 shrink-0" aria-hidden />
            {t.requestedChangesComment}
          </h2>
          <p
            data-testid="review-comment"
            className="mt-2 text-[20px] font-semibold whitespace-pre-wrap text-black"
          >
            {reviewComment}
          </p>
        </section>
      )}

      <section aria-label="Detaljer" className="space-y-2">
        <RestrictionInfoCard label={t.startDate}>
          <p data-testid="start-local">{formatLocal(restriction.startDate)}</p>
          <p
            data-testid="start-utc"
            className="text-[20px] font-normal text-[#6B6B6B]"
          >
            {formatUtc(restriction.startDate)}
          </p>
        </RestrictionInfoCard>

        <RestrictionInfoCard label={t.endDate}>
          <p data-testid="end-local">
            {restriction.endDate
              ? formatLocal(restriction.endDate)
              : t.noEndDate}
          </p>
          {restriction.endDate && (
            <p
              data-testid="end-utc"
              className="text-[20px] font-normal text-[#6B6B6B]"
            >
              {formatUtc(restriction.endDate)}
            </p>
          )}
        </RestrictionInfoCard>

        <RestrictionInfoCard label={t.description}>
          <p className="whitespace-pre-wrap">
            {restriction.restrictionDescription || t.noDescription}
          </p>
        </RestrictionInfoCard>
      </section>

      <SignatureSection
        signatures={restriction.signatures}
        knownUsers={knownUsers}
      />

      {signError && <ErrorAlert>{signError}</ErrorAlert>}

      {requestChangesError && <ErrorAlert>{requestChangesError}</ErrorAlert>}

      {canRequestChanges && (
        <RequestChangesForm
          submitting={requestingChanges}
          onSubmit={handleRequestChanges}
        />
      )}

      {canSign && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={handleSign}
            loading={signing}
            loadingLabel={t.signing}
            icon={<Pencil className="size-4" aria-hidden />}
            title="Sign and Submit for Approval"
          >
            {t.sign}
          </RestrictionActionButton>

          {/* TEMPORARY: edit flow is not implemented yet. */}
          <RestrictionActionButton
            disabled
            title={t.editComingSoon}
            icon={<FilePen className="size-4" aria-hidden />}
          >
            {t.edit}
          </RestrictionActionButton>
        </div>
      )}
    </main>
  );
}
