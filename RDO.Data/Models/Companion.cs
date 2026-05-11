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

    // Compatibilidade portugu�s
    public string Nome { get => Name; set => Name = value; }
    public string Cargo { get => Role; set => Role = value; }
    public string Grupo { get => Group; set => Group = value; }
    public string Contato { get => Contact; set => Contact = value; }
    public bool Ativo { get => IsActive; set => IsActive = value; }
    public int? EmpresaId { get; set; }
}
