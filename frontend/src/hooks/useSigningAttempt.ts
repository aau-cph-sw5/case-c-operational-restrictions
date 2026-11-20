// Runs one signing action so that it is either completed or not, never half-done.
// The UI only reports success once the server has confirmed the signature. If the
// connection drops mid-request, the hook asks the server what happened to the
// attempt instead of guessing, and offers no new sign action while that is unknown.
import { useRef, useState } from "react";
import { newAttemptKey } from "../lib/signingAttempt";
import { restrictionTexts as t } from "../lib/restrictionTexts";
import { getSigningAttemptStatus } from "../services/restrictionApi";
import { isRestrictionApiError } from "../types/restrictionApiError";

export type SigningPhase = "idle" | "signing" | "outcomeUnknown";

interface UseSigningAttemptOptions<TResult> {
  // Sends the signing request with the attempt key.
  send: (attemptKey: string) => Promise<TResult>;
  // Called once the signature is confirmed. `result` is undefined when the
  // confirmation came from the status check and not from the response.
  onSigned: (result: TResult | undefined) => Promise<void> | void;
  errorMessage: (error: unknown) => string;
}

export interface UseSigningAttemptResult {
  phase: SigningPhase;
  error: string | null;
  start: () => Promise<void>;
  checkOutcome: () => Promise<void>;
}

// The server answered with a client error: it did not sign. Anything else
// (no response, a 5xx) leaves the outcome unknown until the server is asked.
function isDefiniteRefusal(error: unknown): boolean {
  return isRestrictionApiError(error) && error.status < 500;
}

export function useSigningAttempt<TResult>({
  send,
  onSigned,
  errorMessage,
}: UseSigningAttemptOptions<TResult>): UseSigningAttemptResult {
  // Kept until the outcome is known, so a retry reuses the same key.
  const attemptKey = useRef<string | null>(null);
  const [phase, setPhase] = useState<SigningPhase>("idle");
  const [error, setError] = useState<string | null>(null);

  async function confirmSigned(result?: TResult) {
    attemptKey.current = null;
    try {
      await onSigned(result);
      setError(null);
    } catch {
      setError(t.signedButRefreshFailed);
    }
    setPhase("idle");
  }

  async function findOutcome(key: string) {
    setPhase("signing");
    try {
      const status = await getSigningAttemptStatus(key);
      if (status.status === "Signed") {
        await confirmSigned();
        return;
      }
      // Nothing was recorded. The key is kept: using it again cannot sign twice.
      setError(t.signNotRecorded);
      setPhase("idle");
    } catch (statusError) {
      if (isRestrictionApiError(statusError) && statusError.status === 401) {
        setError(t.sessionExpired);
        setPhase("idle");
        return;
      }
      setPhase("outcomeUnknown");
    }
  }

  async function start() {
    const key = attemptKey.current ?? newAttemptKey();
    attemptKey.current = key;
    setPhase("signing");
    setError(null);

    let result: TResult;
    try {
      result = await send(key);
    } catch (sendError) {
      if (isDefiniteRefusal(sendError)) {
        attemptKey.current = null;
        setError(errorMessage(sendError));
        setPhase("idle");
        return;
      }
      await findOutcome(key);
      return;
    }

    await confirmSigned(result);
  }

  async function checkOutcome() {
    if (attemptKey.current) await findOutcome(attemptKey.current);
  }

  return { phase, error, start, checkOutcome };
}
