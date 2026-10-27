using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateRestrictionRequest(
    Guid CreatedBy = default,
    [Required(ErrorMessage = "Start date is required.")]
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Reference = null,
    [Required(ErrorMessage = "Situation description is required.", AllowEmptyStrings = false)]
    string SituationDescription = "",
    [Required(ErrorMessage = "Restriction description is required.", AllowEmptyStrings = false)]
    string RestrictionDescription = "",
    [Required(ErrorMessage = "Cancellation description is required.", AllowEmptyStrings = false)]
    string CancellationDescription = ""
);
