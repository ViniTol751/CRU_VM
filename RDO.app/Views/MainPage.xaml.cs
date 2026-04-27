using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RDO.app.Services;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

namespace RDO.App.Views
{
    public class ObraViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string Endereco { get; set; } = "";
        public string Status { get; set; } = "";
        public string Responsavel { get; set; } = "";
        public string Contratante { get; set; } = "";
        public string ResponsavelCliente { get; set; } = "";
        public string ART { get; set; } = "";
        public DateTime DataInicio { get; set; }
        public DateTime? PrevisaoTermino { get; set; }
        public string? ImagemPath { get; set; }
        public string? EmpresaLogoUrl { get; set; }
        public bool HasEmpresaLogo => !string.IsNullOrEmpty(EmpresaLogoUrl);
        public int QtdRdos { get; set; }
        public bool TemRascunho { get; set; }

        public Microsoft.UI.Xaml.Media.Brush StatusBackground => Status switch
        {
            "Em execução" => new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 0, 140, 90)),
            "Concluída" => new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 30, 80, 160)),
            "Paralisada" => new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 180, 40, 40)),
            _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 30, 45, 74))
        };
    }

    public class GrupoObraViewModel
    {
        public string Grupo { get; set; } = "";
        public List<ObraViewModel> Obras { get; set; } = new();
        public bool TemObras => Obras.Count > 0;
        public bool SemObras => Obras.Count == 0;
    }

    public class MeuRelatorioViewModel
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNome { get; set; } = "";
        public string ObraGrupo { get; set; } = "";
        public string ObraStatus { get; set; } = "";
        public string Responsavel { get; set; } = "";
        public string DataFormatada { get; set; } = "";
        public string NumeroFormatado { get; set; } = "";
        public string Status { get; set; } = "";
        public string FuncionariosPresentes { get; set; } = "";
        public List<string> FuncionariosNomes { get; set; } = new();
        public int Revisao { get; set; } = 0;
        public string RevisaoFormatada => $"Rev. {Revisao:D2}";
    }

    public class RascunhoViewModel
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNome { get; set; } = "";
        public string ObraGrupo { get; set; } = "";
        public string NumeroFormatado { get; set; } = "";
        public string DataRelatorio { get; set; } = "";
        public string SalvoEm { get; set; } = "";
        public string CriadoPor { get; set; } = "";
        public DateTime CriadoEmDate { get; set; }
        // Campos para filtragem
        public string ObraNomeFiltro { get; set; } = "";
        public string CriadoPorFiltro { get; set; } = "";
    }

    public sealed partial class MainPage : Page
    {
        internal static bool ShowMeusRelatoriosOnNavigate { get; set; }

        private bool _apenasMinhas = false;
        private bool _mostraMeusRelatorios = false;
        private bool _mostraRascunhos = false;
        private bool _inicializandoFiltros = false;
        private List<ObraViewModel> _todasObras = new();
        private List<MeuRelatorioViewModel> _todosRelatorios = new();
        private List<RascunhoViewModel> _todosRascunhos = new();
        private ContentDialog? _dialogPropriedadesObra;

        // URL da API — altere para o endereço do servidor quando implantado
        private const string ApiUrl = "http://localhost:5043";
        private readonly SyncService _syncService = new SyncService(ApiUrl);

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShowMeusRelatoriosOnNavigate)
            {
                ShowMeusRelatoriosOnNavigate = false;
                _mostraMeusRelatorios = true;
                _mostraRascunhos = false;
                CarregarMeusRelatorios();
            }
            else
            {
                _mostraMeusRelatorios = false;
                _mostraRascunhos = false;
                CarregarObras();
            }
            AtualizarMenuAtivo();
            AtualizarIconeTema();
            CarregarNomeVinculado();

            // Sync automático ao carregar a página (não bloqueia a UI)
            _ = SincronizarAsync();
        }

        private async Task SincronizarAsync()
        {
            AtualizarSyncUI(SyncEstado.Sincronizando);
            BtnSync.IsEnabled = false;

            var resultado = await _syncService.SyncAsync();

            BtnSync.IsEnabled = true;

            if (resultado.IsOffline)
            {
                AtualizarSyncUI(SyncEstado.SemRede);
                return;
            }

            if (resultado.Success)
            {
                AtualizarSyncUI(SyncEstado.Sincronizado,
                    $"↑{resultado.PushedInserted + resultado.PushedUpdated}  ↓{resultado.PulledRecords}");
                // Recarrega dados locais após pull
                if (_mostraMeusRelatorios) CarregarMeusRelatorios();
                else CarregarObras();
            }
            else
            {
                var detalheErro = string.IsNullOrWhiteSpace(resultado.ErrorCode)
                    ? (resultado.Error ?? "Erro")
                    : $"{resultado.ErrorCode}: {resultado.Error}";
                AtualizarSyncUI(SyncEstado.Erro, detalheErro);
            }
        }

        private enum SyncEstado { Sincronizando, Sincronizado, SemRede, Erro }

        private void AtualizarSyncUI(SyncEstado estado, string detalhe = "")
        {
            var (glyph, texto, bgHex, fgHex) = estado switch
            {
                SyncEstado.Sincronizando => ("\uE895", "Sincronizando...",
                    "#1A3050", "#6AB0FF"),
                SyncEstado.Sincronizado  => ("\uE73E", string.IsNullOrEmpty(detalhe)
                    ? "Sincronizado" : $"Sincronizado  {detalhe}",
                    "#0A2A14", "#00D264"),
                SyncEstado.SemRede       => ("\uE774", "Sem rede",
                    "#2A1A00", "#F0BE00"),
                SyncEstado.Erro          => ("\uE783", string.IsNullOrEmpty(detalhe)
                    ? "Erro de sync" : $"Erro: {detalhe}",
                    "#2A0A0A", "#E65050"),
                _                        => ("\uE895", "", "#333", "#888")
            };

            SyncStatusIcon.Glyph = glyph;
            SyncStatusTexto.Text = texto;

            // Converte hex simples para Color
            static Windows.UI.Color HexToColor(string hex)
            {
                hex = hex.TrimStart('#');
                return Windows.UI.Color.FromArgb(255,
                    Convert.ToByte(hex[0..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16));
            }

            SyncStatusBadge.Background = new SolidColorBrush(HexToColor(bgHex));
            SyncStatusIcon.Foreground  = new SolidColorBrush(HexToColor(fgHex));
            SyncStatusTexto.Foreground = new SolidColorBrush(HexToColor(fgHex));

            var erroVisivel = estado == SyncEstado.Erro ? Visibility.Visible : Visibility.Collapsed;
            BtnVerLogs.Visibility   = erroVisivel;
            BtnGuiaErros.Visibility = erroVisivel;
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
            => await SincronizarAsync();

        private async void BtnVerLogs_Click(object sender, RoutedEventArgs e)
        {
            var logDir = SyncLogger.GetLogDirectory();
            try
            {
                System.IO.Directory.CreateDirectory(logDir);
                await Windows.System.Launcher.LaunchFolderPathAsync(logDir);
            }
            catch (Exception ex)
            {
                var d = new ContentDialog
                {
                    Title = "Não foi possível abrir a pasta",
                    Content = $"Caminho: {logDir}\n\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await d.ShowAsync();
            }
        }

        private async void BtnGuiaErros_Click(object sender, RoutedEventArgs e)
        {
            var logDir = SyncLogger.GetLogDirectory();

            var secoes = new[]
            {
                ("Sem rede / Offline", "\uE774",
                 "O aplicativo não detectou conexão com a internet ou a rede local.",
                 new[]
                 {
                     "Verifique se o cabo ou Wi-Fi está conectado.",
                     "Teste abrir uma página no navegador.",
                     "Se estiver em VPN, confirme que o servidor está acessível."
                 }),
                ("API fora do ar (conexão recusada)", "\uE783",
                 "O aplicativo conseguiu chegar à rede, mas a API não respondeu.",
                 new[]
                 {
                     "Verifique se a API está rodando: abra o terminal e execute  dotnet run  na pasta da API.",
                     "Confirme a URL configurada no aplicativo (padrão: http://localhost:5043).",
                     "Se usar Docker, verifique se o container está ativo: docker ps."
                 }),
                ("Erro HTTP 500 (servidor com falha)", "\uE814",
                 "A API recebeu a requisição mas encontrou um erro interno — geralmente no banco de dados.",
                 new[]
                 {
                     "Verifique se o PostgreSQL está saudável: docker ps ou serviço do Windows.",
                     "Consulte os logs da API para a exceção detalhada.",
                     "Execute as migrations se necessário: dotnet ef database update."
                 }),
                ("Erro SYNC-PULL-UPSERT (banco local)", "\uE8F1",
                 "Falha ao salvar dados recebidos no banco SQLite local.",
                 new[]
                 {
                     "O banco de dados local pode estar desatualizado.",
                     "Feche o aplicativo e execute as migrations do SQLite.",
                     $"Consulte os logs em: {logDir}"
                 }),
                ("Timeout / Gateway 504", "\uE916",
                 "O servidor demorou muito para responder.",
                 new[]
                 {
                     "O PostgreSQL pode estar lento ou travado — verifique os logs do banco.",
                     "Tente sincronizar novamente em alguns instantes.",
                     "Se persistir, reinicie os containers Docker."
                 }),
            };

            var root = new StackPanel { Spacing = 20, Width = 560 };

            root.Children.Add(new TextBlock
            {
                Text = "Guia de Erros de Sincronização",
                FontSize = 16,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"]
            });

            foreach (var (titulo, glyph, descricao, passos) in secoes)
            {
                var card = new Border
                {
                    Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                    BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 14)
                };

                var cardContent = new StackPanel { Spacing = 8 };

                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                headerRow.Children.Add(new FontIcon
                {
                    Glyph = glyph,
                    FontSize = 14,
                    Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerRow.Children.Add(new TextBlock
                {
                    Text = titulo,
                    FontSize = 13,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center
                });
                cardContent.Children.Add(headerRow);

                cardContent.Children.Add(new TextBlock
                {
                    Text = descricao,
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    TextWrapping = TextWrapping.Wrap
                });

                var passosList = new StackPanel { Spacing = 4, Margin = new Thickness(4, 0, 0, 0) };
                foreach (var passo in passos)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    row.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 12,
                        Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 1, 0, 0)
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = passo,
                        FontSize = 12,
                        Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 490
                    });
                    passosList.Children.Add(row);
                }
                cardContent.Children.Add(passosList);
                card.Child = cardContent;
                root.Children.Add(card);
            }

            // Botão abrir logs no rodapé
            var btnAbrirLogs = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE8B7", FontSize = 13 },
                        new TextBlock { Text = $"Abrir pasta de logs", FontSize = 13, VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1)
            };
            btnAbrirLogs.Click += async (s, ev) =>
            {
                System.IO.Directory.CreateDirectory(logDir);
                await Windows.System.Launcher.LaunchFolderPathAsync(logDir);
            };
            root.Children.Add(btnAbrirLogs);

            var scroll = new ScrollViewer
            {
                Content = root,
                MaxHeight = 580,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var dialog = new ContentDialog
            {
                Title = "Resolução de Erros",
                Content = scroll,
                CloseButtonText = "Fechar",
                XamlRoot = this.XamlRoot
            };
            dialog.Resources["ContentDialogMaxWidth"] = 640.0;

            await dialog.ShowAsync();
        }

        private void CarregarNomeVinculado()
        {
            // Prioridade 1: funcionário vinculado
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("FuncionarioVinculadoId"))
            {
                var funcId = (int?)ApplicationData.Current.LocalSettings.Values["FuncionarioVinculadoId"];
                if (funcId != null)
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var func = db.Funcionarios.Find(funcId.Value);
                    if (func != null)
                    {
                        NomeUsuarioTexto.Text = AbreviarNome(func.Nome);
                        PerfilUsuarioTexto.Text = func.Funcao ?? "—";
                        return;
                    }
                }
            }
            // Prioridade 2: nome do login
            var nomeLogin = ApplicationData.Current.LocalSettings.Values["NomeUsuario"]?.ToString();
            if (!string.IsNullOrEmpty(nomeLogin))
            {
                NomeUsuarioTexto.Text = AbreviarNome(nomeLogin);
                PerfilUsuarioTexto.Text = "—";
            }
        }

        private async void CarregarObras()
        {
            List<RDO.Data.Models.Project> obras;
            Dictionary<string, RDO.Data.Models.Empresa> empPorGrupo;
            Dictionary<int, int> rdoCounts;
            Dictionary<int, bool> rascunhoCounts;

            // Todas as queries síncronas feitas dentro do using antes de qualquer await
            using (var db = new RdoDbContext(DbContextHelper.GetOptions()))
            {
                obras = db.Obras.Where(o => o.IsActive).ToList();

                var empresas = db.Empresas.Where(e => e.IsActive).ToList();
                empPorGrupo = new Dictionary<string, RDO.Data.Models.Empresa>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in empresas)
                {
                    var key = RDO.App.Services.LogoService.GetBaseNome(e.Nome);
                    if (!empPorGrupo.ContainsKey(key)) empPorGrupo[key] = e;
                }

                var obraIds = obras.Select(o => o.Id).ToList();
                rdoCounts = db.Relatorios
                    .Where(r => !r.Rascunho && obraIds.Contains(r.ObraId))
                    .GroupBy(r => r.ObraId)
                    .ToDictionary(g => g.Key, g => g.Count());
                rascunhoCounts = db.Relatorios
                    .Where(r => r.Rascunho && obraIds.Contains(r.ObraId))
                    .GroupBy(r => r.ObraId)
                    .ToDictionary(g => g.Key, g => true);
            }

            var cfg = RDO.App.Services.LogosConfig.Load();
            var nasFiles = await RDO.App.Services.LogoService.GetNasFilesAsync(cfg);

            // Resolve logos sem concorrência por arquivo (deduplicado por rawUrl)
            var rawUrlByObra = new Dictionary<int, string?>();
            foreach (var o in obras)
            {
                RDO.Data.Models.Empresa? emp = null;
                if (!string.IsNullOrEmpty(o.Grupo)) empPorGrupo.TryGetValue(o.Grupo, out emp);
                rawUrlByObra[o.Id] = emp != null
                    ? RDO.App.Services.LogoService.ResolveLogoUrlFast(cfg, emp.ImagemPath, emp.Nome, nasFiles)
                    : null;
            }

            // Uma Task por rawUrl único → evita criar o mesmo arquivo de cache simultaneamente
            var uniqueRawUrls = rawUrlByObra.Values
                .Where(u => u != null).Select(u => u!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var flattenTasks = uniqueRawUrls.ToDictionary(
                u => u,
                u => RDO.App.Services.LogoService.FlattenToWhiteAsync(u),
                StringComparer.OrdinalIgnoreCase);
            await Task.WhenAll(flattenTasks.Values);

            var logoDict = new Dictionary<int, string?>();
            foreach (var o in obras)
            {
                var rawUrl = rawUrlByObra.GetValueOrDefault(o.Id);
                logoDict[o.Id] = rawUrl != null && flattenTasks.TryGetValue(rawUrl, out var t)
                    ? t.Result
                    : null;
            }

            _todasObras = obras.Select(o => new ObraViewModel
            {
                Id = o.Id,
                Nome = o.Nome,
                Grupo = o.Grupo,
                Endereco = o.Endereco,
                Status = o.Status,
                Responsavel = o.Responsavel,
                Contratante = o.Contratante,
                ResponsavelCliente = o.ResponsavelCliente,
                ART = o.ART,
                DataInicio = o.DataInicio,
                PrevisaoTermino = o.PrevisaoTermino,
                ImagemPath = o.ImagemPath,
                EmpresaLogoUrl = logoDict.GetValueOrDefault(o.Id),
                QtdRdos = rdoCounts.GetValueOrDefault(o.Id, 0),
                TemRascunho = rascunhoCounts.ContainsKey(o.Id)
            }).ToList();

            AplicarFiltros();
        }

        private void CarregarMeusRelatorios()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            // Apenas publicados E cuja obra está ativa (não excluída nem desativada)
            var obraIds = db.Obras
                .Where(o => o.IsActive && !o.IsDeleted)
                .Select(o => o.Id)
                .ToHashSet();

            var relatorios = db.Relatorios
                .Where(r => !r.Rascunho && r.Status != "Rascunho")
                .OrderByDescending(r => r.Data)
                .ToList()
                .Where(r => obraIds.Contains(r.ObraId))
                .ToList();

            // Carrega presenças em lote para evitar N+1
            var relIds = relatorios.Select(r => r.Id).ToList();
            var presencasPorRel = db.PresencasFuncionarios
                .Where(p => relIds.Contains(p.ReportId))
                .GroupBy(p => p.ReportId)
                .ToDictionary(g => g.Key, g => g.Select(p => p.EmployeeName).Distinct().ToList());

            _todosRelatorios = relatorios.Select(r =>
            {
                var obra = db.Obras.Find(r.ObraId);
                var funcNomes = presencasPorRel.TryGetValue(r.Id, out var nomes) ? nomes : new List<string>();
                var statusNorm = r.Status == "Enviado" ? "Publicado" : r.Status;
                return new MeuRelatorioViewModel
                {
                    Id = r.Id,
                    ObraId = r.ObraId,
                    ObraNome = obra?.Nome ?? "—",
                    ObraGrupo = obra?.Grupo ?? "",
                    ObraStatus = obra?.Status ?? "",
                    Responsavel = obra?.Responsavel ?? "",
                    DataFormatada = r.Data.ToString("dd/MM/yyyy"),
                    NumeroFormatado = $"RDO nº {r.Numero:D3}",
                    Status = statusNorm,
                    Revisao = r.Revisao,
                    FuncionariosNomes = funcNomes,
                    FuncionariosPresentes = funcNomes.Count > 0
                        ? string.Join(", ", funcNomes.Take(3)) + (funcNomes.Count > 3 ? $" +{funcNomes.Count - 3}" : "")
                        : ""
                };
            }).ToList();

            PopularFiltrosRelatorios();
            AplicarFiltrosRelatorios();
        }

        private void PopularFiltrosRelatorios()
        {
            _inicializandoFiltros = true;

            var grupos = _todosRelatorios.Select(r => r.ObraGrupo)
                .Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g).ToList();
            FiltroGrupoRelBox.Items.Clear();
            FiltroGrupoRelBox.Items.Add(new ComboBoxItem { Content = "Todos os grupos", Tag = "" });
            foreach (var g in grupos)
                FiltroGrupoRelBox.Items.Add(new ComboBoxItem { Content = g, Tag = g });
            FiltroGrupoRelBox.SelectedIndex = 0;

            var obras = _todosRelatorios.Select(r => r.ObraNome)
                .Where(o => !string.IsNullOrEmpty(o) && o != "—").Distinct().OrderBy(o => o).ToList();
            FiltroObraRelBox.Items.Clear();
            FiltroObraRelBox.Items.Add(new ComboBoxItem { Content = "Todas as obras", Tag = "" });
            foreach (var o in obras)
                FiltroObraRelBox.Items.Add(new ComboBoxItem { Content = o, Tag = o });
            FiltroObraRelBox.SelectedIndex = 0;

            var statusObras = _todosRelatorios.Select(r => r.ObraStatus)
                .Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();
            FiltroStatusObraRelBox.Items.Clear();
            FiltroStatusObraRelBox.Items.Add(new ComboBoxItem { Content = "Todos os status", Tag = "" });
            foreach (var s in statusObras)
            {
                var statusTrad = s switch
                {
                    "In Progress" => "Em execução",
                    "Completed" => "Concluída",
                    "Paused" => "Paralisada",
                    _ => s
                };
                FiltroStatusObraRelBox.Items.Add(new ComboBoxItem { Content = statusTrad, Tag = s });
            }
            FiltroStatusObraRelBox.SelectedIndex = 0;

            var responsaveis = _todosRelatorios.Select(r => r.Responsavel)
                .Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(r => r).ToList();
            FiltroResponsavelRelBox.Items.Clear();
            FiltroResponsavelRelBox.Items.Add(new ComboBoxItem { Content = "Todos os responsáveis", Tag = "" });
            foreach (var r in responsaveis)
                FiltroResponsavelRelBox.Items.Add(new ComboBoxItem { Content = r, Tag = r });
            FiltroResponsavelRelBox.SelectedIndex = 0;

            var funcs = _todosRelatorios.SelectMany(r => r.FuncionariosNomes)
                .Distinct().OrderBy(f => f).ToList();
            FiltroFuncRelBox.Items.Clear();
            FiltroFuncRelBox.Items.Add(new ComboBoxItem { Content = "Todos os funcionários", Tag = "" });
            foreach (var f in funcs)
                FiltroFuncRelBox.Items.Add(new ComboBoxItem { Content = f, Tag = f });
            FiltroFuncRelBox.SelectedIndex = 0;

            _inicializandoFiltros = false;
        }

        private void AplicarFiltrosRelatorios()
        {
            if (MeusRelatoriosListView == null) return;

            var busca       = BuscaRelBox?.Text?.Trim().ToLower() ?? "";
            var grupo       = (FiltroGrupoRelBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var obra        = (FiltroObraRelBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var statusObra  = (FiltroStatusObraRelBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var responsavel = (FiltroResponsavelRelBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var func        = (FiltroFuncRelBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            var resultado = _todosRelatorios.AsEnumerable();

            if (!string.IsNullOrEmpty(busca))
                resultado = resultado.Where(r =>
                    r.ObraNome.ToLower().Contains(busca) ||
                    r.NumeroFormatado.ToLower().Contains(busca));

            if (!string.IsNullOrEmpty(grupo))
                resultado = resultado.Where(r => r.ObraGrupo == grupo);

            if (!string.IsNullOrEmpty(obra))
                resultado = resultado.Where(r => r.ObraNome == obra);

            if (!string.IsNullOrEmpty(statusObra))
                resultado = resultado.Where(r => r.ObraStatus == statusObra);

            if (!string.IsNullOrEmpty(responsavel))
                resultado = resultado.Where(r => r.Responsavel == responsavel);

            // Filtro de funcionário: verifica se o nome está na lista de presentes
            if (!string.IsNullOrEmpty(func))
                resultado = resultado.Where(r =>
                    r.FuncionariosNomes.Any(f =>
                        f.Equals(func, StringComparison.OrdinalIgnoreCase)));

            var lista = resultado.ToList();
            MeusRelatoriosListView.ItemsSource = lista;
            TotalObrasTexto.Text = lista.Count.ToString();
        }
        private void AplicarFiltros()
        {
            if (ObrasItemsControl == null || TotalObrasTexto == null) return;

            var termo = BuscaObrasBox?.Text?.ToLower() ?? "";
            var status = (FiltroStatusBox?.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Todos os status";

            var filtradas = _todasObras.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(termo))
                filtradas = filtradas.Where(o =>
                    o.Nome.ToLower().Contains(termo) ||
                    o.Grupo.ToLower().Contains(termo) ||
                    o.Endereco.ToLower().Contains(termo));

            if (status != "Todos os status")
                filtradas = filtradas.Where(o => o.Status == status);

            var lista = filtradas.ToList();
            TotalObrasTexto.Text = lista.Count.ToString();

            var todosGrupos = _todasObras
                .Select(o => string.IsNullOrEmpty(o.Grupo) ? "Sem grupo" : o.Grupo)
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            ObrasItemsControl.ItemsSource = todosGrupos.Select(g => new GrupoObraViewModel
            {
                Grupo = g,
                Obras = lista
                    .Where(o => (string.IsNullOrEmpty(o.Grupo) ? "Sem grupo" : o.Grupo) == g)
                    .OrderBy(o => o.Nome)
                    .ToList()
            }).ToList();
        }

        private void AtualizarMenuAtivo()
        {
            var isDark = ThemeManager.Current == Microsoft.UI.Xaml.ElementTheme.Dark;

            // Background neutro sutil + indicador azul lateral (estilo WinUI3 NavigationView)
            var corAtiva = new SolidColorBrush(isDark
                ? Color.FromArgb(38, 255, 255, 255)
                : Color.FromArgb(28, 0, 0, 0));
            var transp = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            // Indicador azul WinUI3 (#0078D4) — com margem lateral para ficar próximo ao ícone
            var bordaAtiva   = new Thickness(3, 0, 0, 0);
            var bordaInativa = new Thickness(0);
            var corBordaAtiva   = new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
            var corBordaInativa = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            // Margem lateral cria o afastamento da borda do painel
            var margAtiva   = new Thickness(8, 1, 8, 1);
            var margInativa = new Thickness(0, 1, 0, 1);
            var raioAtivo   = new CornerRadius(6);
            var raioInativo = new CornerRadius(0);

            bool inicioAtivo = !_apenasMinhas && !_mostraMeusRelatorios && !_mostraRascunhos;
            bool relAtivo    = _mostraMeusRelatorios;
            bool rascAtivo   = _mostraRascunhos;

            BtnInicio.Background      = inicioAtivo ? corAtiva : transp;
            BtnInicio.BorderThickness = inicioAtivo ? bordaAtiva : bordaInativa;
            BtnInicio.BorderBrush     = inicioAtivo ? corBordaAtiva : corBordaInativa;
            BtnInicio.Margin          = inicioAtivo ? margAtiva : margInativa;
            BtnInicio.CornerRadius    = inicioAtivo ? raioAtivo  : raioInativo;

            BtnMinhasObras.Background      = relAtivo ? corAtiva : transp;
            BtnMinhasObras.BorderThickness = relAtivo ? bordaAtiva : bordaInativa;
            BtnMinhasObras.BorderBrush     = relAtivo ? corBordaAtiva : corBordaInativa;
            BtnMinhasObras.Margin          = relAtivo ? margAtiva : margInativa;
            BtnMinhasObras.CornerRadius    = relAtivo ? raioAtivo  : raioInativo;

            BtnRascunhos.Background      = rascAtivo ? corAtiva : transp;
            BtnRascunhos.BorderThickness = rascAtivo ? bordaAtiva : bordaInativa;
            BtnRascunhos.BorderBrush     = rascAtivo ? corBordaAtiva : corBordaInativa;
            BtnRascunhos.Margin          = rascAtivo ? margAtiva : margInativa;
            BtnRascunhos.CornerRadius    = rascAtivo ? raioAtivo  : raioInativo;

            // Mostra/esconde painéis
            ObrasPanel.Visibility         = (!_mostraMeusRelatorios && !_mostraRascunhos) ? Visibility.Visible : Visibility.Collapsed;
            RelatoriosPanel.Visibility    = _mostraMeusRelatorios ? Visibility.Visible : Visibility.Collapsed;
            RascunhosPanel.Visibility     = _mostraRascunhos ? Visibility.Visible : Visibility.Collapsed;
            FiltroBar.Visibility          = (!_mostraMeusRelatorios && !_mostraRascunhos) ? Visibility.Visible : Visibility.Collapsed;

            if (_mostraMeusRelatorios)
            {
                TituloPagina.Text   = "Relatórios";
                SubtituloLista.Text = "Todos os relatórios publicados";
                BtnNovaObra.Visibility = Visibility.Collapsed;
            }
            else if (_mostraRascunhos)
            {
                TituloPagina.Text   = "Rascunhos";
                SubtituloLista.Text = "Relatórios em andamento";
                BtnNovaObra.Visibility = Visibility.Collapsed;
            }
            else
            {
                TituloPagina.Text   = "Início";
                SubtituloLista.Text = "Todas as obras";
                BtnNovaObra.Visibility = Visibility.Visible;
            }
        }

        private void AtualizarIconeTema()
        {
            if (BtnTema == null) return;
            var isDark = ThemeManager.Current == Microsoft.UI.Xaml.ElementTheme.Dark;
            BtnTema.Content = isDark ? "\uE706" : "\uE708";

        }


        private void ObraCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if ((sender as Border)?.Tag is ObraViewModel obra)
                AbrirDetalheModal(obra);
        }

        private async void AbrirDetalheModal(ObraViewModel obra)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var relatoriosBd = db.Relatorios
                .Where(r => r.ObraId == obra.Id && !r.Rascunho)
                .OrderByDescending(r => r.Data)
                .ToList()
                .Select(r => new MeuRelatorioViewModel
                {
                    Id = r.Id, ObraId = r.ObraId,
                    DataFormatada = r.Data.ToString("dd/MM/yyyy"),
                    NumeroFormatado = $"RDO nº {r.Numero:D3}",
                    Status = r.Status == "Enviado" ? "Publicado" : r.Status
                }).ToList();

            // ══════════════════════════════════════════════════════════════
            // DESIGN MODERNO: Hero header escuro + Cards com ícones
            // ══════════════════════════════════════════════════════════════
            var root = new StackPanel { Spacing = 24, Width = 1100 };

            // ── HERO HEADER: Fundo cinza moderno + Nome grande + Badge status ──
            var heroCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 45, 50, 55)),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Padding = new Thickness(32, 24, 32, 24)
            };
            var heroGrid = new Grid { ColumnSpacing = 20 };
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var heroLeft = new StackPanel { Spacing = 8 };
            heroLeft.Children.Add(new TextBlock
            {
                Text = obra.Nome,
                FontSize = 28,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
            });
            heroLeft.Children.Add(new TextBlock
            {
                Text = obra.Grupo,
                FontSize = 16,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                Opacity = 0.95
            });

            // Badge de status da obra (colorido)
            var statusObraBg = obra.Status switch
            {
                "Em execução" => Color.FromArgb(255, 0, 120, 215),
                "Concluída" => Color.FromArgb(255, 16, 124, 16),
                "Paralisada" => Color.FromArgb(255, 200, 80, 0),
                _ => Color.FromArgb(255, 100, 100, 100)
            };
            var statusObraBadge = new Border
            {
                Background = new SolidColorBrush(statusObraBg),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(16, 8, 16, 8),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = obra.Status.ToUpper(),
                    FontSize = 12,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                    CharacterSpacing = 120
                }
            };

            // Imagem da obra no hero (se disponível)
            Border? heroImageBorder = null;
            if (!string.IsNullOrEmpty(obra.ImagemPath) && System.IO.File.Exists(obra.ImagemPath))
            {
                heroImageBorder = new Border
                {
                    Width = 120,
                    Height = 80,
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var heroImage = new Image
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(obra.ImagemPath)),
                    Stretch = Stretch.UniformToFill
                };
                heroImageBorder.Child = heroImage;
            }

            Grid.SetColumn(heroLeft, 0);
            Grid.SetColumn(statusObraBadge, heroImageBorder != null ? 2 : 1);
            heroGrid.Children.Add(heroLeft);
            if (heroImageBorder != null)
            {
                Grid.SetColumn(heroImageBorder, 1);
                heroGrid.Children.Add(heroImageBorder);
            }
            heroGrid.Children.Add(statusObraBadge);
            heroCard.Child = heroGrid;
            root.Children.Add(heroCard);

            if (obra.TemRascunho)
            {
                var avisoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                avisoRow.Children.Add(new FontIcon { Glyph = "\uE7BA", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)) });
                avisoRow.Children.Add(new TextBlock { Text = "Há um rascunho salvo para esta obra.", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)), VerticalAlignment = VerticalAlignment.Center });
                root.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(255, 42, 31, 0)), BorderBrush = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)), BorderThickness = new Thickness(1), CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6), Padding = new Thickness(10), Child = avisoRow });
            }

            // Info em grid 3 colunas (mais compacto e moderno)
            var infoCard = new Border
            {
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(20, 16, 20, 16)
            };
            var infoGrid = new Grid { ColumnSpacing = 24, RowSpacing = 16 };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var infoItems = new[]
            {
                ("📍 ENDEREÇO", obra.Endereco),
                ("👤 RESPONSÁVEL FOCUS", obra.Responsavel),
                ("🏢 RESP. CLIENTE", obra.ResponsavelCliente),
                ("📋 ART / RRT", obra.ART),
                ("📅 DATA INÍCIO", obra.DataInicio.ToString("dd/MM/yyyy")),
                ("⏰ PREVISÃO TÉRMINO", obra.PrevisaoTermino?.ToString("dd/MM/yyyy") ?? "—")
            };

            for (int i = 0; i < infoItems.Length; i++)
            {
                var (lbl, val) = infoItems[i];
                int row = i / 3;
                int col = i % 3;
                if (infoGrid.RowDefinitions.Count <= row)
                    infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var sp = new StackPanel { Spacing = 4 };
                sp.Children.Add(new TextBlock
                {
                    Text = lbl,
                    FontSize = 10,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    CharacterSpacing = 120
                });
                sp.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrEmpty(val) ? "—" : val,
                    FontSize = 13,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                    Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetRow(sp, row);
                Grid.SetColumn(sp, col);
                infoGrid.Children.Add(sp);
            }
            infoCard.Child = infoGrid;
            root.Children.Add(infoCard);

            // ── Seção de relatórios ──
            var relHeader = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 4, 0, 0) };
            relHeader.Children.Add(new TextBlock
            {
                Text = "RELATÓRIOS PUBLICADOS",
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                CharacterSpacing = 150,
                VerticalAlignment = VerticalAlignment.Center
            });
            relHeader.Children.Add(new Border
            {
                Background = (Brush)Application.Current.Resources["AccentBrush"],
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Padding = new Thickness(10, 3, 10, 3),
                Child = new TextBlock
                {
                    Text = relatoriosBd.Count.ToString(),
                    FontSize = 12,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = (Brush)Application.Current.Resources["AccentFgBrush"]
                }
            });
            root.Children.Add(relHeader);

            // Coleta de botões que precisam da referência do dialog
            var botoesEditar = new List<(Button btn, MeuRelatorioViewModel rel)>();
            StackPanel? listaPanel = null;

            if (relatoriosBd.Count == 0)
            {
                var emptyState = new StackPanel
                {
                    Spacing = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 32, 0, 32)
                };
                emptyState.Children.Add(new FontIcon
                {
                    Glyph = "\uE9F9",
                    FontSize = 40,
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptyState.Children.Add(new TextBlock
                {
                    Text = "Nenhum relatório publicado ainda",
                    FontSize = 14,
                    FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptyState.Children.Add(new TextBlock
                {
                    Text = "Clique em \"+ Novo RDO\" para criar o primeiro relatório.",
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                root.Children.Add(emptyState);
            }
            else
            {
                listaPanel = new StackPanel { Spacing = 6 };

                foreach (var rel in relatoriosBd)
                {
                    var capturedRel = rel;
                    var itemBorder = new Border
                    {
                        Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                        BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                        BorderThickness = new Thickness(1),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                        Padding = new Thickness(16, 12, 16, 12)
                    };

                    // Conteúdo normal do item
                    var itemGrid = new Grid { ColumnSpacing = 8 };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    // Cor do badge de status
                    var statusBg = rel.Status switch
                    {
                        "Publicado" => Color.FromArgb(255, 10, 80, 40),
                        "Rascunho"  => Color.FromArgb(255, 70, 55, 0),
                        _           => Color.FromArgb(255, 30, 45, 74)
                    };
                    var statusFg = rel.Status switch
                    {
                        "Publicado" => Color.FromArgb(255, 0, 210, 100),
                        "Rascunho"  => Color.FromArgb(255, 240, 190, 0),
                        _           => Color.FromArgb(255, 138, 180, 212)
                    };

                    var statusBadge = new Border
                    {
                        Background = new SolidColorBrush(statusBg),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                        Padding = new Thickness(8, 3, 8, 3),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock { Text = rel.Status, FontSize = 11, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }, Foreground = new SolidColorBrush(statusFg) }
                    };

                    var infoRel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
                    var infoTexts = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
                    infoTexts.Children.Add(new TextBlock { Text = rel.NumeroFormatado, FontSize = 15, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"] });
                    infoTexts.Children.Add(new TextBlock { Text = rel.DataFormatada, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"] });
                    infoRel.Children.Add(infoTexts);
                    infoRel.Children.Add(statusBadge);

                    // Badge de revisão (só aparece se revisão > 1)
                    if (rel.Revisao > 1)
                    {
                        var revBadge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(255, 50, 30, 80)),
                            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                            Padding = new Thickness(8, 3, 8, 3),
                            VerticalAlignment = VerticalAlignment.Center,
                            Child = new TextBlock
                            {
                                Text = $"Rev. {rel.Revisao:D2}",
                                FontSize = 11,
                                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                                Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 150, 255))
                            }
                        };
                        infoRel.Children.Add(revBadge);
                    }

                    var btnEditar = new Button { Content = "\uE70F", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 14, Width = 38, Height = 38, Padding = new Thickness(0), Background = (Brush)Application.Current.Resources["EditBtnBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), Foreground = (Brush)Application.Current.Resources["AccentBrush"], VerticalAlignment = VerticalAlignment.Center, CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6) };
                    ToolTipService.SetToolTip(btnEditar, $"Editar relatório (Rev. {rel.Revisao:D2})");
                    botoesEditar.Add((btnEditar, capturedRel));

                    var btnExportar = new Button { Content = "\uE8C8", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 14, Width = 38, Height = 38, Padding = new Thickness(0), Background = (Brush)Application.Current.Resources["EditBtnBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), Foreground = (Brush)Application.Current.Resources["AccentBrush"], VerticalAlignment = VerticalAlignment.Center, CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6), Tag = capturedRel.Id };
                    ToolTipService.SetToolTip(btnExportar, "Exportar PDF");
                    btnExportar.Click += BtnExportarPdfItem_Click;

                    var btnExcluir = new Button { Content = "\uE74D", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 13, Width = 38, Height = 38, Padding = new Thickness(0), Background = new SolidColorBrush(Color.FromArgb(255, 38, 16, 16)), BorderBrush = new SolidColorBrush(Color.FromArgb(255, 90, 30, 30)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)), VerticalAlignment = VerticalAlignment.Center, CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6) };
                    ToolTipService.SetToolTip(btnExcluir, "Excluir relatório");

                    Grid.SetColumn(infoRel, 0);
                    Grid.SetColumn(btnEditar, 1);
                    Grid.SetColumn(btnExportar, 2);
                    Grid.SetColumn(btnExcluir, 3);
                    itemGrid.Children.Add(infoRel);
                    itemGrid.Children.Add(btnEditar);
                    itemGrid.Children.Add(btnExportar);
                    itemGrid.Children.Add(btnExcluir);
                    itemBorder.Child = itemGrid;

                    var capturedBorder = itemBorder;
                    var capturedListaPanel = listaPanel;
                    btnExcluir.Click += (s, ev) =>
                    {
                        // Substituição inline por confirmação
                        var confirmGrid = new Grid { ColumnSpacing = 10 };
                        confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        confirmGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var msgStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
                        msgStack.Children.Add(new TextBlock { Text = $"Excluir {capturedRel.NumeroFormatado}?", FontSize = 13, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }, Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)) });
                        msgStack.Children.Add(new TextBlock { Text = "Esta ação não pode ser desfeita.", FontSize = 11, Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"] });

                        var btnSim = new Button { Content = "Excluir", Height = 34, Padding = new Thickness(14, 0, 14, 0), Background = new SolidColorBrush(Color.FromArgb(255, 160, 30, 30)), BorderThickness = new Thickness(0), Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)), FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }, FontSize = 12 };
                        var btnNao = new Button { Content = "Cancelar", Height = 34, Padding = new Thickness(14, 0, 14, 0), Background = (Brush)Application.Current.Resources["AppBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"], FontSize = 12 };

                        Grid.SetColumn(msgStack, 0);
                        Grid.SetColumn(btnSim, 1);
                        Grid.SetColumn(btnNao, 2);
                        confirmGrid.Children.Add(msgStack);
                        confirmGrid.Children.Add(btnSim);
                        confirmGrid.Children.Add(btnNao);
                        capturedBorder.Child = confirmGrid;

                        btnSim.Click += (s2, ev2) =>
                        {
                            using var db2 = new RdoDbContext(DbContextHelper.GetOptions());
                            var r = db2.Relatorios.Find(capturedRel.Id);
                            if (r != null)
                            {
                                db2.Climas.RemoveRange(db2.Climas.Where(c => c.RelatorioId == r.Id));
                                db2.Atividades.RemoveRange(db2.Atividades.Where(a => a.RelatorioId == r.Id));
                                db2.Ocorrencias.RemoveRange(db2.Ocorrencias.Where(o => o.RelatorioId == r.Id));
                                db2.Assinaturas.RemoveRange(db2.Assinaturas.Where(a => a.RelatorioId == r.Id));
                                db2.Fotos.RemoveRange(db2.Fotos.Where(f => f.RelatorioId == r.Id));
                                db2.Relatorios.Remove(r);
                                db2.SaveChanges();
                            }
                            capturedListaPanel.Children.Remove(capturedBorder);
                        };
                        btnNao.Click += (s2, ev2) => capturedBorder.Child = itemGrid;
                    };

                    listaPanel.Children.Add(itemBorder);
                }
                root.Children.Add(listaPanel);
            }

            var scroll = new ScrollViewer { Content = root, MaxHeight = 680, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var dialog = new ContentDialog
            {
                Title = "Propriedades da Obra",
                Content = scroll,
                PrimaryButtonText = "+ Novo RDO",
                CloseButtonText = "Fechar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            _dialogPropriedadesObra = dialog;

            // WinUI 3: sobrescreve o MaxWidth padrão (~548 px) para permitir modal mais largo
            dialog.Resources["ContentDialogMaxWidth"] = 1160.0;
            dialog.Resources["ContentDialogMinWidth"] = 960.0;

            foreach (var (btn, rel) in botoesEditar)
            {
                var capturedRel = rel;
                btn.Click += async (s, ev) =>
                {
                    dialog.Hide();

                    // Aviso de nova revisão
                    var novaRev = capturedRel.Revisao + 1;
                    var avisoDialog = new ContentDialog
                    {
                        Title = "Editar relatório publicado",
                        Content = new StackPanel
                        {
                            Spacing = 8,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = $"Este relatório já foi publicado (Rev. {capturedRel.Revisao:D2}).",
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 13
                                },
                                new TextBlock
                                {
                                    Text = $"Ao salvar, será gerada uma nova revisão: Rev. {novaRev:D2}.",
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 13,
                                    Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 150, 255))
                                },
                                new TextBlock
                                {
                                    Text = "Deseja continuar?",
                                    FontSize = 13
                                }
                            }
                        },
                        PrimaryButtonText = "Editar",
                        CloseButtonText = "Cancelar",
                        XamlRoot = this.XamlRoot
                    };

                    if (await avisoDialog.ShowAsync() == ContentDialogResult.Primary)
                        Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = obra.Id, RelatorioId = capturedRel.Id });
                };
            }

            var resultado = await dialog.ShowAsync();
            _dialogPropriedadesObra = null;
            if (resultado == ContentDialogResult.Primary)
                Frame.Navigate(typeof(RdoFormPage), obra.Id);
        }

        private void BuscaRel_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltrosRelatorios();

        private void FiltroRel_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_inicializandoFiltros) return;
            AplicarFiltrosRelatorios();
        }
        private void LimparFiltrosRel_Click(object sender, RoutedEventArgs e)
        {
            BuscaRelBox.Text = "";
            if (FiltroGrupoRelBox.Items.Count > 0) FiltroGrupoRelBox.SelectedIndex = 0;
            if (FiltroObraRelBox.Items.Count > 0) FiltroObraRelBox.SelectedIndex = 0;
            if (FiltroStatusObraRelBox.Items.Count > 0) FiltroStatusObraRelBox.SelectedIndex = 0;
            if (FiltroResponsavelRelBox.Items.Count > 0) FiltroResponsavelRelBox.SelectedIndex = 0;
            if (FiltroFuncRelBox.Items.Count > 0) FiltroFuncRelBox.SelectedIndex = 0;
        }

        private void BuscaRasc_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltrosRascunhos();

        private void FiltroRasc_Changed(object sender, SelectionChangedEventArgs e) => AplicarFiltrosRascunhos();

        private void LimparFiltrosRasc_Click(object sender, RoutedEventArgs e)
        {
            BuscaRascBox.Text = "";
            if (FiltroObraRascBox.Items.Count > 0) FiltroObraRascBox.SelectedIndex = 0;
            if (FiltroRespRascBox.Items.Count > 0) FiltroRespRascBox.SelectedIndex = 0;
        }

        private void ContinuarRascunho_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int obraId)
                Frame.Navigate(typeof(RdoFormPage), obraId);
        }

        private async void ExcluirRascunho_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            var dialog = new ContentDialog
            {
                Title = "Excluir rascunho",
                Content = "Deseja excluir este rascunho permanentemente? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var r = await db.Relatorios.FindAsync(id);
                if (r != null) { db.Relatorios.Remove(r); await db.SaveChangesAsync(); }
                CarregarRascunhos();
            }
        }

        private void BuscaObras_TextChanged(object sender, TextChangedEventArgs e)
        {
            LimparBuscaBtn.Visibility = string.IsNullOrEmpty(BuscaObrasBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
            AplicarFiltros();
        }

        private void LimparBusca_Click(object sender, RoutedEventArgs e)
        {
            BuscaObrasBox.Text = "";
            LimparBuscaBtn.Visibility = Visibility.Collapsed;
            AplicarFiltros();
        }

        private void FiltroStatus_Changed(object sender, SelectionChangedEventArgs e)
            => AplicarFiltros();

        private void BtnInicio_Click(object sender, RoutedEventArgs e)
        {
            _apenasMinhas = false;
            _mostraMeusRelatorios = false;
            _mostraRascunhos = false;
            CarregarObras();
            AtualizarMenuAtivo();
        }

        private void BtnMinhasObras_Click(object sender, RoutedEventArgs e)
        {
            _mostraMeusRelatorios = true;
            _mostraRascunhos = false;
            AtualizarMenuAtivo();
            CarregarMeusRelatorios();
        }

        private void BtnNovaObra_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(NovaObraPage));

        // Retorna "Primeiro S." — skip preposições brasileiras
        private static string AbreviarNome(string nomeCompleto)
        {
            var partes = nomeCompleto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length <= 1) return nomeCompleto;
            var prep = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "de", "da", "do", "dos", "das", "e", "di", "del", "van", "von" };
            for (int i = 1; i < partes.Length; i++)
            {
                if (!prep.Contains(partes[i]))
                    return $"{partes[0]} {partes[i][0]}.";
            }
            return partes[0];
        }

        private void SairBtn_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(LoginPage));

        private void BtnCadastros_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(CadastrosPage));

        private void BtnRascunhos_Click(object sender, RoutedEventArgs e)
        {
            _mostraMeusRelatorios = false;
            _mostraRascunhos = true;
            AtualizarMenuAtivo();
            CarregarRascunhos();
        }

        private void CarregarRascunhos()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            _todosRascunhos = db.Relatorios
                .Where(r => r.Rascunho)
                .OrderByDescending(r => r.CriadoEm)
                .ToList()
                .Select(r =>
                {
                    var obra = db.Obras.Find(r.ObraId);
                    var obraNome = obra?.Nome ?? "—";
                    var usuario = db.Usuarios.Find(r.UsuarioId);
                    var criador = usuario?.Nome ?? "—";
                    var numero = db.Relatorios.Count(x => x.ObraId == r.ObraId && !x.Rascunho) + 1;
                    return new RascunhoViewModel
                    {
                        Id = r.Id,
                        ObraId = r.ObraId,
                        ObraNome = obraNome,
                        ObraGrupo = obra?.Grupo ?? "",
                        NumeroFormatado = $"RDO nº {numero:D3}",
                        DataRelatorio = r.Data.ToString("dd/MM/yyyy"),
                        SalvoEm = $"Salvo em {r.CriadoEm:dd/MM/yyyy HH:mm}",
                        CriadoPor = criador,
                        CriadoEmDate = r.CriadoEm,
                        ObraNomeFiltro = obraNome.ToLower(),
                        CriadoPorFiltro = criador.ToLower()
                    };
                })
                .ToList();

            PopularFiltrosRascunhos();
            AplicarFiltrosRascunhos();
        }

        private void PopularFiltrosRascunhos()
        {
            if (FiltroObraRascBox == null) return;

            var obras = _todosRascunhos.Select(r => r.ObraNome)
                .Where(o => !string.IsNullOrEmpty(o) && o != "—")
                .Distinct().OrderBy(o => o).ToList();
            FiltroObraRascBox.Items.Clear();
            FiltroObraRascBox.Items.Add(new ComboBoxItem { Content = "Todas as obras", Tag = "" });
            foreach (var o in obras)
                FiltroObraRascBox.Items.Add(new ComboBoxItem { Content = o, Tag = o });
            FiltroObraRascBox.SelectedIndex = 0;

            var responsaveis = _todosRascunhos.Select(r => r.CriadoPor)
                .Where(r => !string.IsNullOrEmpty(r) && r != "—")
                .Distinct().OrderBy(r => r).ToList();
            FiltroRespRascBox.Items.Clear();
            FiltroRespRascBox.Items.Add(new ComboBoxItem { Content = "Todos os responsáveis", Tag = "" });
            foreach (var r in responsaveis)
                FiltroRespRascBox.Items.Add(new ComboBoxItem { Content = r, Tag = r });
            FiltroRespRascBox.SelectedIndex = 0;
        }

        private void AplicarFiltrosRascunhos()
        {
            if (RascunhosListView == null) return;

            var busca = BuscaRascBox?.Text?.Trim().ToLower() ?? "";
            var obra = (FiltroObraRascBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            var resp = (FiltroRespRascBox?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            var resultado = _todosRascunhos.AsEnumerable();

            if (!string.IsNullOrEmpty(busca))
                resultado = resultado.Where(r =>
                    r.ObraNomeFiltro.Contains(busca) ||
                    r.CriadoPorFiltro.Contains(busca));

            if (!string.IsNullOrEmpty(obra))
                resultado = resultado.Where(r => r.ObraNome == obra);

            if (!string.IsNullOrEmpty(resp))
                resultado = resultado.Where(r => r.CriadoPor == resp);

            var lista = resultado.ToList();
            RascunhosListView.ItemsSource = lista;
            TotalRascunhosTexto.Text = lista.Count.ToString();
            RascunhosEmptyState.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            AtualizarMenuAtivo();
            AtualizarIconeTema();
        }

        private void BtnPerfil_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(PerfilPage));
        }

        private async void BtnStorageConfig_Click(object sender, RoutedEventArgs e)
        {
            var cfg = SupabaseConfig.Load();

            var projectUrlBox = new TextBox
            {
                PlaceholderText = "https://xxxxxxxxxxx.supabase.co",
                Text = cfg.ProjectUrl,
                Header = "URL do projeto Supabase"
            };
            var serviceKeyBox = new TextBox
            {
                PlaceholderText = "eyJhbGciOiJIUzI1NiIs...",
                Text = cfg.ServiceKey,
                Header = "Service Role Key (ou Anon Key com bucket público)"
            };
            var bucketBox = new TextBox
            {
                PlaceholderText = "rdo-anexos",
                Text = string.IsNullOrWhiteSpace(cfg.Bucket) ? "rdo-anexos" : cfg.Bucket,
                Header = "Nome do bucket"
            };

            var form = new StackPanel { Spacing = 16, Width = 480 };
            form.Children.Add(new TextBlock
            {
                Text = "Configure o Supabase Storage para que os anexos dos relatórios " +
                       "fiquem acessíveis por qualquer pessoa com o link.",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextSecondaryBrush"],
                FontSize = 12
            });
            form.Children.Add(projectUrlBox);
            form.Children.Add(serviceKeyBox);
            form.Children.Add(bucketBox);
            form.Children.Add(new HyperlinkButton
            {
                Content = "Como obter essas informações? (supabase.com)",
                NavigateUri = new Uri("https://supabase.com/dashboard/project/_/settings/api")
            });

            var dialog = new ContentDialog
            {
                Title = "Configurar armazenamento de anexos",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                cfg.ProjectUrl = projectUrlBox.Text.Trim().TrimEnd('/');
                cfg.ServiceKey = serviceKeyBox.Text.Trim();
                cfg.Bucket = string.IsNullOrWhiteSpace(bucketBox.Text) ? "rdo-anexos" : bucketBox.Text.Trim();
                cfg.Save();

                var ok = new ContentDialog
                {
                    Title = "Configuração salva",
                    Content = "Os próximos sincronismos farão upload automático dos anexos para o Supabase.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await ok.ShowAsync();
            }
        }

        private async void BtnExportarPdfItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int relatorioId) return;
            try
            {
                var caminho = await RDO.App.Services.RdoPdfExportService.ExportAsync(relatorioId);
                if (caminho != null)
                    await Windows.System.Launcher.LaunchFileAsync(
                        await Windows.Storage.StorageFile.GetFileFromPathAsync(caminho));
            }
            catch (Exception ex)
            {
                // Fecha o dialog pai (Propriedades da Obra) se estiver aberto,
                // pois WinUI 3 não permite dois ContentDialogs simultâneos.
                _dialogPropriedadesObra?.Hide();
                _dialogPropriedadesObra = null;

                var d = new ContentDialog
                {
                    Title = "Erro ao gerar PDF",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await d.ShowAsync();
            }
        }

        private async void BtnEditarRelatorio_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not MeuRelatorioViewModel rel) return;

            // Aviso de nova revisão
            var novaRev = rel.Revisao + 1;
            var avisoDialog = new ContentDialog
            {
                Title = "Editar relatório publicado",
                Content = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"Este relatório já foi publicado (Rev. {rel.Revisao:D2}).",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 13,
                            Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"]
                        },
                        new TextBlock
                        {
                            Text = $"Ao salvar, será gerada uma nova revisão: Rev. {novaRev:D2}.",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 13,
                            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 200, 150, 255))
                        },
                        new TextBlock
                        {
                            Text = "Deseja continuar?",
                            FontSize = 13,
                            Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"]
                        }
                    }
                },
                PrimaryButtonText = "Editar",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            if (await avisoDialog.ShowAsync() == ContentDialogResult.Primary)
                Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = rel.ObraId, RelatorioId = rel.Id });
        }

        private async void BtnExcluirRelatorio_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int relatorioId) return;

            var confirmDialog = new ContentDialog
            {
                Title = "Excluir relatório",
                Content = "Deseja excluir este relatório permanentemente? Esta ação não pode ser desfeita.",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var rel = await db.Relatorios.FindAsync(relatorioId);
                    if (rel != null)
                    {
                        db.Relatorios.Remove(rel);
                        await db.SaveChangesAsync();
                        CarregarMeusRelatorios(); // Recarrega a lista
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Erro ao excluir",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void BtnVincularPerfil_Click(object sender, RoutedEventArgs e)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var funcionarios = db.Funcionarios.Where(f => f.Ativo).OrderBy(f => f.Nome).ToList();

            var combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Selecione seu nome na lista"
            };

            int? atualId = null;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("FuncionarioVinculadoId"))
                atualId = (int?)ApplicationData.Current.LocalSettings.Values["FuncionarioVinculadoId"];

            foreach (var f in funcionarios)
            {
                var item = new ComboBoxItem { Content = $"{f.Nome} — {f.Funcao}", Tag = f.Id };
                if (f.Id == atualId) item.IsSelected = true;
                combo.Items.Add(item);
            }

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock
            {
                Text = "Vincule sua conta ao seu registro de funcionário para que seus relatórios apareçam nesta lista.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 74, 96, 128)),
                FontSize = 13
            });
            panel.Children.Add(combo);

            var dialog = new ContentDialog
            {
                Title = "Vincular perfil de funcionário",
                Content = panel,
                PrimaryButtonText = "Salvar",
                SecondaryButtonText = "Desvincular",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary &&
                combo.SelectedItem is ComboBoxItem selected && selected.Tag is int funcId)
            {
                ApplicationData.Current.LocalSettings.Values["FuncionarioVinculadoId"] = funcId;
                var nomeCompleto = funcionarios.First(f => f.Id == funcId).Nome;
                NomeUsuarioTexto.Text = AbreviarNome(nomeCompleto);
                PerfilUsuarioTexto.Text = "Vinculado";
            }
            else if (result == ContentDialogResult.Secondary)
            {
                ApplicationData.Current.LocalSettings.Values.Remove("FuncionarioVinculadoId");
                NomeUsuarioTexto.Text = "Usuário";
                PerfilUsuarioTexto.Text = "Sem vínculo";
            }
        }
    }
}
