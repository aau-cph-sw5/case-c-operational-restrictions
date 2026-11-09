using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateRestrictionRequest(
    Guid CreatedBy = default,
    [Required(ErrorMessage = "Title is required.", AllowEmptyStrings = false)]
    [StringLength(100, ErrorMessage = "Title must not exceed 100 characters.")]
    string Title = "",
    [StringLength(100, ErrorMessage = "Reference must not exceed 100 characters.")]
    string? Reference = null,
    [Required(ErrorMessage = "Start date is required.")]
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    [Required(ErrorMessage = "Situation description is required.", AllowEmptyStrings = false)]
    [StringLength(500, ErrorMessage = "Situation description must not exceed 500 characters.")]
    string SituationDescription = "",
    [Required(ErrorMessage = "Restriction description is required.", AllowEmptyStrings = false)]
    [StringLength(500, ErrorMessage = "Restriction description must not exceed 500 characters.")]
    string RestrictionDescription = "",
    [Required(ErrorMessage = "Cancellation description is required.", AllowEmptyStrings = false)]
    [StringLength(500, ErrorMessage = "Cancellation description must not exceed 500 characters.")]
    string CancellationDescription = ""
);
