using RDO.App.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RDO.Data.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;

namespace RDO.App.Views
{
    public sealed partial class PerfilPage : Page
    {
        public PerfilPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            CarregarPerfil();
        }

        private void CarregarPerfil()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            // Usa sempre o usuário logado na sessão atual
            int? usuarioId = null;
            if (LocalSettingsService.ContainsKey("UsuarioId"))
                usuarioId = LocalSettingsService.Get<int?>("UsuarioId");

            string nomeUsuario;
            if (usuarioId.HasValue)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value);
                nomeUsuario = usuario?.Nome ?? LocalSettingsService.Get<string>("NomeUsuario") ?? "Usuário";
            }
            else
            {
                nomeUsuario = LocalSettingsService.Get<string>("NomeUsuario") ?? "Usuário";
            }

            NomeCompletoTexto.Text = nomeUsuario;

            // Tenta encontrar funcionário — primeiro por nome exato, depois por primeiro+último nome
            var func = db.Funcionarios.FirstOrDefault(f => f.Nome == nomeUsuario);
            if (func == null)
            {
                var partesNome = nomeUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partesNome.Length >= 2)
                {
                    var primeiro = partesNome[0];
                    var ultimo   = partesNome[^1];
                    func = db.Funcionarios.AsEnumerable().FirstOrDefault(f =>
                        f.Nome.Contains(primeiro, StringComparison.OrdinalIgnoreCase) &&
                        f.Nome.Contains(ultimo,   StringComparison.OrdinalIgnoreCase));
                }
            }
            FuncaoTexto.Text = func?.Funcao ?? "—";
            EmpresaTexto.Text = func?.Empresa ?? "Focus Engenharia Elétrica";

            var partes = nomeUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            IniciaisTexto.Text = partes.Length >= 2
                ? $"{partes[0][0]}{partes[^1][0]}".ToUpper()
                : nomeUsuario.Length >= 2 ? nomeUsuario[..2].ToUpper() : "U";

            // Estatísticas — apenas relatórios de obras ativas e não excluídas
            var agora = DateTime.Now;

            var obrasVisiveis = db.Obras
                .Where(o => o.IsActive && !o.IsDeleted)
                .Select(o => o.Id)
                .ToHashSet();

            // Relatórios onde o usuário aparece como participante (equipe/assinaturas)
            var relatorioIdsParticipante = func != null
                ? new HashSet<int>(db.Assinaturas
                    .Where(a => a.FuncionarioId == func.Id)
                    .Select(a => a.RelatorioId))
                : new HashSet<int>();

            var uid = usuarioId ?? 0;
            var relatorios = db.Relatorios
                .Where(r => !r.Rascunho && !r.IsDeleted &&
                            (r.UsuarioId == uid ||
                             (uid > 0 && r.UsuarioId == 0) ||
                             relatorioIdsParticipante.Contains(r.Id)))
                .ToList()
                .Where(r => obrasVisiveis.Contains(r.ObraId))
                .ToList();

            var relatorioIds = relatorios.Select(r => r.Id).ToHashSet();

            TotalRelatoriosTexto.Text = relatorios.Count.ToString();

            RelatoriosMesTexto.Text = relatorios
                .Count(r => r.Data.Month == agora.Month && r.Data.Year == agora.Year)
                .ToString();

            // Horas: usa as assinaturas individuais do funcionário (por turno real)
            // Fallback: campos padrão do relatório se não houver assinatura vinculada
            double totalHoras;
            if (func != null)
            {
                var assinaturas = db.Assinaturas
                    .Where(a => a.FuncionarioId == func.Id && relatorioIds.Contains(a.RelatorioId))
                    .ToList();
                totalHoras = assinaturas.Count > 0
                    ? assinaturas.Sum(a => CalcularHorasTrabalhadas(a.HoraEntrada, a.HoraSaida, a.HoraIntervalo))
                    : relatorios.Sum(r => CalcularHorasTrabalhadas(r.HoraEntrada, r.HoraSaida, r.HoraIntervalo));
            }
            else
            {
                totalHoras = relatorios.Sum(r => CalcularHorasTrabalhadas(r.HoraEntrada, r.HoraSaida, r.HoraIntervalo));
            }
            HorasTexto.Text = $"{(int)totalHoras}h";

            // Dias em campo = datas distintas com relatório
            DiasTexto.Text = relatorios
                .Select(r => r.Data.Date)
                .Distinct()
                .Count()
                .ToString();

            // Obras participadas — já filtradas pelo obrasVisiveis acima
            var obrasIds = relatorios
                .Select(r => r.ObraId)
                .Distinct()
                .ToList();
            ObrasTexto.Text = obrasIds.Count.ToString();

            // Último relatório
            var ultimoRel = relatorios
                .OrderByDescending(r => r.Data)
                .FirstOrDefault();
            UltimoRelTexto.Text = ultimoRel != null
                ? ultimoRel.Data.ToString("dd/MM/yy")
                : "—";

            // Últimas 3 obras com relatórios (ativas e não excluídas)
            var obras = db.Obras
                .Where(o => obrasIds.Contains(o.Id) && o.IsActive && !o.IsDeleted)
                .OrderByDescending(o => o.UpdatedAt)
                .Take(3)
                .ToList();
            ObrasRecentesList.ItemsSource = obras;
        }

        private static double CalcularHorasTrabalhadas(string entrada, string saida, string intervalo)
        {
            static double Parse(string t)
                => TimeSpan.TryParse(t, out var ts) ? ts.TotalHours : 0;
            return Math.Max(0, Parse(saida) - Parse(entrada) - Parse(intervalo));
        }

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}
