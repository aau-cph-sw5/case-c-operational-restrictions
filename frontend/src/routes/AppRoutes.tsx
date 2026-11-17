import { Route, Routes } from "react-router-dom";
import App from "../App";
import { LoginPage } from "../pages/LoginPage";
import { RestrictionDetailPage } from "../pages/RestrictionDetailPage";
import { ProtectedRoute } from "../hooks/ProtectedRoute";
import { InstallButton } from "../components/InstallButton";
import { CreateRestrictionPage } from "../pages/CreateRestrictionPage";
import { OverviewPage } from "../pages/OverviewPage";

export function AppRoutes() {
  return (
    <>
      <InstallButton />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<LoginPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path="/home" element={<App />} />
          <Route path="/restrictions/new" element={<CreateRestrictionPage />} />
          <Route path="/restrictions/:id" element={<RestrictionDetailPage />} />
          <Route path="*" element={<App />} />
          <Route path="/overview" element={<OverviewPage />} />
        </Route>
      </Routes>
    </>
  );
}
