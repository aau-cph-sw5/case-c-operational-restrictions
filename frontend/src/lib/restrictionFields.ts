export const STEPS = [
  { title: "Grundoplysninger" },
  { title: "Periode" },
  { title: "Lokation" },
] as const;

export const FIELDS = [
  { name: "title", label: "Titel", kind: "text", required: true, step: 0 },
  {
    name: "description",
    label: "Beskrivelse",
    kind: "textarea",
    required: true,
    step: 0,
  },
  {
    name: "startDate",
    label: "Startdato",
    kind: "date",
    required: true,
    step: 1,
  },
  {
    name: "endDate",
    label: "Slutdato",
    kind: "date",
    required: false,
    step: 1,
  },
  { name: "location", label: "Sted", kind: "text", required: true, step: 2 },
  {
    name: "notes",
    label: "Bemærkninger",
    kind: "textarea",
    required: false,
    step: 2,
  },
] as const;

export type RestrictionField = (typeof FIELDS)[number]["name"];
export type RestrictionFormValues = Record<RestrictionField, string>;
