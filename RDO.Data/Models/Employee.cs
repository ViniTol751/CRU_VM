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
    public string Nome { get => Name; set => Name = value; }
    public string Funcao { get => JobTitle; set => JobTitle = value; }
    public string Empresa { get => Company; set => Company = value; }
    public string Tipo { get => Type; set => Type = value; }
    public string Contato { get => Contact; set => Contact = value; }
    public bool Ativo { get => IsActive; set => IsActive = value; }
}
