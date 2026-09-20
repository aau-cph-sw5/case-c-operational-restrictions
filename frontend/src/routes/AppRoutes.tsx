// ROUTES:
// Defines client-side route configurations using React Router, mapping URLs to page components
import { Route, Routes } from 'react-router-dom';
import App from '../App';
import { LoginPage } from '../pages/LoginPage';

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<App />} />
      <Route path="/login" element={<LoginPage />} />
    </Routes>
  );
}
