
import { useState } from "react";
import {
  NotificationCard,
  type Notification,
} from "../components/notifications/NotificationCard";
import {
  NotificationTabs,
  type NotificationTab,
} from "../components/notifications/NotificationTabs";
import { NotificationDetails } from "../components/notifications/NotificationDetails";

const initialNotifications: Notification[] = [
  {
    id: "OR-001",
    category: "DRIFTSRESTRIKTION",
    title: "Johans: Signatur Påkrævet",
    message: "En ny driftsrestriktion kræver din umiddelbare signatur ...",
    time: "10 min. siden",
    status: "pending",
    unread: true,
    required: true,
    action: "Underskriv nu",
  },
  {
    id: "OR-002",
    category: "DRIFTSRESTRIKTION",
    title: "Anmeldt af DOM: Ændringer Påkrævet",
    message: "Gennemgå venligst de anmodede seneste ændringer ...",
    time: "2 timer siden",
    status: "pending",
    unread: true,
    required: true,
    action: "Se detaljer",
  },
  {
    id: "SYS-001",
    category: "SYSTEM INFO",
    title: "System Opdatering Fuldført",
    message: "Systemet er blevet opdateret og fungerer normalt.",
    time: "I går, 14:30",
    status: "sent",
    unread: false,
    required: false,
  },
  {
    id: "SYS-002",
    category: "SYSTEM INFO",
    title: "Rutinemæssig Vedligeholdelse",
    message: "Information om planlagt vedligeholdelse af systemet ...",
    time: "22. okt.",
    status: "sent",
    unread: true,
    required: false,
  },
  {
    id: "OR-003",
    category: "DRIFTSRESTRIKTION",
    title: "Ny driftsrestriktion",
    message: "En ny driftsrestriktion er blevet oprettet.",
    time: "I går",
    status: "pending",
    unread: true,
    required: true,
    action: "Underskriv nu",
  },
  {
    id: "SYS-003",
    category: "SYSTEM INFO",
    title: "Notifikation sendt",
    message: "Din notifikation er blevet sendt.",
    time: "Mandag",
    status: "failed",
    unread: false,
    required: false,
  },
];

export function NotificationPage() {
  const [notifications, setNotifications] = useState(initialNotifications);
  const [activeTab, setActiveTab] = useState<NotificationTab>("all");
  const [selectedNotification, setSelectedNotification] =
    useState<Notification | null>(null);

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
    const updatedNotification = { ...notification, unread: false };

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
          background: #E0F3FF;
          color: #000000;
          font-family: Inter, sans-serif;
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
          border-bottom: 1px solid #CBD5E1;
        }

        .notification-header h1 {
          max-width: 100%;
          margin: 0;
          color: #000000;
          font: 700 24px/1.3 "Instrument Sans", sans-serif;
          overflow-wrap: anywhere;
        }

        .notification-filters {
          flex-shrink: 0;
          padding: 12px 16px;
          border-bottom: 1px solid #CBD5E1;
        }

        .notification-unread-count {
          margin: 0 0 12px;
          color: #6B6B6B;
          font: 700 16px/1.4 "Instrument Sans", sans-serif;
        }

        .notification-tabs {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
        }

        .notification-tab {
          border: 1px solid #D1D5DB;
          border-radius: 22px;
          padding: 8px 12px;
          background: #FFFFFF;
          color: #374151;
          font: 400 14px/1.4 Inter, sans-serif;
          cursor: pointer;
        }

        .notification-tab.active {
          background: #2563EB;
          border-color: #2563EB;
          color: #FFFFFF;
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
          border: 1px solid #D1D5DB;
          border-radius: 12px;
          background: #EFF6FF;
          color: #000000;
          padding: 12px;
        }

        .notification-card.unread {
          background: #EFF6FF;
          border-color: #93C5FD;
        }

        /* Mørkere kant kun på kort, der kræver handling */
        .notification-card.needs-action {
          border: 1px solid #334155;
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
          font: 700 17px/1 Inter, sans-serif;
          flex-shrink: 0;
        }

        /* Blyant i stedet for orange udråbstegn */
        .notification-card.needs-action .notification-icon.icon-required {
          background: #FFF0DF;
          color: #EF7D00;
          border: 1px solid #EF7D00;
          font-size: 0;
        }

        .notification-card.needs-action .notification-icon.icon-required::after {
          content: "✎";
          font: 700 21px/1 Inter, sans-serif;
          color: #EF7D00;
        }

        .notification-icon.icon-system {
          background: #DCFCE7;
          color: #15803D;
          border: 1px solid #22C55E;
        }

        .notification-icon.icon-info {
          background: #E5E7EB;
          color: #6B7280;
          border: 1px solid #9CA3AF;
        }

        .notification-category {
          min-width: 0;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          font: 400 16px/1.4 Inter, sans-serif;
          color: #6B6B6B;
        }

        .notification-card.needs-action .notification-category {
          color: #EF7D00;
        }

        .notification-time {
          color: #6B6B6B;
          font: 400 14px/1.4 Inter, sans-serif;
          white-space: nowrap;
        }

        .notification-unread-dot {
          width: 10px;
          height: 10px;
          border-radius: 50%;
          background: #2563EB;
          flex-shrink: 0;
        }

        .notification-card h2 {
          margin: 10px 0 5px;
          color: #000000;
          font: 700 22px/1.3 "Inria Serif", serif;
          overflow-wrap: anywhere;
        }

        .notification-card .notification-message {
          margin: 0;
          color: #000000;
          font: 400 20px/1.3 "Inria Serif", serif;
          overflow-wrap: anywhere;
        }

        .notification-action {
          display: inline-block;
          margin-top: 10px;
          border: 0;
          padding: 6px 0;
          background: transparent;
          color: #2563EB;
          font: 600 15px/1.4 Inter, sans-serif;
          cursor: pointer;
          text-align: left;
        }

        .notification-action.primary {
          border-radius: 20px;
          background: #2563EB;
          color: #FFFFFF;
          padding: 9px 14px;
        }

        /* Blyant ved siden af underskriftsknappen */
        .notification-action.primary::before {
          content: "✎";
          display: inline-block;
          margin-right: 7px;
          font-size: 17px;
          line-height: 1;
        }

        .notification-action:focus-visible,
        .notification-tab:focus-visible {
          outline: 3px solid #93C5FD;
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
          background: #FFFFFF;
          color: #000000;
          font-family: Inter, sans-serif;
        }

        .notification-details h2 {
          margin: 16px 40px 16px 0;
          color: #000000;
          font: 700 22px/1.3 "Inria Serif", serif;
          overflow-wrap: anywhere;
        }

        .notification-details p {
          margin: 10px 0;
          line-height: 1.5;
          overflow-wrap: anywhere;
        }

        @media (min-width: 700px) {
          .notification-page {
            max-width: 760px;
            margin: 0 auto;
            border-inline: 1px solid #CBD5E1;
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
            font-size: 13px;
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
            font-size: 11px;
          }

          .notification-time {
            font-size: 11px;
          }

          .notification-card h2 {
            font-size: 18px;
          }

          .notification-card .notification-message {
            font-size: 16px;
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
        {visibleNotifications.length === 0 ? (
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
