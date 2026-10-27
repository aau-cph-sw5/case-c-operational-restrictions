import { render, screen, waitFor, fireEvent } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RestrictionDetailPage } from "../pages/RestrictionDetailPage";
import type { RestrictionApiError } from "../types/restrictionApiError";
import type { RestrictionDto } from "../types/restrictionDto";

const mocks = vi.hoisted(() => ({
  getRestrictionById: vi.fn(),
  signRestriction: vi.fn(),
  getCurrentUser: vi.fn(),
}));

vi.mock("../services/restrictionApi", () => ({
  getRestrictionById: mocks.getRestrictionById,
  signRestriction: mocks.signRestriction,
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
  restrictionDescription:
    "Speed limit 40 km/h between Vestamager and Lindevang",
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
    },
  ],
};

function apiError(status: number): RestrictionApiError {
  return Object.assign(new Error(`HTTP ${status}`), { status });
}

function renderPage(initialEntry: string | { pathname: string; state?: unknown } = "/restrictions/restriction-1") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path="/restrictions/:id" element={<RestrictionDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

const signButton = () =>
  screen.queryByRole("button", { name: /^underskriv$/i });

describe("RestrictionDetailPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getCurrentUser.mockResolvedValue(originator);
    mocks.getRestrictionById.mockResolvedValue(draft);
  });

  it("fetches only the requested restriction by id", async () => {
    renderPage();
    await screen.findByText("restriction-1");

    expect(mocks.getRestrictionById).toHaveBeenCalledTimes(1);
    expect(mocks.getRestrictionById).toHaveBeenCalledWith("restriction-1");
  });

  it("renders id, state badge, dates in local and UTC, and description", async () => {
    renderPage();

    expect(await screen.findByText("restriction-1")).toBeInTheDocument();
    expect(screen.getByTestId("status-badge")).toHaveTextContent("Kladde");
    expect(screen.getByTestId("start-utc")).toHaveTextContent(
      "2026-11-01 08:00:00 UTC",
    );
    expect(screen.getByTestId("end-utc")).toHaveTextContent(
      "2026-11-02 18:30:00 UTC",
    );
    expect(screen.getByTestId("start-local")).toBeInTheDocument();
    expect(screen.getByTestId("end-local")).toBeInTheDocument();
    expect(screen.getByText(/speed limit 40 km\/h/i)).toBeInTheDocument();
  });

  it("shows the Unsigned Draft badge when there are no signatures", async () => {
    renderPage();

    expect(
      await screen.findByText("Ikke underskrevet kladde"),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("signature-block")).not.toBeInTheDocument();
  });

  it("shows signer, role and UTC timestamp for an existing signature", async () => {
    mocks.getRestrictionById.mockResolvedValue(signed);
    renderPage();

    const block = await screen.findByTestId("signature-block");
    expect(block).toHaveTextContent("Olivia Originator");
    expect(block).toHaveTextContent("Originator");
    expect(block).toHaveTextContent("2026-10-22 09:05:07 UTC");
    expect(
      screen.queryByText("Ikke underskrevet kladde"),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("status-badge")).toHaveTextContent(
      "Afventer godkendelse",
    );
  });

  it("shows the sign button for the originator of a draft", async () => {
    renderPage();
    await screen.findByText("restriction-1");

    expect(signButton()).toBeInTheDocument();
  });

  it("shows a temporary, disabled edit button next to sign for the originator", async () => {
    renderPage();
    await screen.findByText("restriction-1");

    expect(screen.getByRole("button", { name: /rediger/i })).toBeDisabled();
  });

  it("hides the sign button for a user who is not the originator", async () => {
    mocks.getCurrentUser.mockResolvedValue({
      ...originator,
      id: "someone-else",
    });
    renderPage();
    await screen.findByText("restriction-1");

    expect(signButton()).not.toBeInTheDocument();
  });

  it("hides the sign button when the restriction is already submitted", async () => {
    mocks.getRestrictionById.mockResolvedValue(signed);
    renderPage();
    await screen.findByTestId("signature-block");

    expect(signButton()).not.toBeInTheDocument();
  });

  it("signs, shows a spinner while pending, then updates state and signature", async () => {
    let resolveSign: (value: RestrictionDto) => void = () => {};
    mocks.signRestriction.mockReturnValue(
      new Promise<RestrictionDto>((resolve) => {
        resolveSign = resolve;
      }),
    );
    renderPage();
    await screen.findByText("restriction-1");

    fireEvent.click(signButton()!);

    const pending = await screen.findByRole("button", {
      name: /underskriver/i,
    });
    expect(pending).toBeDisabled();
    expect(mocks.signRestriction).toHaveBeenCalledWith("restriction-1");

    resolveSign(signed);

    await waitFor(() =>
      expect(screen.getByTestId("status-badge")).toHaveTextContent(
        "Afventer godkendelse",
      ),
    );
    const block = screen.getByTestId("signature-block");
    expect(block).toHaveTextContent("Olivia Originator");
    expect(block).toHaveTextContent("2026-10-22 09:05:07 UTC");
    expect(signButton()).not.toBeInTheDocument();
  });

  it("re-fetches the restriction when the sign response has no body", async () => {
    mocks.signRestriction.mockResolvedValue(null);
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
    expect(screen.getByTestId("signature-block")).toBeInTheDocument();
  });

  it.each([
    [400, /ikke komplet/i],
    [403, /kun restriktionens originator/i],
    [409, /ikke længere en kladde/i],
  ])(
    "shows an alert and stays Draft when signing returns %i",
    async (status, message) => {
      mocks.signRestriction.mockRejectedValue(apiError(status));
      renderPage();
      await screen.findByText("restriction-1");

      fireEvent.click(signButton()!);

      expect(await screen.findByRole("alert")).toHaveTextContent(message);
      expect(screen.getByTestId("status-badge")).toHaveTextContent("Kladde");
      expect(screen.getByText("Ikke underskrevet kladde")).toBeInTheDocument();
      expect(signButton()).toBeEnabled();
    },
  );

  it("shows a not found message on 404", async () => {
    mocks.getRestrictionById.mockRejectedValue(apiError(404));
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(/ikke fundet/i);
  });

  it("shows a permission message on 403", async () => {
    mocks.getRestrictionById.mockRejectedValue(apiError(403));
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(/ikke adgang/i);
  });

  it("handles nullable end date without showing Invalid date", async () => {
    mocks.getRestrictionById.mockResolvedValue({
      ...draft,
      endDate: null,
    });
    renderPage();
    await screen.findByText("restriction-1");

    expect(screen.getByTestId("end-local")).toHaveTextContent("–");
    expect(screen.queryByTestId("end-utc")).not.toBeInTheDocument();
  });

  it("displays a navigation notification when redirected with state", async () => {
    renderPage({
      pathname: "/restrictions/restriction-1",
      state: { notification: "Driftsrestriktion oprettet som kladde." },
    });
    await screen.findByText("restriction-1");

    expect(
      screen.getByTestId("navigation-notification"),
    ).toHaveTextContent("Driftsrestriktion oprettet som kladde.");
  });
});
