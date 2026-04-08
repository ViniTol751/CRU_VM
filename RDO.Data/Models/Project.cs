namespace RDO.Data.Models;

public class Project : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ART { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Status { get; set; } = "In Progress";
    public string Manager { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public ICollection<Report> Reports { get; set; } = new List<Report>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
