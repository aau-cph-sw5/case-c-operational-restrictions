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
  title: "Metro 101 - 2026 version",
  reference: "REF-2026-001",
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

function fillToLastStep({ withEndDate = true, withReference = true } = {}) {
  // Trin 1: Navngiv restriktionen
  type(/^titel/i, "Metro 101 - 2026 version");
  if (withReference) type(/^reference/i, "REF-2026-001");
  click(/fortsæt|næste/i);

  // Trin 2: Detaljer
  type(/^om situationen/i, "Situationen");
  type(/^om driftsrestriktionen/i, "Restriktionen");
  click(/fortsæt|næste/i);

  // Trin 3: Varighed
  type(/^startdato|^ikrafttræden/i, "2026-11-01");
  if (withEndDate) type(/^slutdato|^annulleres/i, "2026-11-02");
  click(/fortsæt|næste/i);

  // Trin 4: Ophævelse
  type(/^om ophævelse/i, "Ophævelsen");
  click(/fortsæt|næste/i);
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
    type(/^titel/i, "Metro 101 - 2026 version");
    expect(screen.getByLabelText(/^titel/i)).toHaveValue(
      "Metro 101 - 2026 version",
    );
  });

  it("blocks Fortsæt while titel is empty", () => {
    renderPage();
    click(/fortsæt|næste/i);
    expect(screen.getByText("Titel er påkrævet.")).toBeInTheDocument();
    expect(screen.getByText(/1 af 5/i)).toBeInTheDocument();
  });

  it("blocks Fortsæt while ikrafttræden is empty", () => {
    renderPage();
    type(/^titel/i, "Titel");
    click(/fortsæt|næste/i);
    type(/^om situationen/i, "Situationen");
    type(/^om driftsrestriktionen/i, "Restriktionen");
    click(/fortsæt|næste/i);

    expect(screen.getByText(/3 af 5/i)).toBeInTheDocument();
    click(/fortsæt|næste/i);
    expect(screen.getByText("Ikrafttræden er påkrævet.")).toBeInTheDocument();
    expect(screen.getByText(/3 af 5/i)).toBeInTheDocument();
  });

  it("shows an inline error when annulleres is not after ikrafttræden", () => {
    renderPage();
    type(/^titel/i, "Titel");
    click(/fortsæt|næste/i);
    type(/^om situationen/i, "Situationen");
    type(/^om driftsrestriktionen/i, "Restriktionen");
    click(/fortsæt|næste/i);

    type(/^startdato|^ikrafttræden/i, "2026-11-05");
    type(/^slutdato|^annulleres/i, "2026-11-01");
    click(/fortsæt|næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();
    expect(screen.getByText(/3 af 5/i)).toBeInTheDocument();
  });

  it("clears the annulleres date range error when ikrafttræden is corrected", () => {
    renderPage();
    type(/^titel/i, "Titel");
    click(/fortsæt|næste/i);
    type(/^om situationen/i, "Situationen");
    type(/^om driftsrestriktionen/i, "Restriktionen");
    click(/fortsæt|næste/i);

    type(/^startdato|^ikrafttræden/i, "2026-11-05");
    type(/^slutdato|^annulleres/i, "2026-11-01");
    click(/fortsæt|næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();

    type(/^startdato|^ikrafttræden/i, "2026-10-25");
    expect(
      screen.queryByText("Annulleres skal være efter Ikrafttræden."),
    ).not.toBeInTheDocument();
  });

  it("keeps the annulleres date range error when ikrafttræden is changed to another invalid date", () => {
    renderPage();
    type(/^titel/i, "Titel");
    click(/fortsæt|næste/i);
    type(/^om situationen/i, "Situationen");
    type(/^om driftsrestriktionen/i, "Restriktionen");
    click(/fortsæt|næste/i);

    type(/^startdato|^ikrafttræden/i, "2026-11-05");
    type(/^slutdato|^annulleres/i, "2026-11-01");
    click(/fortsæt|næste/i);
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();

    type(/^startdato|^ikrafttræden/i, "2026-11-03");
    expect(
      screen.getByText("Annulleres skal være efter Ikrafttræden."),
    ).toBeInTheDocument();
  });

  it("does not call the API when a required field is missing on submit", () => {
    renderPage();
    fillToLastStep();
    click(/tilbage/i);
    fireEvent.change(screen.getByLabelText(/^om ophævelse/i), {
      target: { value: "" },
    });
    click(/fortsæt|næste/i);
    expect(screen.getByText("Om ophævelse er påkrævet.")).toBeInTheDocument();
    expect(createMock).not.toHaveBeenCalled();
  });

  it("allows clicking progress tabs to navigate between steps faster", () => {
    renderPage();
    expect(screen.getByText(/1 af 5/i)).toBeInTheDocument();

    type(/^titel/i, "Test Titel");
    click(/fortsæt|næste/i);
    expect(screen.getByText(/2 af 5/i)).toBeInTheDocument();

    const tabs = screen.getAllByRole("tab");
    expect(tabs).toHaveLength(5);
    fireEvent.click(tabs[0]);
    expect(screen.getByText(/1 af 5/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^titel/i)).toHaveValue("Test Titel");

    fireEvent.click(tabs[1]);
    expect(screen.getByText(/2 af 5/i)).toBeInTheDocument();
  });

  it("navigates to the corresponding step when clicking Rediger in the review summary", () => {
    renderPage();
    fillToLastStep();
    expect(screen.getByText(/5 af 5/i)).toBeInTheDocument();

    const redigerButtons = screen.getAllByRole("button", { name: /rediger/i });
    fireEvent.click(redigerButtons[0]);
    expect(screen.getByText(/1 af 5/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^titel/i)).toHaveValue(
      "Metro 101 - 2026 version",
    );
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
        title: "Metro 101 - 2026 version",
        reference: "REF-2026-001",
        startDate: "2026-11-01",
        endDate: "2026-11-02",
        situationDescription: "Situationen",
        restrictionDescription: "Restriktionen",
        cancellationDescription: "Ophævelsen",
      }),
      "test-token",
    );
  });

  it("submits without reference and sends undefined", async () => {
    createMock.mockResolvedValue({ ...created, reference: undefined });
    renderPage();
    fillToLastStep({ withReference: false });
    click(/opret kladde/i);

    expect(await screen.findByText("Detaljeside")).toBeInTheDocument();
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Metro 101 - 2026 version",
        reference: undefined,
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
      new RestrictionValidationError(["Titlen er for lang."]),
    );
    renderPage();
    fillToLastStep();
    click(/opret kladde/i);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Titlen er for lang.",
    );
  });

  it("advances to the next step when pressing Enter in a single-line text input", () => {
    renderPage();
    expect(screen.getByText(/1 af 5/i)).toBeInTheDocument();

    const titleInput = screen.getByLabelText(/^titel/i);
    fireEvent.change(titleInput, { target: { value: "Enter Test Title" } });
    fireEvent.keyDown(titleInput, { key: "Enter", code: "Enter" });

    expect(screen.getByText(/2 af 5/i)).toBeInTheDocument();
  });

  it("blocks forward tab jumping to step 5 if intermediate steps are incomplete", () => {
    renderPage();
    type(/^titel/i, "Valid Title");

    // Click tab 4 (Step 5: Gennemse)
    const tabs = screen.getAllByRole("tab");
    fireEvent.click(tabs[4]);

    // Should stop at Step 2 (first incomplete step) and flag missing descriptions
    expect(screen.getByText(/2 af 5/i)).toBeInTheDocument();
    expect(screen.getByText("Om situationen er påkrævet.")).toBeInTheDocument();
    expect(
      screen.getByText("Om driftsrestriktionen er påkrævet."),
    ).toBeInTheDocument();
  });

  it("associates aria-describedby with the error message when validation fails", () => {
    renderPage();
    click(/fortsæt|næste/i);

    const titleInput = screen.getByLabelText(/^titel/i);
    expect(titleInput).toHaveAttribute("aria-invalid", "true");
    expect(titleInput).toHaveAttribute("aria-describedby", "title-error");
    expect(screen.getByRole("alert")).toHaveTextContent("Titel er påkrævet.");
  });
});
