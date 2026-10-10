namespace Backend.Models.Requests;

public record CreateRestrictionRequest(
  Guid CreatedBy,
  DateTime StartDate,
  DateTime EndDate
);
