// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
export interface UserDto {
  id: string;
  name: string;
  email: string;
  // Not sent by the backend yet; when present it hides DOM-only actions from other roles.
  role?: string;
}
