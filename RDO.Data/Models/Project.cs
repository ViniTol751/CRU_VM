using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
namespace RDO.Data.Models;

public class Project : ILocalSyncEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ART { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Status { get; set; } = "In Progress";
    public string Manager { get; set; } = string.Empty;
    public string Crea { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string ClientManager { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? ExpectedEndDate { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? EmpresaId { get; set; }
    public bool IsDeleted { get; set; } = false;
    [JsonIgnore] public Empresa? Empresa { get; set; }
    [JsonIgnore] public ICollection<Report> Reports { get; set; } = new List<Report>();
    [JsonIgnore] public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    [NotMapped] public string Nome { get => Name; set => Name = value; }
    [NotMapped] public string Endereco { get => Address; set => Address = value; }
    [NotMapped] public string Grupo { get => Group; set => Group = value; }
    [NotMapped] public string Responsavel { get => Manager; set => Manager = value; }
    [NotMapped] public string ResponsavelCliente { get => ClientManager; set => ClientManager = value; }
    [NotMapped] public string TipoContrato { get => ContractType; set => ContractType = value; }
    [NotMapped] public string Contratante { get => Client; set => Client = value; }
    [NotMapped] public DateTime DataInicio { get => StartDate; set => StartDate = value; }
    [NotMapped] public DateTime? PrevisaoTermino { get => ExpectedEndDate; set => ExpectedEndDate = value; }
    [NotMapped] public string? ImagemPath { get => ImagePath; set => ImagePath = value; }
    [NotMapped] public bool Ativo { get => IsActive; set => IsActive = value; }
}
