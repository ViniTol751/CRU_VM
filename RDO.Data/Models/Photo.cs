namespace RDO.Data.Models;

public class Photo : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string RelatedActivity { get; set; } = string.Empty;
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}
