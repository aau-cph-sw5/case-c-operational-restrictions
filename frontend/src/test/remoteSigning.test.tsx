// MET-C-010: signing from any device, with a clear result when the connection drops.
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RestrictionDetailPage } from "../pages/RestrictionDetailPage";
import type { RestrictionDto } from "../types/restrictionDto";

const mocks = vi.hoisted(() => ({
  getRestrictionById: vi.fn(),
  signRestriction: vi.fn(),
  signForImplementation: vi.fn(),
  getSigningAttemptStatus: vi.fn(),
  requestChanges: vi.fn(),
  getCurrentUser: vi.fn(),
}));

vi.mock("../services/restrictionApi", () => ({
  getRestrictionById: mocks.getRestrictionById,
  signRestriction: mocks.signRestriction,
  signForImplementation: mocks.signForImplementation,
  getSigningAttemptStatus: mocks.getSigningAttemptStatus,
  requestChanges: mocks.requestChanges,
}));

vi.mock("../services/authService", () => ({
  authService: { getCurrentUser: mocks.getCurrentUser },
}));

const originator = {
  id: "user-1",
  name: "Olivia Originator",
  email: "originator@example.test",
};

const draft: RestrictionDto = {
  id: "restriction-1",
  originatorUserId: "user-1",
  status: "Draft",
  startDate: "2026-11-01T08:00:00Z",
  endDate: "2026-11-02T18:30:00Z",
  restrictionDescription: "Speed limit 40 km/h",
  signatures: [],
};

const signed: RestrictionDto = {
  ...draft,
  status: "PendingApproval",
  signatures: [
    {
      signeeId: "user-1",
      signeeRole: "Originator",
      signedAt: "2026-10-22T09:05:07Z",
      deviceClass: "Phone",
    },
  ],
};

