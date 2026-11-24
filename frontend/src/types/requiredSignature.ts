// TYPES / MODELS:
// Matches the backend RequiredSignatureDto: a restriction in force that the
// logged-in user still has to read and sign.
export interface RequiredSignatureDto {
  requiresSignatureId: string;
  // The latest version of the restriction, used to load its current content.
  restrictionId: string;
  title: string;
  reference: string | null;
  startDate: string;
  endDate: string | null;
  requiredSince: string;
}
