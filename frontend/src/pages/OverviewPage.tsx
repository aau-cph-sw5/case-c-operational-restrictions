import { useState, useMemo, useEffect } from "react";
import { Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import {
  ORNotificationCard,
  type DRStatus,
} from "@/components/OverviewNotificationCard";
import { AppHeader } from "../components/AppHeader";
import { getRestrictions } from "@/services/restrictionApi";
import type { RestrictionDto } from "@/types/restrictionDto";

export interface DrItem {
  id: string;
  referenceId: string;
  statusKey: DRStatus;
  title: string;
  date: string;
  signed: boolean;
  category: "kladde" | "alle" | "aktiv" | "arkiv";
}

function toOverviewItem(restriction: RestrictionDto): DrItem {
  const category: DrItem["category"] =
    restriction.status === "Draft"
      ? "kladde"
      : restriction.status === "Active" ||
          restriction.status === "PendingControlRoomSupervisorApproval"
        ? "aktiv"
        : restriction.status === "Archived"
          ? "arkiv"
          : "alle";

  const statusKey: DRStatus =
    restriction.status === "PendingControlRoomSupervisorApproval"
      ? "afventer_implementering"
      : category === "kladde"
        ? "kladde"
        : category === "arkiv"
          ? "arkiv"
          : restriction.status === "Active"
            ? "aktiv"
            : "afventer_deaktivering";

  return {
    id: restriction.id,
    referenceId: restriction.reference ?? restriction.id,
    statusKey,
    title: restriction.title ?? restriction.id,
    date: restriction.startDate,
    signed: restriction.signatures.length > 0,
    category,
  };
}

export function OverviewPage() {
  const navigate = useNavigate();
  const [restrictions, setRestrictions] = useState<DrItem[]>([]);
  const [loadError, setLoadError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<
    "kladde" | "alle" | "aktiv" | "arkiv"
  >("alle");
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    getRestrictions()
      .then((loadedRestrictions) => {
        setRestrictions(loadedRestrictions.map(toOverviewItem));
      })
      .catch(() => {
        setLoadError(true);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  const kladdeCount = restrictions.filter(
    (item) => item.category === "kladde",
  ).length;
  const aktivCount = restrictions.filter(
    (item) => item.category === "aktiv",
  ).length;
  const arkivCount = restrictions.filter(
    (item) => item.category === "arkiv",
  ).length;
  const alleCount = restrictions.length;

  const filteredItems = useMemo(() => {
    return restrictions.filter((item) => {
      const matchesTab = activeTab === "alle" || item.category === activeTab;
      const matchesSearch =
        item.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.referenceId.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesTab && matchesSearch;
    });
  }, [activeTab, searchQuery, restrictions]);

  return (
    <div className="flex min-h-0 w-full justify-center bg-[#F8FAFC] antialiased text-foreground">
      <div className="flex h-[calc(100svh-5rem)] w-full max-w-md flex-col overflow-hidden border-x border-border/40 bg-white shadow-xl">
        {/* Header*/}
        <AppHeader title="Alle Driftsrestriktioner">
          {/* Subheader Filter Tabs*/}
          <div className="grid grid-cols-4 gap-1 p-1 bg-white/60 backdrop-blur-sm rounded-full border border-border/40">
            <button
              onClick={() => setActiveTab("kladde")}
              className={cn(
                "py-2 px-1 text-xs md:text-sm font-semibold rounded-full transition-all text-center truncate cursor-pointer",
                activeTab === "kladde"
                  ? "bg-[#2563EB] text-white shadow-sm"
                  : "text-foreground hover:bg-black/5",
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
                  : "text-foreground hover:bg-black/5",
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
                  : "text-foreground hover:bg-black/5",
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
                  : "text-foreground hover:bg-black/5",
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
        </AppHeader>

        {/* Scrollable Card List Container */}
        <main className="min-h-0 flex-1 space-y-3 overflow-y-auto bg-white p-3.5 pb-6">
          {loading ? (
            <p
              role="status"
              className="py-16 text-center text-muted-foreground"
            >
              Henter driftsrestriktioner...
            </p>
          ) : loadError ? (
            <p role="alert" className="py-16 text-center text-destructive">
              Kunne ikke hente driftsrestriktioner. Prøv igen.
            </p>
          ) : filteredItems.length > 0 ? (
            filteredItems.map((item) => (
              <ORNotificationCard
                key={item.id}
                statusKey={item.statusKey}
                title={item.title}
                date={item.date}
                signed={item.signed}
                onSeMere={() => navigate(`/restrictions/${item.id}`)}
              />
            ))
          ) : (
            <div className="text-center py-16 text-muted-foreground">
              <p className="text-base font-medium">
                Ingen driftsrestriktioner fundet
              </p>
              <p className="text-sm mt-1">Prøv at søge efter noget andet.</p>
            </div>
          )}
        </main>

        {/* Embedded Bottom Navigation  */}
      </div>
    </div>
  );
}
