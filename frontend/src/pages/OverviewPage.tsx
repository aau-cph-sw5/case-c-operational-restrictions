import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { ORNotificationCard } from "@/components/OverviewNotificationCard";
import { getOverviewRestrictions } from "@/services/overviewApi";
import type { DrItem } from "@/types/overviewRestriction";
import { isRestrictionApiError } from "@/types/restrictionApiError";
import { clockTimeNow } from "@/lib/dateFormat";
import { AppHeader } from "../components/AppHeader";
import { getRestrictions } from "@/services/restrictionApi";
import type { RestrictionDto } from "@/types/restrictionDto";

export function OverviewPage() {
  const navigate = useNavigate();
  const [restrictions, setRestrictions] = useState<DrItem[]>([]);
  const [loadError, setLoadError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<
    "kladde" | "alle" | "aktiv" | "arkiv"
  >("alle");
  const [searchQuery, setSearchQuery] = useState("");
  const [restrictions, setRestrictions] = useState<DrItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const [lastUpdated, setLastUpdated] = useState<string>();

  useEffect(() => {
    const controller = new AbortController();

    getOverviewRestrictions(controller.signal)
      .then(setRestrictions)
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        if (isRestrictionApiError(err) && err.status === 401) {
          setError("Du er ikke logget ind.");
        } else {
          setError("Driftsrestriktionerne kunne ikke hentes.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    setLastUpdated(clockTimeNow());
    return () => controller.abort();
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
  const alleCount = restrictions.length - kladdeCount;

  const filteredItems = useMemo(() => {
    return restrictions.filter((item) => {
      const matchesTab =
        (activeTab === "alle" && item.category !== "kladde") ||
        item.category === activeTab;
      const matchesSearch =
        item.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.referenceId.toLowerCase().includes(searchQuery.toLowerCase());
      return matchesTab && matchesSearch;
    });
  }, [restrictions, activeTab, searchQuery]);

  const sortedItems = useMemo(() => {
    return filteredItems.sort((a, b) => {
      if (a.category === "arkiv" && b.category !== "arkiv") return 1;
      else if (b.category === "arkiv" && a.category !== "arkiv") return -1;

      const dateA = new Date(a.date);
      const dateB = new Date(b.date);

      return dateB.getTime() - dateA.getTime();
    });
  }, [filteredItems]);

  return (
    <div className="flex min-h-0 w-full justify-center bg-[#F8FAFC] antialiased text-foreground overscroll-none">
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
          <div className=" text-muted-foreground text-xs text-left">
            Sidst opdateret: {lastUpdated}
          </div>
        </AppHeader>

        {/* Scrollable Card List Container */}
        <main className="min-h-0 flex-1 space-y-3 overflow-y-auto bg-white p-3.5 pb-6">
          {loading ? (
            <p className="text-center py-16 text-muted-foreground">
              Henter driftsrestriktioner...
            </p>
          ) : error ? (
            <p className="text-center py-16 text-destructive font-medium">
              {error}
            </p>
          ) : sortedItems.length > 0 ? (
            sortedItems.map((item) => (
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
