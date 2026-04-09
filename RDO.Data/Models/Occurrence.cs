namespace RDO.Data.Models;

public class Occurrence : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }

    // Compatibilidade português
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string Descricao { get => Description; set => Description = value; }
    public string HoraInicio { get => StartTime; set => StartTime = value; }
    public string HoraFim { get => EndTime; set => EndTime = value; }
}
