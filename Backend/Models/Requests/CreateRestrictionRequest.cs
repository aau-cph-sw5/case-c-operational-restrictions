namespace Backend.Models.Requests;

public record CreateRestrictionRequest(
  Guid CreatedBy = default,
  DateTime StartDate = default,
  DateTime? EndDate = null,
  string? Reference = null,
  string SituationDescription = "",
  string RestrictionDescription = "",
  string CancellationDescription = ""
);
