namespace TesteAPI.Models;

public class EquipmentUsage : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public int HoursUsed { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}