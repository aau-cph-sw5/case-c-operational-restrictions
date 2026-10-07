import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { InstallButton } from "../components/InstallButton";

describe("PWA Install Functionality by Platform", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("Android/Chromium: calls native prompt on click when install prompt is available", async () => {
    const promptMock = vi.fn().mockResolvedValue(undefined);
    render(<InstallButton />);

    const event = new Event("beforeinstallprompt");
    Object.assign(event, {
      prompt: promptMock,
      userChoice: Promise.resolve({ outcome: "accepted" }),
    });

    await act(async () => {
      fireEvent(window, event);
    });

    await act(async () => {
      fireEvent.click(screen.getByRole("button", { name: /install app/i }));
    });

    expect(promptMock).toHaveBeenCalledOnce();
  });

  it("iOS: shows Safari Share instructions when clicked on iPhone/iPad", () => {
    vi.spyOn(window.navigator, "userAgent", "get").mockReturnValue(
      "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1",
    );
    render(<InstallButton />);

    fireEvent.click(screen.getByRole("button", { name: /install app/i }));

    expect(screen.getByText(/install on ios/i)).toBeInTheDocument();
    expect(screen.getByText(/add to home screen/i)).toBeInTheDocument();
  });

  it("Desktop: shows address-bar guidance when clicked without native prompt", () => {
    render(<InstallButton />);

    fireEvent.click(screen.getByRole("button", { name: /install app/i }));

    expect(screen.getByText(/chrome \/ edge \(desktop\)/i)).toBeInTheDocument();
  });

  it("Standalone: hides the button when already installed", () => {
    vi.spyOn(window, "matchMedia").mockReturnValue({
      matches: true,
      media: "(display-mode: standalone)",
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    });

    const { container } = render(<InstallButton />);
    expect(container.firstChild).toBeNull();
  });
});
