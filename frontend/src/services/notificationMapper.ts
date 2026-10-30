// SERVICE:
// Maps backend NotificationDto objects to the view model used by the
// notification components (category, time, status, unread, required, action).
import type { Notification } from "../components/notifications/NotificationCard";
import type { NotificationDto } from "../types/notificationDto";

function formatTime(createdAt: Date): string {
  const minutes = Math.floor((Date.now() - createdAt.getTime()) / 60_000);

  if (minutes < 1) return "Lige nu";
  if (minutes < 60) return `${minutes} min. siden`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return hours === 1 ? "1 time siden" : `${hours} timer siden`;

  const days = Math.floor(hours / 24);
  if (days === 1) return "I går";
  if (days < 7) return `${days} dage siden`;

  return new Intl.DateTimeFormat("da-DK", {
    day: "numeric",
    month: "short",
  }).format(createdAt);
}

function categoryFor(type: NotificationDto["type"]): string {
  return type === "SystemInfo" ? "SYSTEM INFO" : "DRIFTSRESTRIKTION";
}

function titleFor(dto: NotificationDto): string {
  if (dto.title !== null) return dto.title;
  switch (dto.type) {
    case "SignatureRequired":
      return "Signatur påkrævet";
    case "ChangesRequested":
      return "Ændringer påkrævet";
    default:
      return "Systeminformation";
  }
}

function actionFor(
  dto: NotificationDto,
  required: boolean,
): string | undefined {
  if (!required) return undefined;
  return dto.type === "SignatureRequired" ? "Underskriv nu" : "Se detaljer";
}

export function toNotificationViewModel(dto: NotificationDto): Notification {
  const required = dto.requiresAction && dto.resolvedAt === null;

  return {
    id: dto.id,
    category: categoryFor(dto.type),
    title: titleFor(dto),
    message: dto.message ?? "",
    time: formatTime(dto.createdAt),
    // The in-app notification is always available; push delivery status is not
    // part of the API contract, so it is reported as sent.
    status: "sent",
    unread: dto.readAt === null,
    required,
    action: actionFor(dto, required),
  };
}
