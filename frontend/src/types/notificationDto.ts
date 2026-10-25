// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
export type NotificationType =
  | "SignatureRequired"
  | "ChangesRequested"
  | "SystemInfo";

export interface NotificationDto {
  id: string;
  operationalRestrictionId: string | null;
  type: NotificationType;
  title: string | null;
  message: string | null;
  requiresAction: boolean;
  createdAt: string;
  readAt: string | null;
  resolvedAt: string | null;
}
