namespace TesteAPI.Models;

public class ProjectMember : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Technician";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Project? Project { get; set; }
    public User? User { get; set; }
}