// fetch rejects with a TypeError when the connection drops: no HTTP status.
const connectionDropped = () => new TypeError("Failed to fetch");

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/restrictions/restriction-1"]}>
      <Routes>
        <Route path="/restrictions/:id" element={<RestrictionDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

const signButton = () =>
  screen.queryByRole("button", { name: /^underskriv$/i });

describe("remote signing", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getCurrentUser.mockResolvedValue(originator);
    mocks.getRestrictionById.mockResolvedValue(draft);
  });

  it("shows the device class recorded with a signature", async () => {
    mocks.getRestrictionById.mockResolvedValue(signed);
    renderPage();

    expect(await screen.findByTestId("signature-device")).toHaveTextContent(
      "Telefon",
    );
  });

  it("does not show a device line when the device class is unknown", async () => {
    mocks.getRestrictionById.mockResolvedValue({
      ...signed,
      signatures: [{ ...signed.signatures[0], deviceClass: "Unknown" }],
    });
    renderPage();
    await screen.findByTestId("signature-block");

    expect(screen.queryByTestId("signature-device")).not.toBeInTheDocument();
  });

  it("does not claim success until the server has confirmed the signature", async () => {
    let resolveSign: (value: RestrictionDto) => void = () => {};
    mocks.signRestriction.mockReturnValue(
      new Promise<RestrictionDto>((resolve) => {
        resolveSign = resolve;
      }),
    );
    renderPage();
    await screen.findByText("restriction-1");

    fireEvent.click(signButton()!);

    expect(
      await screen.findByRole("button", { name: /underskriver/i }),
    ).toBeDisabled();
    expect(screen.queryByTestId("signature-block")).not.toBeInTheDocument();

    resolveSign(signed);
    await screen.findByTestId("signature-block");
  });

  it("sends one attempt key per attempt and a new one for the next attempt", async () => {
    mocks.signRestriction.mockRejectedValue(
      Object.assign(new Error("HTTP 409"), { status: 409 }),
    );
    renderPage();
    await screen.findByText("restriction-1");

    fireEvent.click(signButton()!);
    await screen.findByRole("alert");
    fireEvent.click(signButton()!);
    await waitFor(() => expect(mocks.signRestriction).toHaveBeenCalledTimes(2));

    const [, firstKey] = mocks.signRestriction.mock.calls[0];
    const [, secondKey] = mocks.signRestriction.mock.calls[1];
    expect(firstKey).toMatch(/^[0-9a-f-]{36}$/);
    // The server answered with a refusal, so that attempt is over.
    expect(secondKey).not.toBe(firstKey);
  });

  describe("when the connection drops during signing", () => {
    it("shows the signature when the server says the attempt was committed", async () => {
      mocks.signRestriction.mockRejectedValue(connectionDropped());
      mocks.getSigningAttemptStatus.mockResolvedValue({
        status: "Signed",
        signature: signed.signatures[0],
      });
      mocks.getRestrictionById
        .mockResolvedValueOnce(draft)
        .mockResolvedValueOnce(signed);
      renderPage();
      await screen.findByText("restriction-1");

      fireEvent.click(signButton()!);

      await waitFor(() =>
        expect(screen.getByTestId("status-badge")).toHaveTextContent(
          "Afventer godkendelse",
        ),
      );
      const [, attemptKey] = mocks.signRestriction.mock.calls[0];
      expect(mocks.getSigningAttemptStatus).toHaveBeenCalledWith(attemptKey);
      expect(screen.getByTestId("signature-block")).toBeInTheDocument();
    });

    it("says it was not signed and allows a retry with the same key", async () => {
      mocks.signRestriction.mockRejectedValueOnce(connectionDropped());
      mocks.getSigningAttemptStatus.mockResolvedValue({ status: "NotSigned" });
      renderPage();
      await screen.findByText("restriction-1");

      fireEvent.click(signButton()!);

      expect(await screen.findByRole("alert")).toHaveTextContent(
        /ikke registreret/i,
      );
      expect(screen.getByTestId("status-badge")).toHaveTextContent("Kladde");
      expect(signButton()).toBeEnabled();

      mocks.signRestriction.mockResolvedValue(signed);
      fireEvent.click(signButton()!);
      await screen.findByTestId("signature-block");

      const [, firstKey] = mocks.signRestriction.mock.calls[0];
      const [, retryKey] = mocks.signRestriction.mock.calls[1];
      expect(retryKey).toBe(firstKey);
    });

    it("offers no sign button while the outcome is unknown, then resolves it", async () => {
      mocks.signRestriction.mockRejectedValue(connectionDropped());
      mocks.getSigningAttemptStatus.mockRejectedValue(connectionDropped());
      renderPage();
      await screen.findByText("restriction-1");

      fireEvent.click(signButton()!);

      expect(
        await screen.findByTestId("signing-outcome-unknown"),
      ).toHaveTextContent(/kunne ikke bekræfte/i);
      expect(signButton()).not.toBeInTheDocument();
      expect(screen.getByTestId("status-badge")).toHaveTextContent("Kladde");

      mocks.getSigningAttemptStatus.mockResolvedValue({
        status: "Signed",
        signature: signed.signatures[0],
      });
      mocks.getRestrictionById.mockResolvedValue(signed);
      fireEvent.click(screen.getByRole("button", { name: /tjek igen/i }));

      await screen.findByTestId("signature-block");
      expect(
        screen.queryByTestId("signing-outcome-unknown"),
      ).not.toBeInTheDocument();
    });

    it("treats a server error like a dropped connection and asks the server", async () => {
      mocks.signRestriction.mockRejectedValue(
        Object.assign(new Error("HTTP 500"), { status: 500 }),
      );
      mocks.getSigningAttemptStatus.mockResolvedValue({ status: "NotSigned" });
      renderPage();
      await screen.findByText("restriction-1");

      fireEvent.click(signButton()!);

      await waitFor(() =>
        expect(mocks.getSigningAttemptStatus).toHaveBeenCalledTimes(1),
      );
      expect(await screen.findByRole("alert")).toHaveTextContent(
        /ikke registreret/i,
      );
    });
  });
});
