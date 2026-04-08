namespace TesteAPI.Models;

public class Signature : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? SignedAt { get; set; }
    public bool IsSigned { get; set; } = false;
    public int? EmployeeId { get; set; }
    public string CheckInTime { get; set; } = "08:00";
    public string CheckOutTime { get; set; } = "17:00";
    public string BreakTime { get; set; } = "01:00";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}