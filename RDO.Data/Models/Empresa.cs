using System.ComponentModel.DataAnnotations.Schema;

namespace RDO.Data.Models;

public class Empresa : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? ImagemPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    [NotMapped] public bool Ativo { get => IsActive; set => IsActive = value; }
    [NotMapped] public string? LogoUrl { get; set; }
}

