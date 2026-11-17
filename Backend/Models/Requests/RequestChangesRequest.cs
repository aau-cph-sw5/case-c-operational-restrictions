using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record RequestChangesRequest(
    [Required(ErrorMessage = "A review comment is required.", AllowEmptyStrings = false)]
    string Comment = "");
