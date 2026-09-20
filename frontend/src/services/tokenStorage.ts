// STORAGE:
// Owns persistence of the JWT access token in the browser.
// Kept separate from authService so HTTP concerns and storage/business
// decisions don't mix in the same module.
const TOKEN_STORAGE_KEY = 'accessToken';

export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_STORAGE_KEY, token);
}

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_STORAGE_KEY);
}
