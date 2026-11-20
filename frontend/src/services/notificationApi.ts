// API:
// Raw HTTP calls to the .NET backend's notification endpoints.
// No side effects and no storage — just request in, response out.
// Parses ISO date strings from the JSON body into Date objects.
import type { NotificationDto } from "../types/notificationDto";
import { getStoredToken } from "./tokenStorage";

// Shape of the JSON body as it arrives over the wire, with dates as ISO strings.
interface NotificationDtoJson {
  id: string;
  operationalRestrictionId: string | null;
  type: NotificationDto["type"];
  title: string | null;
  message: string | null;
  requiresAction: boolean;
  createdAt: string;
  readAt: string | null;
  resolvedAt: string | null;
}

function parseDate(value: string | null): Date | null {
  return value === null ? null : new Date(value);
}

function toNotificationDto(json: NotificationDtoJson): NotificationDto {
  return {
    id: json.id,
    operationalRestrictionId: json.operationalRestrictionId,
    type: json.type,
    title: json.title,
    message: json.message,
    requiresAction: json.requiresAction,
    createdAt: new Date(json.createdAt),
    readAt: parseDate(json.readAt),
    resolvedAt: parseDate(json.resolvedAt),
  };
}

export async function getNotifications(): Promise<NotificationDto[]> {
  const token = getStoredToken();
  if (token === null) {
    throw new Error("Not authenticated.");
  }

  const response = await fetch("/api/notifications", {
    method: "GET",
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`);
  }

  const parsed: NotificationDtoJson[] = await response.json();

  return parsed.map(toNotificationDto);
}
