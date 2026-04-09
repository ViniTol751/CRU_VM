namespace RDO.Data.Models;

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

    // Compatibilidade português
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string NomeFuncionario { get => EmployeeName; set => EmployeeName = value; }
    public string Funcao { get => JobTitle; set => JobTitle = value; }
    public int HorasTrabalhadas { get => HoursWorked; set => HoursWorked = value; }
}
