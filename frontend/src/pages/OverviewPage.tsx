import { useState, useMemo } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { ORNotificationCard,type DRStatus } from "@/components/OverviewNotificationCard";
import { BottomNav } from "@/components/ui/bottom-nav";

export interface DrItem {
  id: string;
  referenceId: string;
  statusKey: DRStatus;
  title: string;
  category: "kladde" | "alle" | "aktiv" | "arkiv";
}

const MOCK_DATA: DrItem[] = [
  // Kladde items
  {
    id: "k1",
    referenceId: "01012026-0101",
    statusKey: "kladde",
    title: "Vandskade ved Sluseholmen medfører afbrudte døre",
    category: "kladde",
  },
  {
    id: "k2",
    referenceId: "01012026-0102",
    statusKey: "kladde",
    title: "Nørrebro lukket pga demonstrationer",
    category: "kladde",
  },

  // Aktiv items
  {
    id: "a1",
    referenceId: "01012026-0001",
    statusKey: "aktiv",
    title: "Vandskade ved Sluseholmen medfører afbrudte døre",
    category: "aktiv",
  },
  {
    id: "a2",
    referenceId: "12052026-0003",
    statusKey: "aktiv",
    title: "Sænket hastighed mellem KBH H og Rådhuspladsen",
    category: "aktiv",
  },
  {
    id: "a3",
    referenceId: "15052026-0004",
    statusKey: "aktiv",
    title: "Færre toge på M3-linjen",
    category: "aktiv",
  },
  {
    id: "a4",
    referenceId: "01012026-0002",
    statusKey: "aktiv",
    title: "Nørrebro lukket pga demonstrationer",
    category: "aktiv",
  },

  // Afventer deaktivering items (under Aktiv tab)
  {
    id: "ad1",
    referenceId: "14032024-00123",
    statusKey: "afventer_deaktivering",
    title: "Strømafbrydelse ved Østerport st.",
    category: "aktiv",
  },
  {
    id: "ad2",
    referenceId: "03012026-0005",
    statusKey: "afventer_deaktivering",
    title: "Østerport lukket pga demonstrationer",
    category: "aktiv",
  },

  // Arkiv items
  {
    id: "ar1",
    referenceId: "03012023-1001",
    statusKey: "arkiv",
    title: "Motor på 3 toge er brændt sammen",
    category: "arkiv",
  },
  {
    id: "ar2",
    referenceId: "03012022-1002",
    statusKey: "arkiv",
    title: "M3-linjen holder sommerferie",
    category: "arkiv",
  },
  {
    id: "ar3",
    referenceId: "03012021-1003",
    statusKey: "arkiv",
    title: "Ingen toge sikre; elektriker er på strejke",
    category: "arkiv",
  },
  {
    id: "ar4",
    referenceId: "03012023-1004",
    statusKey: "arkiv",
    title: "Hul i skinnerne ved Mozarts plads",
    category: "arkiv",
  },
  {
    id: "ar5",
    referenceId: "03012024-1005",
    statusKey: "arkiv",
    title: "Graffiti-maler tilskadekommet ved tog",
    category: "arkiv",
  },
  {
    id: "ar6",
    referenceId: "03012024-1006",
    statusKey: "arkiv",
    title: "Toge blev solgt på auktion, vi mangler nye",
    category: "arkiv",
  },
];

