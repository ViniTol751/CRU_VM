using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class ReportCompanion : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int CompanionId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public Report? Report { get; set; }
    public Companion? Companion { get; set; }

    // Compatibilidade português
    [NotMapped] public int RelatorioId { get => ReportId; set => ReportId = value; }
    [NotMapped] public int AcompanhanteId { get => CompanionId; set => CompanionId = value; }
    [NotMapped] public Companion? Acompanhante { get => Companion; set => Companion = value; }
}
