namespace RDO.Data.Models;

public class Companion : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
}
