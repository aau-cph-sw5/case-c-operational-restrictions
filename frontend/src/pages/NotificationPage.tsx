import { useEffect, useState } from "react";
import {
  NotificationCard,
  type Notification,
} from "../components/notifications/NotificationCard";
import {
  NotificationTabs,
  type NotificationTab,
} from "../components/notifications/NotificationTabs";
import { NotificationDetails } from "../components/notifications/NotificationDetails";
import { getNotifications } from "../services/notificationApi";
import { toNotificationViewModel } from "../services/notificationMapper";

export function NotificationPage() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<NotificationTab>("all");
  const [selectedNotification, setSelectedNotification] =
    useState<Notification | null>(null);

  useEffect(() => {
    let cancelled = false;

    getNotifications()
      .then((result) => {
        if (!cancelled) setNotifications(result.map(toNotificationViewModel));
      })
      .catch((err: unknown) => {
        if (!cancelled)
          setError(
            err instanceof Error
              ? err.message
              : "Kunne ikke hente notifikationer.",
          );
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const counts = {
    all: notifications.length,
    unread: notifications.filter((item) => item.unread).length,
    required: notifications.filter((item) => item.required).length,
  };

  const visibleNotifications = notifications.filter((item) => {
    if (activeTab === "unread") return item.unread;
    if (activeTab === "required") return item.required;
    return true;
  });

  function openNotification(notification: Notification) {
    const updatedNotification = {
      ...notification,
      unread: false,
    };

    setNotifications((current) =>
      current.map((item) =>
        item.id === notification.id ? updatedNotification : item,
      ),
    );

    setSelectedNotification(updatedNotification);
  }

  function closeDetails() {
    setSelectedNotification(null);
  }

  function handleSign(notification: Notification) {
    window.alert(
      `Driftsrestriktion ${notification.id} er valgt til signering. ` +
        "Den rigtige signeringsfunktion er endnu ikke tilsluttet.",
    );
  }

  return (
    <main className="box-border flex h-dvh min-h-0 w-full flex-col overflow-hidden bg-[#e0f2fe] text-[#111827] [font-family:Arial,sans-serif] min-[700px]:mx-auto min-[700px]:max-w-[760px] min-[700px]:border-x min-[700px]:border-slate-300">
      <header className="relative shrink-0 border-b-2 border-slate-500 px-4 pt-5 pb-3 text-center min-[700px]:px-5 min-[700px]:pt-6 min-[700px]:pb-4 max-[380px]:px-3 max-[380px]:pt-3.5 max-[380px]:pb-2.5">
        <h1 className="m-0 max-w-full text-[25px] leading-tight font-bold break-words text-[#111827] max-[380px]:text-[22px]">
          Notifikationer
        </h1>
      </header>

      <section className="shrink-0 border-b border-slate-300 px-4 py-3 min-[700px]:px-5 min-[700px]:py-3.5 max-[380px]:px-3 max-[380px]:py-2.5">
        <p className="mb-3 text-base font-semibold text-slate-600">
          {counts.unread} ulæste beskeder
        </p>

        <NotificationTabs
          activeTab={activeTab}
          counts={counts}
          onTabChange={setActiveTab}
        />
      </section>

      <section
        className="flex min-h-0 flex-1 flex-col gap-2.5 overflow-y-auto overscroll-contain px-3 pt-3.5 pb-6 [-webkit-overflow-scrolling:touch] min-[700px]:gap-3 min-[700px]:px-4.5 min-[700px]:pt-5 min-[700px]:pb-7 max-[380px]:gap-2 max-[380px]:px-2 max-[380px]:pt-2.5 max-[380px]:pb-5"
        aria-label="Notifikationer"
      >
        {error !== null ? (
          <p className="text-sm text-red-700">{error}</p>
        ) : isLoading ? (
          <p className="text-sm text-slate-600">Henter notifikationer ...</p>
        ) : visibleNotifications.length === 0 ? (
          <p className="text-sm text-slate-600">
            Ingen notifikationer i denne kategori.
          </p>
        ) : (
          visibleNotifications.map((notification) => (
            <NotificationCard
              key={notification.id}
              notification={notification}
              onOpen={openNotification}
            />
          ))
        )}
      </section>

      {selectedNotification && (
        <NotificationDetails
          notification={selectedNotification}
          onClose={closeDetails}
          onSign={handleSign}
        />
      )}
    </main>
  );
}
