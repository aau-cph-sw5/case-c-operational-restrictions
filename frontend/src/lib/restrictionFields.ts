// Single source of truth for the form: field names, Danish labels, step.
export const STEPS = [
  {
    title: "Navngiv restriktionen",
    eyebrow: "NAVNGIV RESTRIKTIONEN",
    heading: "Hvad skal vi kalde den?",
    description:
      "Brug en tydelig og specifik titel samt en eventuel reference.",
  },
  {
    title: "Detaljer",
    eyebrow: "TILFØJ DETALJERNE",
    heading: "Hvad har teamet brug for at vide?",
    description:
      "Forklar hvorfor driftsrestriktionen indføres og hvad der kræves for at håndhæve den.",
  },
  {
    title: "Varighed",
    eyebrow: "SÆT VARIGHEDEN",
    heading: "Hvad er varigheden?",
    description:
      "Indsæt den påkrævede startdato, for hvornår restriktionen træder i kraft. Slutdato kan efterlades tom, hvis den er ukendt.",
  },
  {
    title: "Ophævelse",
    eyebrow: "OPHÆVELSE",
    heading: "Hvordan ophæves den?",
    description:
      "Beskriv hvad der kræves for at ophæve restriktionen – ressourcer, udstyr eller rettelser.",
  },
  {
    title: "Gennemse",
    eyebrow: "GENNEMSE OG OPRET",
    heading: "Ser alt rigtigt ud?",
    description:
      "Gennemse nedenstående oplysninger, før du opretter driftsrestriktionen som kladde.",
  },
] as const;

export const FIELDS = [
  {
    name: "title",
    label: "Titel",
    uiLabel: "Titel",
    placeholder: "F.eks. Adgang til nordre forplads lukket",
    kind: "text",
    required: true,
    step: 0,
    charLimit: 100,
  },
  {
    name: "reference",
    label: "Reference",
    uiLabel: "Reference",
    placeholder: "F.eks. REF-2026-001",
    kind: "text",
    required: false,
    step: 0,
    charLimit: 100,
  },
  {
    name: "situationDescription",
    label: "Om situationen",
    uiLabel: "Om situationen",
    helper: "Forklar hvorfor driftsrestriktionen indføres.",
    placeholder:
      "F.eks. Køretøjsadgang til nordre forplads er suspenderet på grund af planlagt belægningsarbejde.",
    kind: "textarea",
    required: true,
    step: 1,
    charLimit: 500,
  },
  {
    name: "restrictionDescription",
    label: "Om driftsrestriktionen",
    uiLabel: "Om driftsrestriktionen",
    helper:
      "Beskriv hvad (ressourcer, udstyr) der etablerer driftsrestriktionen, hvad der skal til for at håndhæve den, og dens evt. varighed.",
    placeholder:
      "F.eks. Brug den skiltede østlige servicerute. Adgangsveje afspærres og overvåges af driftsafdelingen.",
    kind: "textarea",
    required: true,
    step: 1,
    charLimit: 500,
  },
  {
    name: "startDate",
    label: "Ikrafttræden",
    uiLabel: "Startdato",
    kind: "date",
    required: true,
    step: 2,
  },
  {
    name: "endDate",
    label: "Annulleres",
    uiLabel: "Slutdato",
    kind: "date",
    required: false,
    step: 2,
  },
  {
    name: "cancellationDescription",
    label: "Om ophævelse",
    uiLabel: "Om ophævelse",
    helper:
      "Beskriv hvad der kræves for at ophæve restriktionen – ressourcer, udstyr, rettelser i fx procedurer eller manualer, træning osv.",
    placeholder:
      "F.eks. Kræver godkendelse fra sikkerhedsteamet, fjernelse af midlertidige afspærringer samt opdatering af rutevejledninger.",
    kind: "textarea",
    required: true,
    step: 3,
    charLimit: 500,
  },
] as const;

export type RestrictionField = (typeof FIELDS)[number]["name"];
export type RestrictionFormValues = Record<RestrictionField, string>;
