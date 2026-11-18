import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { registerSW } from "virtual:pwa-register";
import "./index.css";
import { AppRoutes } from "./routes/AppRoutes.tsx";
import "@fontsource/instrument-sans/400.css";
import "@fontsource/instrument-sans/700.css";
import "@fontsource/inria-serif/400.css";
import "@fontsource/inria-serif/700.css";

registerSW({ immediate: true });

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <AppRoutes />
    </BrowserRouter>
  </StrictMode>,
);