export function OverviewPage() {
  const [activeTab, setActiveTab] = useState<"kladde" | "alle" | "aktiv" | "arkiv">("alle");
  const [searchQuery, setSearchQuery] = useState("");

  const kladdeCount = MOCK_DATA.filter((item) => item.category === "kladde").length;
  const aktivCount = MOCK_DATA.filter((item) => item.category === "aktiv").length;
  const arkivCount = MOCK_DATA.filter((item) => item.category === "arkiv").length;
  const alleCount = MOCK_DATA.length;

  const filteredItems = useMemo(() => {
    return MOCK_DATA.filter((item) => {
      const matchesTab = activeTab === "alle" || item.category === activeTab;
      const matchesSearch =
        item.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.referenceId.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesTab && matchesSearch;
    });
  }, [activeTab, searchQuery]);

  return (
    <div className="h-screen w-screen bg-[#F8FAFC] flex justify-center items-center overflow-hidden antialiased text-foreground">
      {/* Permanent Fixed Mobile Shell Container */}
      <div className="w-full max-w-md h-full bg-white flex flex-col shadow-xl relative border-x border-border/40 overflow-hidden">
        
        {/* Header*/}
        <header className="shrink-0 bg-[#E0F3FF] pt-5 pb-4 px-4 flex flex-col gap-4 border-b border-border/50 z-30">
          <h1 className="text-xl md:text-2xl font-bold text-center text-foreground font-heading">
            Alle Driftsrestriktioner
          </h1>

          {/* Subheader Filter Tabs*/}
          <div className="grid grid-cols-4 gap-1 p-1 bg-white/60 backdrop-blur-sm rounded-full border border-border/40">
            <button
              onClick={() => setActiveTab("kladde")}
              className={cn(
                "py-2 px-1 text-xs md:text-sm font-semibold rounded-full transition-all text-center truncate cursor-pointer",
                activeTab === "kladde"
                  ? "bg-[#2563EB] text-white shadow-sm"
                  : "text-foreground hover:bg-black/5"
              )}
            >
              Kladde ({kladdeCount})
            </button>
            <button
              onClick={() => setActiveTab("alle")}
              className={cn(
                "py-2 px-1 text-xs md:text-sm font-semibold rounded-full transition-all text-center truncate cursor-pointer",
                activeTab === "alle"
                  ? "bg-[#2563EB] text-white shadow-sm"
                  : "text-foreground hover:bg-black/5"
              )}
            >
              Alle ({alleCount})
            </button>
            <button
              onClick={() => setActiveTab("aktiv")}
              className={cn(
                "py-2 px-1 text-xs md:text-sm font-semibold rounded-full transition-all text-center truncate cursor-pointer",
                activeTab === "aktiv"
                  ? "bg-[#2563EB] text-white shadow-sm"
                  : "text-foreground hover:bg-black/5"
              )}
            >
              Aktiv ({aktivCount})
            </button>
            <button
              onClick={() => setActiveTab("arkiv")}
              className={cn(
                "py-2 px-1 text-xs md:text-sm font-semibold rounded-full transition-all text-center truncate cursor-pointer",
                activeTab === "arkiv"
                  ? "bg-[#2563EB] text-white shadow-sm"
                  : "text-foreground hover:bg-black/5"
              )}
            >
              Arkiv ({arkivCount})
            </button>
          </div>

          {/* Search Bar */}
          <div className="relative w-full">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" />
            <Input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tryk her for at søge"
              className="pl-9 bg-white border-border/80 h-11 text-base rounded-full shadow-xs focus-visible:ring-2 focus-visible:ring-primary"
            />
          </div>
        </header>

        {/* Scrollable Card List Container */}
        <main className="flex-1 overflow-y-auto p-3.5 space-y-3 bg-white pb-20">
          {filteredItems.length > 0 ? (
            filteredItems.map((item) => (
              <ORNotificationCard
                key={item.id}
                referenceId={item.referenceId}
                statusKey={item.statusKey}
                title={item.title}
                onSeMere={() => {
                  console.log(`Action clicked for ${item.referenceId}`);
                }}
              />
            ))
          ) : (
            <div className="text-center py-16 text-muted-foreground">
              <p className="text-base font-medium">Ingen driftsrestriktioner fundet</p>
              <p className="text-sm mt-1">Prøv at søge efter noget andet.</p>
            </div>
          )}
        </main>

        {/* Embedded Bottom Navigation  */}
        <BottomNav />
      </div>
    </div>
  );
}