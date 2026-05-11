namespace TesteAPI.Models;

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
    public int Revisao { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Project? Project { get; set; }
    public User? User { get; set; }
    public Companion? Companion { get; set; }
    public ICollection<WeatherDetail> WeatherDetails { get; set; } = new List<WeatherDetail>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<Occurrence> Occurrences { get; set; } = new List<Occurrence>();
    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
    public ICollection<ReportEquipment> Equipments { get; set; } = new List<ReportEquipment>();
    public ICollection<ReportCompanion> ReportCompanions { get; set; } = new List<ReportCompanion>();
}