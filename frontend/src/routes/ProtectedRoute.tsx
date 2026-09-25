import { useEffect, useState } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { authService } from '../services/authService';

function useIsAuthenticated(): boolean | null {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);

  useEffect(() => {
    let cancelled = false;

    authService
      .isAuthenticated()
      .then((result) => {
        if (!cancelled) setIsAuthenticated(result);
      })
      .catch((error) => console.error('Session check failed', error));

    return () => {
      cancelled = true;
    };
  }, []);

  return isAuthenticated;
}

export function ProtectedRoute() {
  const isAuthenticated = useIsAuthenticated();

  if (isAuthenticated === null) {
    return <p className="container">Checking session...</p>;
  }

  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
