// Pure validation, kept separate from the page so it is easy to unit test.
export interface RestrictionFormValues {
  title: string;
  description: string;
  location: string;
  startDate: string;
  endDate: string;
  notes: string;
}

export type RestrictionField = keyof RestrictionFormValues;
export type FormErrors = Partial<Record<RestrictionField, string>>;

export const EMPTY_VALUES: RestrictionFormValues = {
  title: "",
  description: "",
  location: "",
  startDate: "",
  endDate: "",
  notes: "",
};

const REQUIRED: Record<string, string> = {
  title: "Title is required.",
  description: "Description is required.",
  location: "Location is required.",
  startDate: "Start date is required.",
  endDate: "End date is required.",
};

export function validateRestrictionForm(
  values: RestrictionFormValues,
  fields: RestrictionField[] = Object.keys(values) as RestrictionField[],
): FormErrors {
  const errors: FormErrors = {};

  for (const field of fields) {
    if (field in REQUIRED && values[field].trim() === "") {
      errors[field] = REQUIRED[field];
    }
  }

  // yyyy-MM-dd strings compare correctly as plain strings.
  if (
    fields.includes("endDate") &&
    values.startDate &&
    values.endDate &&
    values.endDate <= values.startDate
  ) {
    errors.endDate = "End date must be after start date.";
  }

  return errors;
}

export function hasErrors(errors: FormErrors): boolean {
  return Object.values(errors).some(Boolean);
}
