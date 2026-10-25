import { describe, expect, it } from "vitest";
import {
  EMPTY_VALUES,
  hasErrors,
  validateRestrictionForm,
} from "../lib/restrictionValidation";

const valid = {
  title: "Hastigheds Begrænsning",
  description: "Nedsat hastighed på M3",
  location: "Nørreport",
  startDate: "2026-11-01",
  endDate: "2026-11-02",
  notes: "",
};

describe("validateRestrictionForm", () => {
  it("flags every empty required field, but not slutdato", () => {
    const errors = validateRestrictionForm(EMPTY_VALUES);
    expect(Object.keys(errors).sort()).toEqual([
      "description",
      "location",
      "startDate",
      "title",
    ]);
  });

  it("uses Danish error messages", () => {
    expect(validateRestrictionForm(EMPTY_VALUES).title).toBe(
      "Titel er påkrævet.",
    );
  });

  it("accepts a valid form", () => {
    expect(hasErrors(validateRestrictionForm(valid))).toBe(false);
  });

  it("accepts a form without slutdato", () => {
    expect(hasErrors(validateRestrictionForm({ ...valid, endDate: "" }))).toBe(
      false,
    );
  });

  it("rejects a slutdato before the startdato", () => {
    const errors = validateRestrictionForm({ ...valid, endDate: "2026-10-01" });
    expect(errors.endDate).toBe("Slutdato skal være efter startdato.");
  });

  it("rejects a slutdato equal to the startdato", () => {
    const errors = validateRestrictionForm({
      ...valid,
      endDate: valid.startDate,
    });
    expect(errors.endDate).toBe("Slutdato skal være efter startdato.");
  });

  it("only validates the requested fields", () => {
    const errors = validateRestrictionForm(EMPTY_VALUES, ["title"]);
    expect(Object.keys(errors)).toEqual(["title"]);
  });
});
