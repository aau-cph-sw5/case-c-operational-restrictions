import { useCallback, useEffect, useState } from "react";
import { getMyPendingRequiredSignatures } from "../services/requiredSignatureApi";
import type { RequiredSignatureDto } from "../types/requiredSignature";

export interface UsePendingRequiredSignaturesResult {
  requiredSignatures: RequiredSignatureDto[];
  loading: boolean;
  error: unknown;
  reload: () => void;
}

// Loads the logged-in user's pending required signatures. Call reload() after
// signing so the list (and whether the blocking dialog is open) updates.
export function usePendingRequiredSignatures(): UsePendingRequiredSignaturesResult {
  const [requiredSignatures, setRequiredSignatures] = useState<
    RequiredSignatureDto[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    getMyPendingRequiredSignatures()
      .then((loaded) => {
        if (!cancelled) setRequiredSignatures(loaded);
      })
      .catch((fetchError: unknown) => {
        if (!cancelled) setError(fetchError);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [reloadCount]);

  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  return { requiredSignatures, loading, error, reload };
}
