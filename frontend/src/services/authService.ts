// SERVICE:
// The single entry point pages/components use for authentication.
// Composes the raw HTTP layer (authApi) with token persistence
// (tokenStorage), so callers only need to know about this one object —
// the closest React equivalent to injecting an Angular AuthService.
import { login as loginRequest } from './authApi';
import { getStoredToken, saveToken } from './tokenStorage';
import type { LoginRequest } from '../types/loginRequest';
import type { LoginResponse } from '../types/loginResponse';

export const authService = {
  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await loginRequest(request);
    saveToken(response.accessToken);
    return response;
  },

  getStoredToken,
};
