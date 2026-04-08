namespace RDO.Data.Models;

public class User : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Profile { get; set; } = "Technician";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public ICollection<ProjectMember> Projects { get; set; } = new List<ProjectMember>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
