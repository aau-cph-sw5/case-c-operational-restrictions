
import type { Notification } from "./NotificationCard";

interface NotificationDetailsProps {
  notification: Notification;
  onClose: () => void;
  onSign?: (notification: Notification) => void;
}

export function NotificationDetails({
  notification,
  onClose,
  onSign,
}: NotificationDetailsProps) {
  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="notification-details-title"
      className="notification-details-overlay"
      onClick={onClose}
    >
      <section
        className="notification-details"
        style={{ position: "relative" }}
        onClick={(event) => event.stopPropagation()}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Luk besked"
          title="Luk besked"
          style={{
            position: "absolute",
            top: "12px",
            right: "12px",
            width: "40px",
            height: "40px",
            fontSize: "28px",
            fontWeight: "bold",
            background: "#eeeeee",
            color: "#111111",
            border: "1px solid #cccccc",
            borderRadius: "50%",
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            zIndex: 10,
          }}
        >
          ×
        </button>

        <p className="notification-category">
          {notification.category}
        </p>

            <h2
            id="notification-details-title"
            style={{
                paddingRight: "45px",
                color: "#172033",
                fontSize: "24px",
                fontWeight: 700,
                marginTop: "20px",
                marginBottom: "20px",
            }}
            >
            {notification.title}
            </h2>

        <p>
          {notification.message.replace(/\s*\.\.\.$/, ".")}
        </p>

        <p>
          <strong>Tidspunkt:</strong> {notification.time}
        </p>

        <p>
          <strong>Status:</strong> {notification.status}
        </p>

        {notification.required && (
          <p>
            Der kræves en handling på denne notifikation.
          </p>
        )}

        {notification.required && onSign && (
          <button
            type="button"
            className="notification-action primary"
            onClick={() => onSign(notification)}
          >
            Underskriv nu
          </button>
        )}
      </section>
    </div>
  );
}
