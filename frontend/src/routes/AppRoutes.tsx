import { Route, Routes } from "react-router-dom";
import App from "../App";
import { LoginPage } from "../pages/LoginPage";
import { RestrictionDetailPage } from "../pages/RestrictionDetailPage";
import { ProtectedRoute } from "../hooks/ProtectedRoute";
import { InstallButton } from "../components/InstallButton";
import { CreateRestrictionPage } from "../pages/CreateRestrictionPage";

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
        </Route>
      </Routes>
    </>
  );
}
