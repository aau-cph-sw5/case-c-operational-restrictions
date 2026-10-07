import { useState } from "react";
import { usePwaInstall } from "../hooks/usePwaInstall";

export function InstallButton() {
  const { isInstallable, isIos, canPromptDirectly, promptInstall } =
    usePwaInstall();
  const [showInstructions, setShowInstructions] = useState(false);

  if (!isInstallable) {
    return null;
  }

  const handleClick = async () => {
    if (canPromptDirectly) {
      await promptInstall();
    } else {
      setShowInstructions((prev) => !prev);
    }
  };

  return (
    <div
      style={{
        position: "fixed",
        top: "1rem",
        right: "1rem",
        zIndex: 1000,
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-end",
      }}
    >
      <button
        type="button"
        onClick={handleClick}
        style={{
          display: "flex",
          alignItems: "center",
          gap: "0.5rem",
          backgroundColor: "#863bff",
          color: "#ffffff",
          border: "none",
          borderRadius: "8px",
          padding: "0.6rem 1rem",
          fontSize: "0.9rem",
          fontWeight: 600,
          cursor: "pointer",
          boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
        }}
        aria-label="Install app"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="7 10 12 15 17 10" />
          <line x1="12" y1="15" x2="12" y2="3" />
        </svg>
        Install App
      </button>

      {showInstructions && (
        <div
          role="dialog"
          aria-label="Install instructions"
          style={{
            marginTop: "0.5rem",
            backgroundColor: "#ffffff",
            color: "#1f2937",
            padding: "1rem",
            borderRadius: "8px",
            border: "1px solid #e5e7eb",
            boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
            maxWidth: "300px",
            fontSize: "0.85rem",
            lineHeight: 1.4,
            textAlign: "left",
          }}
        >
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              marginBottom: "0.5rem",
            }}
          >
            <strong style={{ color: "#863bff" }}>
              {isIos ? "Install on iOS" : "Install App"}
            </strong>
            <button
              type="button"
              onClick={() => setShowInstructions(false)}
              style={{
                background: "transparent",
                border: "none",
                fontSize: "1rem",
                cursor: "pointer",
                padding: "0 0.25rem",
                color: "#6b7280",
              }}
              aria-label="Close"
            >
              ×
            </button>
          </div>
          {isIos ? (
            <ol style={{ paddingLeft: "1.2rem", margin: 0 }}>
              <li>
                Tap the <strong>Share</strong> button (
                <span aria-hidden="true">⎋</span>) in Safari's toolbar.
              </li>
              <li style={{ marginTop: "0.25rem" }}>
                Scroll down and tap <strong>Add to Home Screen</strong>.
              </li>
            </ol>
          ) : (
            <ul style={{ paddingLeft: "1.2rem", margin: 0 }}>
              <li>
                <strong>Chrome / Edge (Desktop):</strong> Click the install icon
                (⊕) in your browser address bar, or use the menu (⋮) &rarr;
                "Install Metro Operational Restrictions".
              </li>
              <li style={{ marginTop: "0.5rem" }}>
                <strong>Mobile:</strong> Open in Chrome (Android) or Safari
                (iOS) to install directly to your device home screen.
              </li>
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
