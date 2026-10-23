using Backend.Models.Enums;
namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Represents database tables managed and mapped by EF Core to PostgreSQL
public class File
{
    public Guid FileId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public Guid RestrictionId { get; set; } = Guid.Empty;

    public DateTime CreatedAt { get; set; }= DateTime.Now;

}
