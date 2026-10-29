using Backend.Models.Enums;

namespace Backend.Models.Requests;

public record UpdateByIdRequest(
  Guid Id,
  Guid EditedBy,
  RestrictionState? Status,
  DateTime? StartDate,
  DateTime? EndDate
);
