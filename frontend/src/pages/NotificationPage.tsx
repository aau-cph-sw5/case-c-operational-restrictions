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
    <main className="notification-page">
      <style>{`
        .notification-page {
          box-sizing: border-box;
          width: 100%;
          height: 100vh;
          height: 100dvh;
          min-height: 0;
          display: flex;
          flex-direction: column;
          overflow: hidden;
          background: #e0f2fe;
          color: #111827;
          font-family: Arial, sans-serif;
        }

        .notification-page *,
        .notification-page *::before,
        .notification-page *::after {
          box-sizing: border-box;
        }

        .notification-header {
          flex-shrink: 0;
          text-align: center;
          padding: 20px 16px 12px;
          border-bottom: 2px solid #64748b;
          position: relative;
        }

        .notification-header h1 {
          max-width: 100%;
          overflow-wrap: anywhere;
        }

        .notification-header h1 {
          color: #111827;
          font: 700 25px Arial, sans-serif;
          margin: 0;
        }

        .notification-filters {
          flex-shrink: 0;
          padding: 12px 16px;
          border-bottom: 1px solid #cbd5e1;
        }

        .notification-unread-count {
          font: 600 16px Arial, sans-serif;
          color: #475569;
          margin: 0 0 12px;
        }

        .notification-tabs {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
        }

        .notification-tab {
          border: 1px solid #d1d5db;
          border-radius: 22px;
          padding: 8px 12px;
          background: white;
          color: #374151;
          font: 16px Arial, sans-serif;
          cursor: pointer;
        }

        .notification-tab.active {
          background: #2563eb;
          border-color: #2563eb;
          color: white;
        }

        .notification-list {
          flex: 1;
          min-height: 0;
          overflow-y: auto;
          overscroll-behavior: contain;
          padding: 14px 12px 24px;
          display: flex;
          flex-direction: column;
          gap: 10px;
          -webkit-overflow-scrolling: touch;
        }

        .notification-card {
          flex-shrink: 0;
          min-width: 0;
          border: 1px solid #cbd5e1;
          border-radius: 12px;
          background: white;
          color: #111827;
          padding: 12px;
        }

        /* Ulæste beskeder */
        .notification-card.unread {
          background: #eff6ff;
          border-color: #93c5fd;
          color: #172033;
        }

        .notification-card.unread h2,
        .notification-card.unread .notification-message {
          color: #172033;
        }

        .notification-card-top {
          display: grid;
          grid-template-columns: 28px minmax(0, 1fr) auto 10px;
          align-items: center;
          gap: 7px;
        }

        .notification-icon {
          width: 27px;
          height: 27px;
          display: flex;
          align-items: center;
          justify-content: center;
          border-radius: 50%;
          font: 700 17px Arial, sans-serif;
          flex-shrink: 0;
        }

        /* Gul cirkel: kræver handling/underskrift */
        .notification-icon.icon-required {
          background: #fef3c7;
          color: #b45309;
          border: 1px solid #f59e0b;
        }

        /* Grøn cirkel: systeminformation */
        .notification-icon.icon-system {
          background: #dcfce7;
          color: #15803d;
          border: 1px solid #22c55e;
        }

        /* Neutral cirkel: almindelig information */
        .notification-icon.icon-info {
          background: #e5e7eb;
          color: #6b7280;
          border: 1px solid #9ca3af;
        }

        .notification-category {
          min-width: 0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          font: 12px Arial, sans-serif;
          font-weight: 600;
          color: #64748b;
        }

        .notification-card.needs-action .notification-category {
          color: #b45309;
        }

        .notification-time {
          color: #2563eb;
          font-size: 12px;
          white-space: nowrap;
        }

        /* Blå prik: ulæst */
        .notification-unread-dot {
          width: 10px;
          height: 10px;
          border-radius: 50%;
          background: #2563eb;
          flex-shrink: 0;
        }

        .notification-card h2 {
          font: 700 18px/1.3 Arial, sans-serif;
          margin: 10px 0 5px;
          color: #111827;
          overflow-wrap: anywhere;
        }

        .notification-message {
          margin: 0;
          font-size: 15px;
          line-height: 1.5;
          color: #374151;
          overflow-wrap: anywhere;
        }

        .notification-action {
          display: inline-block;
          margin-top: 10px;
          border: 0;
          padding: 6px 0;
          background: transparent;
          color: #2563eb;
          font: 600 15px Arial, sans-serif;
          cursor: pointer;
          text-align: left;
        }

        .notification-action.primary {
          border-radius: 20px;
          background: #2563eb;
          color: white;
          padding: 9px 14px;
        }

        .notification-action:focus-visible,
        .notification-tab:focus-visible {
          outline: 3px solid #93c5fd;
          outline-offset: 3px;
        }

        .notification-details-overlay {
          position: fixed;
          inset: 0;
          z-index: 1000;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 16px;
          background: rgba(15, 23, 42, 0.6);
        }

        .notification-details {
          position: relative;
          width: 100%;
          max-width: 520px;
          max-height: 85vh;
          max-height: 85dvh;
          overflow-y: auto;
          padding: 24px;
          border-radius: 16px;
          background: white;
          color: #111827;
          font-family: Arial, sans-serif;
        }

        .notification-details h2 {
          margin: 16px 40px 16px 0;
          font-size: 22px;
          line-height: 1.35;
          overflow-wrap: anywhere;
        }

        .notification-details p {
          margin: 10px 0;
          line-height: 1.5;
          overflow-wrap: anywhere;
        }

        .notification-details-close {
          position: absolute;
          top: 10px;
          right: 10px;
          width: 40px;
          height: 40px;
          display: flex;
          align-items: center;
          justify-content: center;
          border: 1px solid #d1d5db;
          border-radius: 50%;
          background: #f3f4f6;
          color: #111827;
          font-size: 27px;
          cursor: pointer;
        }

        @media (min-width: 700px) {
          .notification-page {
            max-width: 760px;
            margin: 0 auto;
            border-inline: 1px solid #cbd5e1;
          }

          .notification-header {
            padding: 24px 20px 16px;
          }

          .notification-filters {
            padding: 14px 20px;
          }

          .notification-list {
            padding: 20px 18px 28px;
            gap: 12px;
          }

          .notification-card {
            padding: 14px 16px;
          }

          .notification-card h2 {
            font-size: 20px;
          }

          .notification-message {
            font-size: 16px;
          }

          .notification-category,
          .notification-time {
            font-size: 13px;
          }
        }

        @media (max-width: 380px) {
          .notification-header {
            padding: 14px 12px 10px;
          }

          .notification-header h1 {
            font-size: 22px;
          }

          .notification-filters {
            padding: 10px 12px;
          }

          .notification-tabs {
            gap: 6px;
          }

          .notification-tab {
            padding: 7px 9px;
            font-size: 14px;
          }

          .notification-list {
            padding: 10px 8px 20px;
          }

          .notification-card {
            padding: 10px;
          }

          .notification-card-top {
            grid-template-columns: 26px minmax(0, 1fr) auto 9px;
            gap: 5px;
          }

          .notification-icon {
            width: 25px;
            height: 25px;
          }

          .notification-category {
            font-size: 10px;
          }

          .notification-time {
            font-size: 10px;
          }

          .notification-card h2 {
            font-size: 16px;
          }

          .notification-message {
            font-size: 14px;
          }
        }
      `}</style>

      <header className="notification-header">
        <h1>Notifikationer</h1>
      </header>

      <section className="notification-filters">
        <p className="notification-unread-count">
          {counts.unread} ulæste beskeder
        </p>

        <NotificationTabs
          activeTab={activeTab}
          counts={counts}
          onTabChange={setActiveTab}
        />
      </section>

      <section className="notification-list" aria-label="Notifikationer">
        {error !== null ? (
          <p>{error}</p>
        ) : isLoading ? (
          <p>Henter notifikationer ...</p>
        ) : visibleNotifications.length === 0 ? (
          <p>Ingen notifikationer i denne kategori.</p>
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
