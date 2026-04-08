namespace TesteAPI.Models;

public class Material : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Type { get; set; } = "Received";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}