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
      Detail page
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

function fillToLastStep() {
  type(/^title/i, "Speed restriction");
  type(/^description/i, "Reduced speed");
  click(/next/i);
  type(/^start date/i, "2026-11-01");
  type(/^end date/i, "2026-11-02");
  click(/next/i);
  type(/^location/i, "Nørreport");
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
    type(/^title/i, "Speed restriction");
    expect(screen.getByLabelText(/^title/i)).toHaveValue("Speed restriction");
  });

  it("blocks Next while required fields are empty", () => {
    renderPage();
    click(/next/i);
    expect(screen.getByText("Title is required.")).toBeInTheDocument();
    expect(screen.getByText("Description is required.")).toBeInTheDocument();
    expect(screen.getByText(/step 1 of 3/i)).toBeInTheDocument();
  });

  it("shows an inline error when end date is not after start date", () => {
    renderPage();
    type(/^title/i, "T");
    type(/^description/i, "D");
    click(/next/i);
    type(/^start date/i, "2026-11-05");
    type(/^end date/i, "2026-11-01");
    click(/next/i);
    expect(screen.getByText(/after start date/i)).toBeInTheDocument();
    expect(screen.getByText(/step 2 of 3/i)).toBeInTheDocument();
  });

  it("does not call the API when the last step is invalid", () => {
    renderPage();
    type(/^title/i, "T");
    type(/^description/i, "D");
    click(/next/i);
    type(/^start date/i, "2026-11-01");
    type(/^end date/i, "2026-11-02");
    click(/next/i);
    click(/create draft/i);
    expect(screen.getByText("Location is required.")).toBeInTheDocument();
    expect(createMock).not.toHaveBeenCalled();
  });

  it("submits with the JWT and redirects with a success notification", async () => {
    createMock.mockResolvedValue({
      id: "abc-123",
      title: "Speed restriction",
      description: "Reduced speed",
      location: "Nørreport",
      startDate: "2026-11-01",
      endDate: "2026-11-02",
    });
    renderPage();
    fillToLastStep();
    click(/create draft/i);

    expect(await screen.findByText("Detail page")).toBeInTheDocument();
    expect(screen.getByText("Restriction draft created.")).toBeInTheDocument();
    expect(createMock).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Speed restriction",
        startDate: "2026-11-01",
        endDate: "2026-11-02",
      }),
      "test-token",
    );
  });

  it("shows a loading state on the submit button while the request runs", async () => {
    createMock.mockReturnValue(new Promise(() => {})); // never resolves
    renderPage();
    fillToLastStep();
    click(/create draft/i);

    const button = await screen.findByRole("button", { name: /creating/i });
    expect(button).toBeDisabled();
  });

  it("shows server validation errors above the form on 400", async () => {
    createMock.mockRejectedValue(
      new RestrictionValidationError(["Title is too long."]),
    );
    renderPage();
    fillToLastStep();
    click(/create draft/i);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Title is too long.",
    );
  });
});
