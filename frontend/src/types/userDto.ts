// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
import type { UserRole } from "../lib/userRole";

export interface UserDto {
  id: string;
  name: string;
  email: string;
  role: UserRole;
}
