namespace RDO.Data.Models;

public class ReportEquipment : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int EquipmentId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Report? Report { get; set; }
    public Equipment? Equipment { get; set; }

    // Compatibilidade português
    public int RelatorioId { get => ReportId; set => ReportId = value; }
    public int EquipamentoCadastradoId { get => EquipmentId; set => EquipmentId = value; }
    public Equipment? EquipamentoCadastrado { get => Equipment; set => Equipment = value; }
}
