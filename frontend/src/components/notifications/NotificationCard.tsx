import { useNavigate } from "react-router-dom";
import { NotificationStatus } from "../NotificationStatus";

export interface Notification {
  id: string;
  operationalRestrictionId: string | null;
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
  const navigate = useNavigate();
  const isSystemInfo = notification.category.toLowerCase().includes("system");

  const iconType = notification.required
    ? "required"
    : isSystemInfo
      ? "system"
      : "info";

  const iconStyles = {
    required: "border border-amber-500 bg-amber-100 text-amber-700",
    system: "border border-green-500 bg-green-100 text-green-700",
    info: "border border-gray-400 bg-gray-200 text-gray-500",
  };

  function handleAction() {
    if (
      notification.action === "Se detaljer" &&
      notification.operationalRestrictionId
    ) {
      navigate(
        `/restrictions/${encodeURIComponent(
          notification.operationalRestrictionId,
        )}`,
      );
      return;
    }

    onOpen(notification);
  }

  const cardClasses = [
    "min-w-0 shrink-0 rounded-xl border p-3 text-[#111827] min-[700px]:px-4 min-[700px]:py-3.5 max-[380px]:p-2",
    notification.unread
      ? "border-blue-300 bg-blue-50 text-[#172033]"
      : "border-slate-300 bg-white",
    notification.required ? "needs-action" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const actionClasses = [
    "mt-2.5 inline-block cursor-pointer border-0 bg-transparent py-1.5 text-left text-[15px] font-semibold text-blue-600 focus-visible:outline-[3px] focus-visible:outline-offset-2 focus-visible:outline-blue-300",
    notification.action === "Underskriv nu"
      ? "rounded-full bg-blue-600 px-3.5 py-2.5 text-white hover:bg-blue-700"
      : "hover:underline",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <article className={cardClasses}>
      <div className="grid grid-cols-[28px_minmax(0,1fr)_auto_10px] items-center gap-[7px] max-[380px]:grid-cols-[26px_minmax(0,1fr)_auto_9px] max-[380px]:gap-[5px]">
        <span
          className={`flex size-[27px] shrink-0 items-center justify-center rounded-full text-[17px] font-bold max-[380px]:size-[25px] ${iconStyles[iconType]}`}
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

        <span
          className={`min-w-0 overflow-hidden text-xs font-semibold text-ellipsis whitespace-nowrap min-[700px]:text-[13px] max-[380px]:text-[10px] ${
            notification.required ? "text-amber-700" : "text-slate-500"
          }`}
        >
          {notification.category}
        </span>

        <time className="text-xs whitespace-nowrap text-blue-600 min-[700px]:text-[13px] max-[380px]:text-[10px]">
          {notification.time}
        </time>

        {notification.unread && (
          <span
            className="size-2.5 shrink-0 rounded-full bg-blue-600"
            aria-label="Ulæst"
            title="Ulæst"
          />
        )}
      </div>

      <h2 className="mt-2.5 mb-1.5 text-lg leading-[1.3] font-bold break-words text-[#111827] min-[700px]:text-xl max-[380px]:text-base">
        {notification.title}
      </h2>

      <p className="m-0 text-[15px] leading-relaxed break-words text-gray-700 min-[700px]:text-base max-[380px]:text-sm">
        {notification.message}
      </p>

      <button type="button" className={actionClasses} onClick={handleAction}>
        {notification.action ?? "Se besked"}
      </button>

      <div hidden>
        <NotificationStatus status={notification.status} />
      </div>
    </article>
  );
}
