using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class User : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Profile { get; set; } = "Technician";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public ICollection<ProjectMember> Projects { get; set; } = new List<ProjectMember>();
    [JsonIgnore] public ICollection<Report> Reports { get; set; } = new List<Report>();

    // Compatibilidade português
    public string Nome { get => Name; set => Name = value; }
    public string SenhaHash { get => PasswordHash; set => PasswordHash = value; }
    public string Perfil { get => Profile; set => Profile = value; }
    public bool Ativo { get => IsActive; set => IsActive = value; }
}
