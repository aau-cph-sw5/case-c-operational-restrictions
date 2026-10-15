using Backend.Models.Enum;

namespace Backend.Models.Requests;

public record UpdateByIdRequest(
  Guid Id,
  Guid EditedBy,
  RestrictionState? State,
  DateTime? StartDate,
  DateTime? EndDate
);