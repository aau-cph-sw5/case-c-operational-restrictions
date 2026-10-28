// TYPES / MODELS:
// TypeScript interfaces and types defining domain models, state, and API contracts (matching backend DTOs)
// Dates are Date objects; the ISO strings from the JSON body are parsed in notificationApi.
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
  createdAt: Date;
  readAt: Date | null;
  resolvedAt: Date | null;
}
