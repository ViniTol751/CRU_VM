using System.ComponentModel.DataAnnotations;

namespace TesteAPI.Models
{
    public class Relatorio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo 'Obra' é obrigatório.")]
        public string Obra { get; set; } = string.Empty;

        public string ResponsavelFocus { get; set; } = string.Empty;

        public string ResponsavelCliente { get; set; } = string.Empty;

        public int NumeroART { get; set; }

        public DateTime Data { get; set; }

        public string AtividadesExecutadas { get; set; } = string.Empty;

        public string Observacoes { get; set; } = string.Empty;

        public string EquipamentosUtilizados { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo 'Colaboradores Executantes' é obrigatório.")]
        public string ColaboradoresExecutantes { get; set; } = string.Empty;

        public string AcompanhanteTerceiro { get; set; } = string.Empty;

    }
}