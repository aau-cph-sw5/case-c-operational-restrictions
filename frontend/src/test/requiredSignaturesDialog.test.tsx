import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RequiredSignaturesDialog } from "../components/RequiredSignatures/RequiredSignaturesDialog";
import type { RequiredSignatureDto } from "../types/requiredSignature";
import type { RestrictionDto } from "../types/restrictionDto";

const mocks = vi.hoisted(() => ({
  getMyPendingRequiredSignatures: vi.fn(),
  signRequiredSignature: vi.fn(),
  getRestrictionById: vi.fn(),
}));

vi.mock("../services/requiredSignatureApi", () => ({
  getMyPendingRequiredSignatures: mocks.getMyPendingRequiredSignatures,
  signRequiredSignature: mocks.signRequiredSignature,
}));

vi.mock("../services/restrictionApi", () => ({
  getRestrictionById: mocks.getRestrictionById,
}));

const required: RequiredSignatureDto = {
  requiresSignatureId: "required-1",
  restrictionId: "restriction-2",
  title: "Sporafspærring M3",
  reference: "REF-99",
  startDate: "2026-11-01T08:00:00Z",
  endDate: null,
  requiredSince: "2026-10-30T08:00:00Z",
};

const restriction: RestrictionDto = {
  id: "restriction-2",
  title: "Sporafspærring M3",
  originatorUserId: "user-1",
  status: "Active",
  startDate: "2026-11-01T08:00:00Z",
  endDate: null,
  restrictionDescription: "Hastighed 40 km/t mellem Vestamager og Lindevang",
  signatures: [],
};

describe("RequiredSignaturesDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getRestrictionById.mockResolvedValue(restriction);
    mocks.signRequiredSignature.mockResolvedValue(undefined);
  });

  it("renders nothing when the user has no required signatures", async () => {
    mocks.getMyPendingRequiredSignatures.mockResolvedValue([]);

    render(<RequiredSignaturesDialog />);

    await waitFor(() =>
      expect(mocks.getMyPendingRequiredSignatures).toHaveBeenCalled(),
    );
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("shows a list of required restrictions that cannot be dismissed", async () => {
    mocks.getMyPendingRequiredSignatures.mockResolvedValue([required]);

    render(<RequiredSignaturesDialog />);

    expect(
      await screen.findByText("Restriktioner du skal underskrive"),
    ).toBeInTheDocument();
    expect(screen.getAllByTestId("required-signature-row")).toHaveLength(1);
    expect(screen.getByText("REF-99")).toBeInTheDocument();
    expect(
      screen.getByText("1 restriktion venter på din underskrift"),
    ).toBeInTheDocument();

    fireEvent.keyDown(document.activeElement ?? document.body, {
      key: "Escape",
    });

    expect(
      screen.getByText("Restriktioner du skal underskrive"),
    ).toBeInTheDocument();
  });

  it("opens the restriction and closes the blocking dialog once everything is signed", async () => {
    mocks.getMyPendingRequiredSignatures
      .mockResolvedValueOnce([required])
      .mockResolvedValueOnce([]);

    render(<RequiredSignaturesDialog />);

    fireEvent.click(await screen.findByTestId("required-signature-row"));

    expect(
      await screen.findByText(
        "Hastighed 40 km/t mellem Vestamager og Lindevang",
      ),
    ).toBeInTheDocument();
    expect(mocks.getRestrictionById).toHaveBeenCalledWith("restriction-2");

    fireEvent.click(
      screen.getByRole("button", {
        name: "Jeg har læst restriktionen – underskriv",
      }),
    );

    await waitFor(() =>
      expect(mocks.signRequiredSignature).toHaveBeenCalledWith("required-1"),
    );
    await waitFor(() =>
      expect(
        screen.queryByText("Restriktioner du skal underskrive"),
      ).not.toBeInTheDocument(),
    );
  });

  it("only skips the required signatures after the user confirms", async () => {
    mocks.getMyPendingRequiredSignatures.mockResolvedValue([required]);

    render(<RequiredSignaturesDialog />);

    fireEvent.click(
      await screen.findByRole("button", {
        name: "Gå videre uden at underskrive",
      }),
    );
    expect(
      await screen.findByText(/Du har stadig 1 restriktion/),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Annuller" }));
    await waitFor(() =>
      expect(
        screen.queryByText(/Du har stadig 1 restriktion/),
      ).not.toBeInTheDocument(),
    );
    expect(
      screen.getByText("Restriktioner du skal underskrive"),
    ).toBeInTheDocument();

    fireEvent.click(
      screen.getByRole("button", { name: "Gå videre uden at underskrive" }),
    );
    fireEvent.click(await screen.findByRole("button", { name: "Gå videre" }));

    await waitFor(() =>
      expect(
        screen.queryByText("Restriktioner du skal underskrive"),
      ).not.toBeInTheDocument(),
    );
    expect(mocks.signRequiredSignature).not.toHaveBeenCalled();
  });

  it("stays open with a retry when the list cannot be loaded", async () => {
    mocks.getMyPendingRequiredSignatures
      .mockRejectedValueOnce(
        Object.assign(new Error("HTTP 500"), { status: 500 }),
      )
      .mockResolvedValueOnce([]);

    render(<RequiredSignaturesDialog />);

    fireEvent.click(await screen.findByRole("button", { name: "Prøv igen" }));

    await waitFor(() =>
      expect(mocks.getMyPendingRequiredSignatures).toHaveBeenCalledTimes(2),
    );
  });
});
