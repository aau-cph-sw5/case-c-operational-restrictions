
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
            width: "36px",
            height: "36px",
            fontSize: "26px",
            fontWeight: 400,
            background: "#E0F3FF",
            color: "#111827",
            border: "1px solid #2563EB",
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

        <p
          className="notification-category"
          style={{
            fontFamily: "Inter, sans-serif",
            fontSize: "16px",
            color: notification.required ? "#EF7D00" : "#6B6B6B",
          }}
        >
          {notification.category}
        </p>

        <h2
          id="notification-details-title"
          style={{
            paddingRight: "45px",
            color: "#000000",
            fontFamily: '"Inria Serif", serif',
            fontSize: "22px",
            fontWeight: 700,
            lineHeight: 1.3,
            marginTop: "20px",
            marginBottom: "20px",
          }}
        >
          {notification.title}
        </h2>

        <p
          style={{
            color: "#000000",
            fontFamily: '"Inria Serif", serif',
            fontSize: "20px",
            fontWeight: 400,
            lineHeight: 1.3,
            overflowWrap: "anywhere",
          }}
        >
          {notification.message.replace(/\s*\.\.\.$/, ".")}
        </p>

        <p style={{ marginTop: "16px", color: "#6B6B6B" }}>
          <strong>Tidspunkt:</strong> {notification.time}
        </p>

        <p style={{ marginTop: "8px", color: "#6B6B6B" }}>
          <strong>Status:</strong> {notification.status}
        </p>

        {notification.required && (
          <p style={{ marginTop: "12px", color: "#000000" }}>
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
