using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class Activity : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "In Progress";
    public int? ParentId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public Report? Report { get; set; }
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string Descricao { get => Description; set => Description = value; }
    public string Local { get => Location; set => Location = value; }
}
