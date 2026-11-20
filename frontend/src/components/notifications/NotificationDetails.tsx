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
      className="fixed inset-0 z-[1000] flex items-center justify-center bg-slate-900/60 p-4"
      onClick={onClose}
    >
      <section
        className="relative max-h-[85dvh] w-full max-w-[520px] overflow-y-auto rounded-2xl bg-white p-6 text-gray-900 shadow-xl [font-family:Arial,sans-serif]"
        onClick={(event) => event.stopPropagation()}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Luk besked"
          title="Luk besked"
          className="absolute top-3 right-3 z-10 flex size-10 cursor-pointer items-center justify-center rounded-full border border-gray-300 bg-gray-100 text-[28px] leading-none text-gray-900 hover:bg-gray-200 focus-visible:outline-[3px] focus-visible:outline-offset-2 focus-visible:outline-blue-300"
        >
          ×
        </button>

        <p className="text-xs font-semibold text-slate-500">
          {notification.category}
        </p>

        <h2
          id="notification-details-title"
          className="mt-5 mb-5 pr-[45px] text-2xl leading-[1.35] font-bold break-words text-[#172033]"
        >
          {notification.title}
        </h2>

        <p className="my-2.5 leading-relaxed break-words text-gray-700">
          {notification.message.replace(/\s*\.\.\.$/, ".")}
        </p>

        <p className="my-2.5 leading-relaxed">
          <strong>Tidspunkt:</strong> {notification.time}
        </p>

        <p className="my-2.5 leading-relaxed">
          <strong>Status:</strong> {notification.status}
        </p>

        {notification.required && (
          <p className="my-2.5 leading-relaxed">
            Der kræves en handling på denne notifikation.
          </p>
        )}

        {notification.required && onSign && (
          <button
            type="button"
            className="mt-3 inline-flex cursor-pointer items-center justify-center rounded-full bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700 focus-visible:outline-[3px] focus-visible:outline-offset-2 focus-visible:outline-blue-300"
            onClick={() => onSign(notification)}
          >
            Underskriv nu
          </button>
        )}
      </section>
    </div>
  );
}
