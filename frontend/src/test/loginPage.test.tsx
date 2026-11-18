import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "../pages/LoginPage";
import { authService } from "../services/authService";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("LoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders login form with Danish labels and inputs", () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    expect(
      screen.getByRole("heading", { name: "Log ind" }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/^e-mail/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^adgangskode/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Log ind" })).toBeInTheDocument();
  });

  it("disables login button until both a valid email and password are provided", () => {
    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    const loginBtn = screen.getByRole("button", { name: "Log ind" });
    const emailInput = screen.getByLabelText(/^e-mail/i);
    const passwordInput = screen.getByLabelText(/^adgangskode/i);

    expect(loginBtn).toBeDisabled();

    // Invalid email without @ or dot
    fireEvent.change(emailInput, { target: { value: "invalidemail" } });
    fireEvent.change(passwordInput, { target: { value: "Secret123!" } });
    expect(loginBtn).toBeDisabled();

    // Email with @ but no domain dot
    fireEvent.change(emailInput, { target: { value: "user@metroservice" } });
    expect(loginBtn).toBeDisabled();

    // Valid email
    fireEvent.change(emailInput, { target: { value: "user@metroservice.dk" } });
    expect(loginBtn).not.toBeDisabled();
  });

  it("submits login form and navigates to /home on success", async () => {
    vi.spyOn(authService, "login").mockResolvedValue({
      accessToken: "mock-token",
      expiresAtUtc: "2026-12-31T23:59:59Z",
      user: {
        id: "00000000-0000-0000-0000-000000000001",
        name: "Test User",
        email: "test@metroservice.dk",
      },
    });

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    fireEvent.change(screen.getByLabelText(/^e-mail/i), {
      target: { value: "test@metroservice.dk" },
    });
    fireEvent.change(screen.getByLabelText(/^adgangskode/i), {
      target: { value: "Password123!" },
    });

    fireEvent.click(screen.getByRole("button", { name: "Log ind" }));

    await waitFor(() => {
      expect(authService.login).toHaveBeenCalledWith({
        email: "test@metroservice.dk",
        password: "Password123!",
      });
      expect(mockNavigate).toHaveBeenCalledWith("/home", { replace: true });
    });
  });

  it("displays error alert when login fails", async () => {
    vi.spyOn(authService, "login").mockRejectedValue(
      new Error("Forkert e-mail eller adgangskode."),
    );

    render(
      <MemoryRouter>
        <LoginPage />
      </MemoryRouter>,
    );

    fireEvent.change(screen.getByLabelText(/^e-mail/i), {
      target: { value: "wrong@metroservice.dk" },
    });
    fireEvent.change(screen.getByLabelText(/^adgangskode/i), {
      target: { value: "WrongPassword!" },
    });

    fireEvent.click(screen.getByRole("button", { name: "Log ind" }));

    await waitFor(() => {
      expect(
        screen.getByText("Forkert e-mail eller adgangskode."),
      ).toBeInTheDocument();
    });
  });
});
