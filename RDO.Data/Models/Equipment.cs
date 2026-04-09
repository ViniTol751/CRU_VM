namespace RDO.Data.Models;

public class Equipment : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Compatibilidade português
    public string Nome { get => Name; set => Name = value; }
    public string Fabricante { get => Manufacturer; set => Manufacturer = value; }
    public string Modelo { get => Model; set => Model = value; }
    public string NumeroSerie { get => SerialNumber; set => SerialNumber = value; }
    public bool Ativo { get => IsActive; set => IsActive = value; }
}
