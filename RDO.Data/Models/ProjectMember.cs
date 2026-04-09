namespace RDO.Data.Models;

public class ProjectMember : ILocalSyncEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Technician";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public Project? Project { get; set; }
    public User? User { get; set; }

    // Compatibilidade português
    public int ObraId { get => ProjectId; set => ProjectId = value; }
    public int UsuarioId { get => UserId; set => UserId = value; }
    public string Papel { get => Role; set => Role = value; }
}
