// SERVICE:
// The single entry point pages/components use for authentication.
// Composes the raw HTTP layer (authApi) with token persistence
// (tokenStorage), so callers only need to know about this one object —
// the closest React equivalent to injecting an Angular AuthService.
import { getCurrentUser, login as loginRequest } from './authApi';
import { clearToken, getStoredToken, saveToken } from './tokenStorage';
import type { LoginRequest } from '../types/loginRequest';
import type { LoginResponse } from '../types/loginResponse';

export const authService = {
  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await loginRequest(request);
    saveToken(response.accessToken);
    return response;
  },

  // Route guards call this. It asks the backend to validate the stored
  // token rather than trusting anything decoded from it in the browser.
  async isAuthenticated(): Promise<boolean> {
    const token = getStoredToken();
    if (!token) return false;

    try {
      await getCurrentUser(token);
      return true;
    } catch {
      clearToken();
      return false;
    }
  },

  getStoredToken,
};
