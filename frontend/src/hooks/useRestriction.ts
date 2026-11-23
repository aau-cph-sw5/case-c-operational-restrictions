import { useEffect, useState } from "react";
import { getRestrictionById } from "../services/restrictionApi";
import type { RestrictionDto } from "../types/restrictionDto";

export interface UseRestrictionResult {
  restriction: RestrictionDto | null;
  loading: boolean;
  error: unknown;
  setRestriction: (restriction: RestrictionDto | null) => void;
}

export function useRestriction(id: string | undefined): UseRestrictionResult {
  const [restriction, setRestriction] = useState<RestrictionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    if (!id) {
      setRestriction(null);
      setError(null);
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    getRestrictionById(id)
      .then((loadedRestriction) => {
        if (!cancelled) setRestriction(loadedRestriction);
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
  }, [id]);

  return { restriction, loading, error, setRestriction };
}
