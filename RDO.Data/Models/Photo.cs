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

    // Compatibilidade português
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string CaminhoArquivo { get => FilePath; set => FilePath = value; }
    public string Legenda { get => Caption; set => Caption = value; }
    public string AtividadeRelacionada { get => RelatedActivity; set => RelatedActivity = value; }
    public DateTime TiradaEm { get => TakenAt; set => TakenAt = value; }
}
