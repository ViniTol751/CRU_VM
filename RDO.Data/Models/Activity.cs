namespace RDO.Data.Models;

public class Activity : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "In Progress";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }

    // Compatibilidade português
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string Descricao { get => Description; set => Description = value; }
    public string Local { get => Location; set => Location = value; }
}
