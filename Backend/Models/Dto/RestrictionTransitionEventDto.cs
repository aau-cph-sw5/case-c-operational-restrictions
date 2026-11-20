namespace Backend.Models.DTOs;

public sealed class RestrictionTransitionEventDto
{
    public string Action { get; init; } = string.Empty;
    public string? FromState { get; init; }
    public string ToState { get; init; } = string.Empty;
    public Guid ActorId { get; init; }
    public DateTime OccurredAt { get; init; }
}
