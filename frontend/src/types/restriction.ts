export interface CreateRestrictionRequest {
  startDate: string; // yyyy-MM-dd (Ikrafttræden)
  endDate: string | null; // nullable (Annulleres)
  reference?: string;
  situationDescription: string;
  restrictionDescription: string;
  cancellationDescription: string;
}

export interface RestrictionDto extends CreateRestrictionRequest {
  id: string;
}
