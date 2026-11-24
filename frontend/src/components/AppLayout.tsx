import { Outlet } from "react-router-dom";
import { BottomNav } from "./ui/bottom-nav";

export function AppLayout() {
  return (
    <div className="flex min-h-svh flex-col bg-[#F8FAFC]">
      <main className="min-h-0 flex-1">
        <Outlet />
      </main>

      <BottomNav />
    </div>
  );
}
