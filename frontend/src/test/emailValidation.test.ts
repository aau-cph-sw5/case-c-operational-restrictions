import { describe, expect, it } from "vitest";
import { isValidEmail } from "../lib/emailValidation";

describe("isValidEmail", () => {
  it("returns false for empty or whitespace strings", () => {
    expect(isValidEmail("")).toBe(false);
    expect(isValidEmail("   ")).toBe(false);
  });

  it("returns false for strings without @ or domain dot", () => {
    expect(isValidEmail("plainaddress")).toBe(false);
    expect(isValidEmail("user@domain")).toBe(false);
    expect(isValidEmail("user.name")).toBe(false);
    expect(isValidEmail("@domain.com")).toBe(false);
    expect(isValidEmail("user@.com")).toBe(false);
  });

  it("returns true for valid emails containing @ and domain dot", () => {
    expect(isValidEmail("user@metroservice.dk")).toBe(true);
    expect(isValidEmail("first.last@metroservice.dk")).toBe(true);
    expect(isValidEmail("operator+1@example.com")).toBe(true);
  });
});
