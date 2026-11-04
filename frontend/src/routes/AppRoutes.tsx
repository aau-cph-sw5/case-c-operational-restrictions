// ROUTES:
// Defines client-side route configurations using React Router, mapping URLs to page components
import { Route, Routes } from "react-router-dom";
import App from "../App";
import { LoginPage } from "../pages/LoginPage";
import { RestrictionDetailPage } from "../pages/RestrictionDetailPage";
import { ProtectedRoute } from "../hooks/ProtectedRoute";
import { InstallButton } from "../components/InstallButton";
import { OverviewPage } from "../pages/OverviewPage";

export function AppRoutes() {
  return (
    <>
      <InstallButton />
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route path="/home" element={<App />} />
        <Route path="/overview" element={<OverviewPage />} />

      </Routes>
    </>
  );
}
