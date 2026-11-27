import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { SignatureOverviewPage } from "../pages/SignatureOverviewPage";
import type { SignatureCompletenessReportDto } from "../types/signatureCompleteness";

const mocks = vi.hoisted(() => ({
  getSignatureCompleteness: vi.fn(),
}));

vi.mock("../services/signatureCompletenessApi", () => ({
  getSignatureCompleteness: mocks.getSignatureCompleteness,
}));

const report: SignatureCompletenessReportDto = {
  computedAt: "2026-11-18T10:00:00Z",
  outstandingSignatureCount: 1,
  restrictions: [
    {
      restrictionId: "restriction-1",
      title: "Kongens Nytorv",
      reference: "REF-001",
    },
    {
      restrictionId: "restriction-2",
      title: "Nørreport",
      reference: "REF-002",
    },
  ],
  operators: [
    {
      operatorId: "user-1",
      name: "Operator One",
      cells: [
        {
          restrictionId: "restriction-1",
          isSigned: false,
          signedAt: null,
        },
        {
          restrictionId: "restriction-2",
          isSigned: true,
          signedAt: "2026-11-18T09:00:00Z",
        },
      ],
    },
  ],
};

describe("SignatureOverviewPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    mocks.getSignatureCompleteness.mockResolvedValue(report);
  });

  it("loads and displays the signature report", async () => {
    render(<SignatureOverviewPage />);

    expect(
      screen.getByText("Loading signature overview..."),
    ).toBeInTheDocument();

    expect(await screen.findByText("Kongens Nytorv")).toBeInTheDocument();
    expect(mocks.getSignatureCompleteness).toHaveBeenCalledTimes(1);
  });

  it("searches for a restriction by title", async () => {
    render(<SignatureOverviewPage />);

    await screen.findByText("Kongens Nytorv");

    const searchInput = screen.getByPlaceholderText(/search/i);

    fireEvent.change(searchInput, {
      target: { value: "Nørreport" },
    });

    expect(screen.getByText("Nørreport")).toBeInTheDocument();
    expect(screen.queryByText("Kongens Nytorv")).not.toBeInTheDocument();
  });

  it("filters restrictions with missing signatures", async () => {
    render(<SignatureOverviewPage />);

    await screen.findByText("Kongens Nytorv");

    fireEvent.click(screen.getByRole("button", { name: /^missing$/i }));

    expect(screen.getByText("Kongens Nytorv")).toBeInTheDocument();
    expect(screen.queryByText("Nørreport")).not.toBeInTheDocument();
  });

  it("shows completed restrictions when Complete is selected", async () => {
    render(<SignatureOverviewPage />);

    await screen.findByText("Kongens Nytorv");

    fireEvent.click(screen.getByRole("button", { name: /^complete$/i }));

    expect(screen.getByText("Nørreport")).toBeInTheDocument();
    expect(screen.queryByText("Kongens Nytorv")).not.toBeInTheDocument();
  });

  it("shows an error message when the API fails", async () => {
    mocks.getSignatureCompleteness.mockRejectedValue(
      new Error("Network error"),
    );

    render(<SignatureOverviewPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Could not load signature overview.",
    );
  });

  it("shows a login message for 401 Unauthorized", async () => {
    mocks.getSignatureCompleteness.mockRejectedValue(
      Object.assign(new Error("Unauthorized"), { status: 401 }),
    );

    render(<SignatureOverviewPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Your session has expired. Please log in again.",
    );
  });

  it("shows a permission message for 403 Forbidden", async () => {
    mocks.getSignatureCompleteness.mockRejectedValue(
      Object.assign(new Error("Forbidden"), { status: 403 }),
    );

    render(<SignatureOverviewPage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "You do not have permission to view signature overview.",
    );
  });
});
