namespace TesteAPI.Models;

public class ReportCompanion : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int CompanionId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
    public Companion? Companion { get; set; }
}