namespace TesteAPI.Models;

public class EmployeePresence : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public int HoursWorked { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
}