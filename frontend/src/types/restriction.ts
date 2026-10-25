export interface CreateRestrictionRequest {
  title: string;
  description: string;
  location: string;
  startDate: string; // yyyy-MM-dd
  endDate: string | null; // yyyy-MM-dd
  notes?: string;
}

export interface RestrictionDto extends CreateRestrictionRequest {
  id: string;
}
