export interface CreateRestrictionRequest {
  title: string;
  reference?: string;
  startDate: string; // yyyy-MM-dd (Ikrafttræden)
  endDate: string | null; // nullable (Annulleres)
  situationDescription: string;
  restrictionDescription: string;
  cancellationDescription: string;
}

export interface RestrictionDto extends CreateRestrictionRequest {
  id: string;
}
