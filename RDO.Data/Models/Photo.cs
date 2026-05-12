using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
    public string Type { get; set; } = "photo";   // "photo" | "document"
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public Report? Report { get; set; }
    [NotMapped] public int RelatorioId { get => ReportId; set => ReportId = value; }
    [NotMapped] public string CaminhoArquivo { get => FilePath; set => FilePath = value; }
    [NotMapped] public string Legenda { get => Caption; set => Caption = value; }
    [NotMapped] public string AtividadeRelacionada { get => RelatedActivity; set => RelatedActivity = value; }
    [NotMapped] public DateTime TiradaEm { get => TakenAt; set => TakenAt = value; }
}
