// Pure validation, kept separate from the page so it is easy to unit test.
import { FIELDS } from "./restrictionFields";
import type {
  RestrictionField,
  RestrictionFormValues,
} from "./restrictionFields";

export type { RestrictionField, RestrictionFormValues };
export type FormErrors = Partial<Record<RestrictionField, string>>;

export const EMPTY_VALUES = Object.fromEntries(
  FIELDS.map((field) => [field.name, ""]),
) as RestrictionFormValues;

export function validateRestrictionForm(
  values: RestrictionFormValues,
  fields: RestrictionField[] = FIELDS.map((field) => field.name),
): FormErrors {
  const errors: FormErrors = {};

  for (const def of FIELDS) {
    if (!fields.includes(def.name)) continue;
    if (def.required && values[def.name].trim() === "") {
      errors[def.name] = `${def.label} er påkrævet.`;
    }
  }

  // Slutdato is optional, so it is only compared when it has been filled in.
  // yyyy-MM-dd strings compare correctly as plain strings.
  if (
    fields.includes("endDate") &&
    values.startDate &&
    values.endDate &&
    values.endDate <= values.startDate
  ) {
    errors.endDate = "Slutdato skal være efter startdato.";
  }

  return errors;
}

export function hasErrors(errors: FormErrors): boolean {
  return Object.values(errors).some(Boolean);
}
