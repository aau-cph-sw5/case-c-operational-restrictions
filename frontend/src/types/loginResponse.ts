// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
import type { UserDto } from './userDto';

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: UserDto;
}
