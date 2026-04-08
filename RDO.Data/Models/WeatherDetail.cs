namespace RDO.Data.Models;

public class WeatherDetail : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Period { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    public string Weather { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public double RainfallIndex { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}
