using System.ComponentModel.DataAnnotations.Schema;
namespace RDO.Data.Models;

public class Employee : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Type { get; set; } = "Own";
    public string Contact { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Compatibilidade português
    [NotMapped] public string Nome { get => Name; set => Name = value; }
    [NotMapped] public string Funcao { get => JobTitle; set => JobTitle = value; }
    [NotMapped] public string Empresa { get => Company; set => Company = value; }
    [NotMapped] public string Tipo { get => Type; set => Type = value; }
    [NotMapped] public string Contato { get => Contact; set => Contact = value; }
    [NotMapped] public bool Ativo { get => IsActive; set => IsActive = value; }
}
