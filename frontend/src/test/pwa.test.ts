import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

describe("PWA Configuration and Assets", () => {
  const currentDir = path.dirname(fileURLToPath(import.meta.url));
  const publicDir = path.resolve(currentDir, "../../public");
  const indexHtmlPath = path.resolve(currentDir, "../../index.html");

  it("should have all required PWA icons in the public directory", () => {
    const requiredIcons = [
      "pwa-192x192.png",
      "pwa-512x512.png",
      "pwa-maskable-512x512.png",
      "apple-touch-icon.png",
      "favicon.svg",
    ];

    for (const icon of requiredIcons) {
      const fullPath = path.join(publicDir, icon);
      expect(
        fs.existsSync(fullPath),
        `Expected ${icon} to exist in public directory`,
      ).toBe(true);

      const stats = fs.statSync(fullPath);
      expect(stats.size).toBeGreaterThan(0);
    }
  });

  it("should have PWA and iOS meta tags configured in index.html", () => {
    const htmlContent = fs.readFileSync(indexHtmlPath, "utf-8");

    expect(htmlContent).toContain(
      '<meta name="theme-color" content="#863bff" />',
    );
    expect(htmlContent).toContain(
      '<meta name="apple-mobile-web-app-capable" content="yes" />',
    );
    expect(htmlContent).toContain(
      '<meta name="apple-mobile-web-app-title" content="Driftsrestriktioner" />',
    );
    expect(htmlContent).toContain(
      '<link rel="apple-touch-icon" href="/apple-touch-icon.png" />',
    );
  });
});
