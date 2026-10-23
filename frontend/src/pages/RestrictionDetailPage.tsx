// PAGE:
// Route-level screen component rendered by React Router for a specific URL
import {
  ChevronLeft,
  FilePen,
  LoaderCircle,
  OctagonAlert,
  Pencil,
} from "lucide-react";
import { useEffect, useState, type ReactNode } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { RestrictionActionButton } from "../components/RestrictionActionButton";
import { RestrictionInfoCard } from "../components/RestrictionInfoCard";
import { RestrictionStatusBadge } from "../components/RestrictionStatusBadge";
import { SignatureSection } from "../components/SignatureSection";
import { formatLocal, formatUtc } from "../lib/dateFormat";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import { authService } from "../services/authService";
import {
  getRestrictionById,
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

// Important-notification style from the design guide
function ErrorAlert({ children }: { children: ReactNode }) {
  return (
    <div
      role="alert"
      className="flex items-start gap-2 rounded-lg border border-[#EF7D00] bg-[#FEF3C7] p-3 font-['Inter_Variable','Inter',sans-serif] text-base text-[#EF7D00]"
    >
      <OctagonAlert className="mt-0.5 size-5 shrink-0" aria-hidden />
      <span>{children}</span>
    </div>
  );
}

// TEMPORARY: stands in for the app header's back chevron until the shared
function TemporaryBackButton() {
  const navigate = useNavigate();
  return (
    <button
      type="button"
      onClick={() => navigate("/home")}
      className="inline-flex items-center gap-1 self-start font-['Inter_Variable','Inter',sans-serif] text-base font-medium text-[#2563EB]"
    >
      <ChevronLeft className="size-5" aria-hidden />
      {t.back}
    </button>
  );
}

const pageClasses =
  "mx-auto flex min-h-screen max-w-md flex-col gap-4 bg-white p-4 text-left font-['Inria_Serif',serif] text-[20px] text-black";

export function RestrictionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [restriction, setRestriction] = useState<RestrictionDto | null>(null);
  const [currentUser, setCurrentUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [signing, setSigning] = useState(false);
  const [signError, setSignError] = useState<string | null>(null);

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
      // The response may or may not carry the updated restriction; if not,re-fetch this one restriction so the signature block is shown.
      const updated =
        (await signRestriction(restriction.id)) ??
        (await getRestrictionById(restriction.id));
      setRestriction(updated);
    } catch (error) {
      setSignError(signErrorMessage(error));
    } finally {
      setSigning(false);
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
          className="font-['Inter_Variable','Inter',sans-serif] text-base text-[#2563EB] underline"
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

  const knownUsers = currentUser ? { [currentUser.id]: currentUser.name } : {};

  return (
    <main className={pageClasses}>
      <TemporaryBackButton />

      <header className="space-y-1">
        <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#2563EB]">
          {t.screenLabel}
        </p>
        <h1 className="font-['Instrument_Sans',sans-serif] text-2xl font-bold break-all text-black">
          {restriction.id}
        </h1>
        <p className="font-['Instrument_Sans',sans-serif] text-base font-bold text-[#6B6B6B]">
          {t.idLabel}
        </p>
        <div className="pt-1">
          <RestrictionStatusBadge status={restriction.status} />
        </div>
      </header>

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
          <p data-testid="end-local">{formatLocal(restriction.endDate)}</p>
          <p
            data-testid="end-utc"
            className="text-[20px] font-normal text-[#6B6B6B]"
          >
            {formatUtc(restriction.endDate)}
          </p>
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

      {canSign && (
        <div className="flex flex-wrap gap-3 pb-4">
          <RestrictionActionButton
            onClick={handleSign}
            loading={signing}
            loadingLabel={t.signing}
            icon={<Pencil className="size-4" aria-hidden />}
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
