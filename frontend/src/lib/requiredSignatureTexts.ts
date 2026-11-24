// All user-facing text for the required-signatures dialogs (Danish, per the design).
export const requiredSignatureTexts = {
  heading: "Restriktioner du skal underskrive",
  description:
    "Følgende driftsrestriktioner er i kraft. Du skal læse og underskrive dem alle, før du kan fortsætte.",
  listLabel: "Restriktioner der kræver din underskrift",
  startDate: "Start",
  endDate: "Slut",
  open: "Læs og underskriv",
  loadFailed:
    "Dine påkrævede underskrifter kunne ikke hentes. Prøv igen, før du fortsætter.",
  retry: "Prøv igen",
  restrictionHeading: "Driftsrestriktion",
  restrictionDescription:
    "Læs restriktionen igennem, og underskriv for at bekræfte, at du har læst den.",
  loadingRestriction: "Henter restriktion...",
  sign: "Jeg har læst restriktionen – underskriv",
  signing: "Underskriver...",
  back: "Tilbage",
  skip: "Gå videre uden at underskrive",
  skipConfirmHeading: "Gå videre uden at underskrive?",
  // count is 0 when the list could not be loaded, so the number is unknown.
  skipConfirmDescription: (count: number) =>
    count === 0
      ? "Du kan have restriktioner, der venter på din underskrift. De vises igen, næste gang du åbner appen."
      : `Du har stadig ${count === 1 ? "1 restriktion" : `${count} restriktioner`}, der venter på din underskrift. De vises igen, næste gang du åbner appen.`,
  skipConfirmCancel: "Annuller",
  skipConfirm: "Gå videre",
  remaining: (count: number) =>
    count === 1
      ? "1 restriktion venter på din underskrift"
      : `${count} restriktioner venter på din underskrift`,
  signForbidden:
    "Du er ikke den bruger, der skal underskrive denne restriktion.",
  signNotFound: "Underskriftskravet blev ikke fundet. Genindlæs siden.",
  signConflict:
    "Restriktionen er allerede underskrevet eller er blevet erstattet af en nyere version.",
  signFailed: "Underskrivningen mislykkedes. Prøv igen.",
  sessionExpired: "Din session er udløbet. Log ind igen.",
} as const;
