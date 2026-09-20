namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
public class UserDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
