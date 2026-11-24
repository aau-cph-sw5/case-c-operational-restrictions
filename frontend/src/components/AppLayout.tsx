import { Outlet } from "react-router-dom";
import { RequiredSignaturesDialog } from "./RequiredSignatures/RequiredSignaturesDialog";
import { BottomNav } from "./ui/bottom-nav";

export function AppLayout() {
  return (
    <div className="flex min-h-svh flex-col bg-[#F8FAFC]">
      <main className="min-h-0 flex-1">
        <Outlet />
      </main>

      <BottomNav />

      {/* AppLayout only renders inside ProtectedRoute, i.e. after the JWT has been validated. */}
      <RequiredSignaturesDialog />
    </div>
  );
}
