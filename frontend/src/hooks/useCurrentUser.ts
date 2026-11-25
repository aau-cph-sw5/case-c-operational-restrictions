import { useEffect, useState } from "react";
import { authService } from "../services/authService";
import type { UserDto } from "../types/userDto";

export interface UseCurrentUserResult {
  user: UserDto | null;
  loading: boolean;
  error: unknown;
}

export function useCurrentUser(): UseCurrentUserResult {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    authService
      .getCurrentUser()
      .then((loadedUser) => {
        if (!cancelled) setUser(loadedUser);
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
  }, []);

  return { user, loading, error };
}
