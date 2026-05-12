using System.ComponentModel.DataAnnotations.Schema;
namespace RDO.Data.Models;

public class Companion : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public int? EmpresaId { get; set; }

    // Compatibilidade português
    [NotMapped] public string Nome { get => Name; set => Name = value; }
    [NotMapped] public string Cargo { get => Role; set => Role = value; }
    [NotMapped] public string Grupo { get => Group; set => Group = value; }
    [NotMapped] public string Contato { get => Contact; set => Contact = value; }
    [NotMapped] public bool Ativo { get => IsActive; set => IsActive = value; }
}
