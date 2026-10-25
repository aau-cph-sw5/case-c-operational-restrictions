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
  type(/^titel/i, "Hastigheds Begrænsning");
  type(/^beskrivelse/i, "Nedsat hastighed");
  click(/næste/i);
  type(/^startdato/i, "2026-11-01");
  if (withEndDate) type(/^slutdato/i, "2026-11-02");
  click(/næste/i);
  type(/^sted/i, "Nørreport");
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
    type(/^titel/i, "Hastigheds Begrænsning");
    expect(screen.getByLabelText(/^titel/i)).toHaveValue(
      "Hastigheds Begrænsning",
    );
  });

  it("blocks Næste while required fields are empty", () => {
    renderPage();
    click(/næste/i);
    expect(screen.getByText("Titel er påkrævet.")).toBeInTheDocument();
    expect(screen.getByText("Beskrivelse er påkrævet.")).toBeInTheDocument();
    expect(screen.getByText(/trin 1 af 3/i)).toBeInTheDocument();
  });

  it("shows an inline error when slutdato is not after startdato", () => {
    renderPage();
    type(/^titel/i, "T");
    type(/^beskrivelse/i, "B");
    click(/næste/i);
    type(/^startdato/i, "2026-11-05");
    type(/^slutdato/i, "2026-11-01");
    click(/næste/i);
    expect(
      screen.getByText("Slutdato skal være efter startdato."),
    ).toBeInTheDocument();
    expect(screen.getByText(/trin 2 af 3/i)).toBeInTheDocument();
  });

  it("does not call the API when the last step is invalid", () => {
    renderPage();
    type(/^titel/i, "T");
    type(/^beskrivelse/i, "B");
    click(/næste/i);
    type(/^startdato/i, "2026-11-01");
    click(/næste/i);
    click(/opret kladde/i);
    expect(screen.getByText("Sted er påkrævet.")).toBeInTheDocument();
    expect(createMock).not.toHaveBeenCalled();
  });

  it("submits with the JWT and redirects with a success notification", async () => {
    createMock.mockResolvedValue({
      id: "abc-123",
      title: "Hastigheds Begrænsning",
      description: "Nedsat hastighed",
      location: "Nørreport",
      startDate: "2026-11-01",
      endDate: "2026-11-02",
    });
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    expect(await screen.findByText("Detaljeside")).toBeInTheDocument();
    expect(
      screen.getByText("Driftsrestriktion oprettet som kladde."),
    ).toBeInTheDocument();
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Hastigheds Begrænsning",
        startDate: "2026-11-01",
        endDate: "2026-11-02",
      }),
      "test-token",
    );
  });

  it("submits without slutdato and sends null", async () => {
    createMock.mockResolvedValue({
      id: "abc-123",
      title: "Hastigheds Begrænsning",
      description: "Nedsat hastighed",
      location: "Nørreport",
      startDate: "2026-11-01",
      endDate: null,
    });
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
      new RestrictionValidationError(["Titlen er for lang."]),
    );
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Titlen er for lang.",
    );
  });
});
