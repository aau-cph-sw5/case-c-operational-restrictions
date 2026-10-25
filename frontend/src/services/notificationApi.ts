// API:
// Raw HTTP calls to the .NET backend's notification endpoints.
// No side effects and no storage — just request in, response out.
import type { NotificationDto } from "../types/notificationDto";
import { getStoredToken } from "./tokenStorage";

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

  return response.json();
}
