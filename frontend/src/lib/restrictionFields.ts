// Single source of truth for the form: field names, Danish labels, step.
export const STEPS = [
  { title: "Periode" },
  { title: "Situation og restriktion" },
  { title: "Ophævelse" },
] as const;

export const FIELDS = [
  {
    name: "startDate",
    label: "Ikrafttræden",
    kind: "date",
    required: true,
    step: 0,
  },
  {
    name: "endDate",
    label: "Annulleres",
    kind: "date",
    required: false,
    step: 0,
  },
  {
    name: "reference",
    label: "Reference",
    kind: "text",
    required: false,
    step: 0,
    charLimit: 100,
  },
  {
    name: "situationDescription",
    label: "Om situationen",
    kind: "textarea",
    required: true,
    step: 1,
    charLimit: 500,
  },
  {
    name: "restrictionDescription",
    label: "Om driftsrestriktionen",
    kind: "textarea",
    required: true,
    step: 1,
    charLimit: 500,
  },
  {
    name: "cancellationDescription",
    label: "Om ophævelse",
    kind: "textarea",
    required: true,
    step: 2,
    charLimit: 500,
  },
] as const;

export type RestrictionField = (typeof FIELDS)[number]["name"];
export type RestrictionFormValues = Record<RestrictionField, string>;
