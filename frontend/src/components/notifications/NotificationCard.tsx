import { NotificationStatus } from "../NotificationStatus";

export interface Notification {
  id: string;
  category: string;
  title: string;
  message: string;
  time: string;
  status: "pending" | "sent" | "failed";
  unread: boolean;
  required: boolean;
  action?: string;
}

interface NotificationCardProps {
  notification: Notification;
  onOpen: (notification: Notification) => void;
}

export function NotificationCard({
  notification,
  onOpen,
}: NotificationCardProps) {
  const isSystemInfo = notification.category.toLowerCase().includes("system");

  const iconType = notification.required
    ? "required"
    : isSystemInfo
      ? "system"
      : "info";

  return (
    <article
      className={[
        "notification-card",
        notification.unread ? "unread" : "",
        notification.required ? "needs-action" : "",
        isSystemInfo ? "system-info" : "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <div className="notification-card-top">
        <span
          className={`notification-icon icon-${iconType}`}
          aria-label={
            notification.required
              ? "Kræver underskrift"
              : isSystemInfo
                ? "Systeminformation"
                : "Information"
          }
          role="img"
        >
          !
        </span>

        <span className="notification-category">{notification.category}</span>

        <time className="notification-time">{notification.time}</time>

        {notification.unread && (
          <span
            className="notification-unread-dot"
            aria-label="Ulæst"
            title="Ulæst"
          />
        )}
      </div>

      <h2>{notification.title}</h2>

      <p className="notification-message">{notification.message}</p>

      <button
        type="button"
        className={`notification-action ${
          notification.action === "Underskriv nu" ? "primary" : ""
        }`}
        onClick={() => onOpen(notification)}
      >
        {notification.action ?? "Se besked"}
      </button>

      <div hidden>
        <NotificationStatus status={notification.status} />
      </div>
    </article>
  );
}
