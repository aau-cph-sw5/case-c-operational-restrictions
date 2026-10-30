namespace Backend.Models.Requests;

public record CreateRestrictionRequest(
  Guid CreatedBy,
  String RestrictionDescription,
  DateTime StartDate,
  DateTime EndDate
);