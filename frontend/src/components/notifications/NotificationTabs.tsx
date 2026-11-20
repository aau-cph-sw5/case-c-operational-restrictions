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
    <nav
      className="flex flex-wrap gap-2 max-[380px]:gap-1.5"
      aria-label="Filtrer notifikationer"
    >
      {tabs.map((tab) => {
        const isActive = activeTab === tab.id;

        return (
          <button
            key={tab.id}
            type="button"
            aria-pressed={isActive}
            onClick={() => onTabChange(tab.id)}
            className={`cursor-pointer rounded-full border px-3 py-2 text-base transition-colors focus-visible:outline-[3px] focus-visible:outline-offset-[3px] focus-visible:outline-blue-300 max-[380px]:px-[9px] max-[380px]:py-[7px] max-[380px]:text-sm ${
              isActive
                ? "border-blue-600 bg-blue-600 text-white"
                : "border-gray-300 bg-white text-gray-700 hover:bg-slate-50"
            }`}
          >
            {tab.label} ({counts[tab.id]})
          </button>
        );
      })}
    </nav>
  );
}
