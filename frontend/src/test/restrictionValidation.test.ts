import { describe, expect, it } from "vitest";
import {
  EMPTY_VALUES,
  hasErrors,
  validateRestrictionForm,
} from "../lib/restrictionValidation";

const valid = {
  title: "Speed restriction",
  description: "Reduced speed on the M3 line",
  location: "Nørreport",
  startDate: "2026-11-01",
  endDate: "2026-11-02",
  notes: "",
};

describe("validateRestrictionForm", () => {
  it("flags every empty required field", () => {
    const errors = validateRestrictionForm(EMPTY_VALUES);
    expect(Object.keys(errors).sort()).toEqual([
      "description",
      "endDate",
      "location",
      "startDate",
      "title",
    ]);
  });

  it("accepts a valid form (notes are optional)", () => {
    expect(hasErrors(validateRestrictionForm(valid))).toBe(false);
  });

  it("rejects an end date before the start date", () => {
    const errors = validateRestrictionForm({ ...valid, endDate: "2026-10-01" });
    expect(errors.endDate).toMatch(/after start date/i);
  });

  it("rejects an end date equal to the start date", () => {
    const errors = validateRestrictionForm({ ...valid, endDate: valid.startDate });
    expect(errors.endDate).toMatch(/after start date/i);
  });

  it("only validates the requested fields", () => {
    const errors = validateRestrictionForm(EMPTY_VALUES, ["title"]);
    expect(Object.keys(errors)).toEqual(["title"]);
  });
});