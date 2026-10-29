import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CreateRestrictionPage } from "../pages/CreateRestrictionPage";
import {
  RestrictionValidationError,
  createRestriction,
} from "../services/restrictionApi";

vi.mock("../services/restrictionApi", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../services/restrictionApi")>();
  return { ...actual, createRestriction: vi.fn() };
});

const createMock = vi.mocked(createRestriction);

const created = {
  id: "abc-123",
  startDate: "2026-11-01",
  endDate: "2026-11-02",
  situationDescription: "Situationen",
  restrictionDescription: "Restriktionen",
  cancellationDescription: "Ophævelsen",
};

function DetailStub() {
  const location = useLocation();
  const note = (location.state as { notification?: string } | null)
    ?.notification;
  return (
    <div>
      Detaljeside
      {note && <p>{note}</p>}
    </div>
  );
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/restrictions/new"]}>
      <Routes>
        <Route path="/restrictions/new" element={<CreateRestrictionPage />} />
        <Route path="/restrictions/:id" element={<DetailStub />} />
      </Routes>
    </MemoryRouter>,
  );
}

function type(label: RegExp, value: string) {
  fireEvent.change(screen.getByLabelText(label), { target: { value } });
}

function click(name: RegExp) {
  fireEvent.click(screen.getByRole("button", { name }));
}

function fillToLastStep({ withEndDate = true } = {}) {
  type(/^ikrafttræden/i, "2026-11-01");
  if (withEndDate) type(/^annulleres/i, "2026-11-02");
  click(/næste/i);
  type(/^om situationen/i, "Situationen");
  type(/^om driftsrestriktionen/i, "Restriktionen");
  click(/næste/i);
  type(/^om ophævelse/i, "Ophævelsen");
}

describe("CreateRestrictionPage", () => {
  beforeEach(() => {
    localStorage.setItem("accessToken", "test-token");
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
    localStorage.clear();
  });

  it("updates inputs as the user types", () => {
    renderPage();
    type(/^reference/i, "Metro 101 - 2026 version");
    expect(screen.getByLabelText(/^reference/i)).toHaveValue(
      "Metro 101 - 2026 version",
    );
  });

  it("blocks Næste while ikrafttræden is empty", () => {
    renderPage();
    click(/næste/i);
    expect(screen.getByText("Ikrafttræden er påkrævet.")).toBeInTheDocument();
    expect(screen.getByText(/trin 1 af 3/i)).toBeInTheDocument();
  });

  it("shows an inline error when annulleres is not after ikrafttræden", () => {
    renderPage();
    type(/^ikrafttræden/i, "2026-11-05");
    type(/^annulleres/i, "2026-11-01");
    click(/næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();
    expect(screen.getByText(/trin 1 af 3/i)).toBeInTheDocument();
  });

  it("clears the annulleres date range error when ikrafttræden is corrected", () => {
    renderPage();
    type(/^ikrafttræden/i, "2026-11-05");
    type(/^annulleres/i, "2026-11-01");
    click(/næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();

    type(/^ikrafttræden/i, "2026-10-25");
    expect(
      screen.queryByText("Annulleres skal være efter Ikrafttræden."),
    ).not.toBeInTheDocument();
  });

  it("keeps the annulleres date range error when ikrafttræden is changed to another invalid date", () => {
    renderPage();
    type(/^ikrafttræden/i, "2026-11-05");
    type(/^annulleres/i, "2026-11-01");
    click(/næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();

    type(/^ikrafttræden/i, "2026-11-03");
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();
  });

  it("does not call the API when the last step is invalid", () => {
    renderPage();
    type(/^ikrafttræden/i, "2026-11-01");
    click(/næste/i);
    type(/^om situationen/i, "S");
    type(/^om driftsrestriktionen/i, "R");
    click(/næste/i);
    click(/opret kladde/i);
    expect(screen.getByText("Om ophævelse er påkrævet.")).toBeInTheDocument();
    expect(createMock).not.toHaveBeenCalled();
  });

  it("submits with the JWT and redirects with a success notification", async () => {
    createMock.mockResolvedValue(created);
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    expect(await screen.findByText("Detaljeside")).toBeInTheDocument();
    expect(
      screen.getByText("Driftsrestriktion oprettet som kladde."),
    ).toBeInTheDocument();
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        startDate: "2026-11-01",
        endDate: "2026-11-02",
        situationDescription: "Situationen",
        restrictionDescription: "Restriktionen",
        cancellationDescription: "Ophævelsen",
      }),
      "test-token",
    );
  });

  it("submits without annulleres and sends null", async () => {
    createMock.mockResolvedValue({ ...created, endDate: null });
    renderPage();
    fillToLastStep({ withEndDate: false });
    click(/opret kladde/i);

    expect(await screen.findByText("Detaljeside")).toBeInTheDocument();
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({ endDate: null }),
      "test-token",
    );
  });

  it("shows a loading state on the submit button while the request runs", async () => {
    createMock.mockReturnValue(new Promise(() => {})); // never resolves
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    const button = await screen.findByRole("button", { name: /opretter/i });
    expect(button).toBeDisabled();
  });

  it("shows server validation errors above the form on 400", async () => {
    createMock.mockRejectedValue(
      new RestrictionValidationError(["Referencen er for lang."]),
    );
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Referencen er for lang.",
    );
  });
});
