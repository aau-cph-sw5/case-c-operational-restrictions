import { describe, expect, it } from "vitest";
import { validatePassword } from "../lib/passwordValidation";

describe("validatePassword", () => {
  it("fails if password is too short", () => {
    const res = validatePassword("Aa1!", "Aa1!");
    expect(res.hasMinLength).toBe(false);
    expect(res.isValid).toBe(false);
  });

  it("fails if password lacks uppercase letter", () => {
    const res = validatePassword("password123", "password123");
    expect(res.hasUppercase).toBe(false);
    expect(res.isValid).toBe(false);
  });

  it("fails if password lacks lowercase letter", () => {
    const res = validatePassword("PASSWORD123", "PASSWORD123");
    expect(res.hasLowercase).toBe(false);
    expect(res.isValid).toBe(false);
  });

  it("fails if password lacks number", () => {
    const res = validatePassword("PasswordOnly", "PasswordOnly");
    expect(res.hasNumber).toBe(false);
    expect(res.isValid).toBe(false);
  });

  it("fails if confirm password does not match", () => {
    const res = validatePassword("ValidPass123", "DifferentPass123");
    expect(res.hasMinLength).toBe(true);
    expect(res.hasUppercase).toBe(true);
    expect(res.hasLowercase).toBe(true);
    expect(res.hasNumber).toBe(true);
    expect(res.passwordsMatch).toBe(false);
    expect(res.isValid).toBe(false);
  });

  it("succeeds when all requirements are met", () => {
    const res = validatePassword("ValidPass123", "ValidPass123");
    expect(res.hasMinLength).toBe(true);
    expect(res.hasUppercase).toBe(true);
    expect(res.hasLowercase).toBe(true);
    expect(res.hasNumber).toBe(true);
    expect(res.passwordsMatch).toBe(true);
    expect(res.isValid).toBe(true);
  });
});
