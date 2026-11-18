import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SetPasswordPage } from "../pages/SetPasswordPage";
import { authService } from "../services/authService";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("SetPasswordPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows invalid state when token is missing from query string", () => {
    render(
      <MemoryRouter initialEntries={["/set-password"]}>
        <Routes>
          <Route path="/set-password" element={<SetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(
      screen.getByRole("heading", { name: "Ugyldigt eller udløbet link" }),
    ).toBeInTheDocument();
    expect(screen.getByText(/mangler et sikkerhedstoken/i)).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Gå til login" }),
    ).toBeInTheDocument();
  });

  it("shows invalid state when token verification fails", async () => {
    vi.spyOn(authService, "validateToken").mockRejectedValue(
      new Error("Linket er udløbet."),
    );

    render(
      <MemoryRouter initialEntries={["/set-password?token=invalid-token"]}>
        <Routes>
          <Route path="/set-password" element={<SetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "Ugyldigt eller udløbet link" }),
      ).toBeInTheDocument();
      expect(screen.getByText("Linket er udløbet.")).toBeInTheDocument();
    });
  });

  it("renders form when token is valid and validates password requirements", async () => {
    vi.spyOn(authService, "validateToken").mockResolvedValue({
      id: "00000000-0000-0000-0000-000000000001",
      name: "Test Operator",
      email: "operator@metroservice.dk",
    });

    render(
      <MemoryRouter initialEntries={["/set-password?token=valid-token"]}>
        <Routes>
          <Route path="/set-password" element={<SetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "Opret adgangskode" }),
      ).toBeInTheDocument();
      expect(
        screen.getByText(/operator@metroservice\.dk/i),
      ).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole("button", {
      name: "Gem adgangskode",
    });
    expect(submitBtn).toBeDisabled();

    const newPasswordInput = screen.getByLabelText(/^ny adgangskode/i);
    const confirmPasswordInput = screen.getByLabelText(/^bekræft adgangskode/i);

    // Enter short password
    fireEvent.change(newPasswordInput, { target: { value: "Short1!" } });
    fireEvent.change(confirmPasswordInput, { target: { value: "Short1!" } });
    expect(submitBtn).toBeDisabled();

    // Enter valid password
    fireEvent.change(newPasswordInput, {
      target: { value: "ValidPassword123" },
    });
    fireEvent.change(confirmPasswordInput, {
      target: { value: "ValidPassword123" },
    });

    expect(submitBtn).not.toBeDisabled();
  });

  it("submits new password and shows browser-adaptive success message", async () => {
    vi.spyOn(authService, "validateToken").mockResolvedValue({
      id: "00000000-0000-0000-0000-000000000001",
      name: "Test Operator",
      email: "operator@metroservice.dk",
    });
    vi.spyOn(authService, "setPassword").mockResolvedValue();

    render(
      <MemoryRouter initialEntries={["/set-password?token=valid-token"]}>
        <Routes>
          <Route path="/set-password" element={<SetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "Opret adgangskode" }),
      ).toBeInTheDocument();
    });

    fireEvent.change(screen.getByLabelText(/^ny adgangskode/i), {
      target: { value: "ValidPassword123" },
    });
    fireEvent.change(screen.getByLabelText(/^bekræft adgangskode/i), {
      target: { value: "ValidPassword123" },
    });

    fireEvent.click(screen.getByRole("button", { name: "Gem adgangskode" }));

    await waitFor(() => {
      expect(authService.setPassword).toHaveBeenCalledWith({
        token: "valid-token",
        password: "ValidPassword123",
      });
      expect(
        screen.getByRole("heading", { name: "Adgangskode oprettet" }),
      ).toBeInTheDocument();
      expect(screen.getByText(/lukke denne fane/i)).toBeInTheDocument();
      expect(
        screen.getByRole("button", { name: "Log ind i browseren" }),
      ).toBeInTheDocument();
    });
  });

  it("shows standalone PWA success state when in standalone display mode", async () => {
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

    vi.spyOn(authService, "validateToken").mockResolvedValue({
      id: "00000000-0000-0000-0000-000000000001",
      name: "Test Operator",
      email: "operator@metroservice.dk",
    });
    vi.spyOn(authService, "setPassword").mockResolvedValue();

    render(
      <MemoryRouter initialEntries={["/set-password?token=valid-token"]}>
        <Routes>
          <Route path="/set-password" element={<SetPasswordPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(
        screen.getByRole("heading", { name: "Opret adgangskode" }),
      ).toBeInTheDocument();
    });

    fireEvent.change(screen.getByLabelText(/^ny adgangskode/i), {
      target: { value: "ValidPassword123" },
    });
    fireEvent.change(screen.getByLabelText(/^bekræft adgangskode/i), {
      target: { value: "ValidPassword123" },
    });

    fireEvent.click(screen.getByRole("button", { name: "Gem adgangskode" }));

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: "Gå til login" }),
      ).toBeInTheDocument();
      expect(screen.queryByText(/lukke denne fane/i)).not.toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: "Gå til login" }));
    expect(mockNavigate).toHaveBeenCalledWith("/login");
  });
});
