export type NotificationTab = "all" | "unread" | "required";

interface NotificationTabsProps {
  activeTab: NotificationTab;
  counts: {
    all: number;
    unread: number;
    required: number;
  };
  onTabChange: (tab: NotificationTab) => void;
}

const tabs: { id: NotificationTab; label: string }[] = [
  { id: "all", label: "Alle" },
  { id: "unread", label: "Ulæste" },
  { id: "required", label: "Påkrævet" },
];

export function NotificationTabs({
  activeTab,
  counts,
  onTabChange,
}: NotificationTabsProps) {
  return (
    <nav className="notification-tabs" aria-label="Filtrer notifikationer">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          type="button"
          className={`notification-tab ${activeTab === tab.id ? "active" : ""}`}
          aria-pressed={activeTab === tab.id}
          onClick={() => onTabChange(tab.id)}
        >
          {tab.label} ({counts[tab.id]})
        </button>
      ))}
    </nav>
  );
}
