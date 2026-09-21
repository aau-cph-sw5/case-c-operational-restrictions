// ROUTES:
// Defines client-side route configurations using React Router, mapping URLs to page components
import { Route, Routes } from 'react-router-dom';
import App from '../App';
import { LoginPage } from '../pages/LoginPage';
import { ProtectedRoute } from './ProtectedRoute';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<LoginPage />} />
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/home" element={<App />} />
      </Route>
    </Routes>
  );
}
