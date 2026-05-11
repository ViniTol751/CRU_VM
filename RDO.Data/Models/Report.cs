using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class Report : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public int? CompanionId { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public string CheckInTime { get; set; } = "08:00";
    public string CheckOutTime { get; set; } = "17:00";
    public string BreakTime { get; set; } = "01:00";
    public string GeneralNotes { get; set; } = string.Empty;
    public string Status { get; set; } = "Filling";
    public bool IsSynced { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDraft { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public int Revisao { get; set; } = 0;
    [JsonIgnore] public Project? Project { get; set; }
    [JsonIgnore] public User? User { get; set; }
    [JsonIgnore] public Companion? Companion { get; set; }
    [JsonIgnore] public ICollection<WeatherDetail> WeatherDetails { get; set; } = new List<WeatherDetail>();
    [JsonIgnore] public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    [JsonIgnore] public ICollection<Occurrence> Occurrences { get; set; } = new List<Occurrence>();
    [JsonIgnore] public ICollection<Material> Materials { get; set; } = new List<Material>();
    [JsonIgnore] public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    [JsonIgnore] public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    [JsonIgnore] public ICollection<ReportEquipment> Equipments { get; set; } = new List<ReportEquipment>();
    [JsonIgnore] public ICollection<ReportCompanion> ReportCompanions { get; set; } = new List<ReportCompanion>();
    public int ObraId { get => ProjectId; set => ProjectId = value; }
    public int UsuarioId { get => UserId; set => UserId = value; }
    public int? AcompanhanteId { get => CompanionId; set => CompanionId = value; }
    public int Numero { get => Number; set => Number = value; }
    public DateTime Data { get => Date; set => Date = value; }
    public string HoraEntrada { get => CheckInTime; set => CheckInTime = value; }
    public string HoraSaida { get => CheckOutTime; set => CheckOutTime = value; }
    public string HoraIntervalo { get => BreakTime; set => BreakTime = value; }
    public string ObsGerais { get => GeneralNotes; set => GeneralNotes = value; }
    public bool Sincronizado { get => IsSynced; set => IsSynced = value; }
    public DateTime CriadoEm { get => CreatedAt; set => CreatedAt = value; }
    public bool Rascunho { get => IsDraft; set => IsDraft = value; }
    [JsonIgnore] public Project? Obra { get => Project; set => Project = value; }
    [JsonIgnore] public User? Usuario { get => User; set => User = value; }
    [JsonIgnore] public Companion? Acompanhante { get => Companion; set => Companion = value; }
    [JsonIgnore] public ICollection<WeatherDetail> Climas { get => WeatherDetails; set => WeatherDetails = value; }
    [JsonIgnore] public ICollection<Activity> Atividades { get => Activities; set => Activities = value; }
    [JsonIgnore] public ICollection<Occurrence> Ocorrencias { get => Occurrences; set => Occurrences = value; }
    [JsonIgnore] public ICollection<Material> Materiais { get => Materials; set => Materials = value; }
    [JsonIgnore] public ICollection<Photo> Fotos { get => Photos; set => Photos = value; }
    [JsonIgnore] public ICollection<Signature> Assinaturas { get => Signatures; set => Signatures = value; }
    [JsonIgnore] public ICollection<ReportEquipment> Equipamentos { get => Equipments; set => Equipments = value; }
    [JsonIgnore] public ICollection<ReportCompanion> RelatorioAcompanhantes { get => ReportCompanions; set => ReportCompanions = value; }
}
