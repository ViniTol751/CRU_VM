using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class Material : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Type { get; set; } = "Received";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public Report? Report { get; set; }
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public string Nome { get => Name; set => Name = value; }
    public string Quantidade { get => Quantity; set => Quantity = value; }
    public string Unidade { get => Unit; set => Unit = value; }
    public string Tipo { get => Type; set => Type = value; }
}
