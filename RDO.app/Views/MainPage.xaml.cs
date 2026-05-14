using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RDO.app.Services;
using RDO.App.Services;
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

        public bool PrazoVencido => Status != "Concluída" && Status != "Paralisada"
            && PrevisaoTermino.HasValue
            && PrevisaoTermino.Value.Date < DateTime.Today;

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
        public DateTime Data { get; set; }
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
        private List<ObraViewModel> _todasObras = new();
        private List<MeuRelatorioViewModel> _todosRelatorios = new();
        private List<RascunhoViewModel> _todosRascunhos = new();
        private ContentDialog? _dialogPropriedadesObra;

        private static string ApiUrl => RDO.App.Services.AppConfig.Load().ApiUrl;
        private readonly SyncService _syncService = new SyncService(RDO.App.Services.AppConfig.Load().ApiUrl);
        private DispatcherTimer? _autoSyncTimer;
        private bool _sincronizando = false;

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

            // Auto-sync a cada 2 minutos (replica mudanças de outras máquinas sem clicar)
            _autoSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            _autoSyncTimer.Tick += async (_, _) => { if (!_sincronizando) await SincronizarAsync(); };
            _autoSyncTimer.Start();
        }

        private async Task SincronizarAsync()
        {
            if (_sincronizando) return;
            _sincronizando = true;
            AtualizarSyncUI(SyncEstado.Sincronizando);
            BtnSync.IsEnabled = false;

            try
            {
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
                    CadastrosPage.PendingRefresh = true;
                    // Se o usuário já está em CadastrosPage, recarrega a aba imediatamente
                    if (Frame.Content is CadastrosPage cadastrosAtivo)
                        cadastrosAtivo.RefreshFromSync();
                    if (_mostraMeusRelatorios) CarregarMeusRelatorios();
                    else CarregarObras();
                    // Atualiza rascunhos se o painel estiver aberto
                    if (_mostraRascunhos) CarregarRascunhos();
                }
                else
                {
                    // Converte o código interno (ex: SYNC-PULL-CONN) para o código padronizado (ex: SYNC-002)
                    var codigoPadrao = string.IsNullOrWhiteSpace(resultado.ErrorCode)
                        ? RDO.App.Services.AppErrorCodes.SYNC_007
                        : RDO.App.Services.AppErrorCodes.MapToStandardCode(resultado.ErrorCode);
                    var mensagem = resultado.Error ?? "Erro desconhecido";
                    // #region agent log
                    RDO.app.Services.DebugAgentLog.Write("H1-H4", "MainPage.xaml.cs:SincronizarAsync", "sync UI error branch",
                        new
                        {
                            rawErrorCode = resultado.ErrorCode,
                            mappedCode = codigoPadrao,
                            resultado.Success,
                            resultado.IsOffline,
                            msgLen = (mensagem ?? "").Length
                        });
                    // #endregion
                    AtualizarSyncUI(SyncEstado.Erro, codigoPadrao, mensagem ?? "");
                }
            }
            catch (Exception ex)
            {
                BtnSync.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"[SYNC] Exceção inesperada: {ex}");
                // #region agent log
                RDO.app.Services.DebugAgentLog.Write("H5", "MainPage.xaml.cs:SincronizarAsync", "exception in SincronizarAsync",
                    new { exType = ex.GetType().Name, exMessage = ex.Message });
                // #endregion
                AtualizarSyncUI(SyncEstado.Erro,
                    RDO.App.Services.AppErrorCodes.SYNC_007,
                    "Falha na comunicação com o servidor");
            }
            finally
            {
                _sincronizando = false;
            }
        }

        private enum SyncEstado { Sincronizando, Sincronizado, SemRede, Erro }

        private void AtualizarSyncUI(SyncEstado estado, string detalhe = "", string mensagem = "")
        {
            // Para erros, detalhe = código padronizado (ex: SYNC-002), mensagem = texto descritivo
            var textoErro = estado == SyncEstado.Erro && !string.IsNullOrEmpty(detalhe)
                ? $"[{detalhe}]  {(string.IsNullOrEmpty(mensagem) ? detalhe : mensagem)}"
                : detalhe;

            var (glyph, texto, bgHex, fgHex) = estado switch
            {
                SyncEstado.Sincronizando => ("\uE895", "Sincronizando...",
                    "#1A3050", "#6AB0FF"),
                SyncEstado.Sincronizado  => ("\uE73E", string.IsNullOrEmpty(detalhe)
                    ? "Sincronizado" : $"Sincronizado  {detalhe}",
                    "#0A2A14", "#00D264"),
                SyncEstado.SemRede       => ("\uE774", "Sem rede",
                    "#2A1A00", "#F0BE00"),
                SyncEstado.Erro          => ("\uE783", string.IsNullOrEmpty(textoErro)
                    ? "Erro de sync" : textoErro,
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
                await RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.IO_001, $"Caminho: {logDir}", ex);
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

            // Botão PDF de troubleshooting no rodapé
            var btnPdfTroubleshooting = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FontIcon { Glyph = "\uEA90", FontSize = 14, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 230, 80, 80)) },
                        new TextBlock { Text = "Abrir guia de erros (PDF)", FontSize = 13, VerticalAlignment = VerticalAlignment.Center }
                    }
                },
                HorizontalAlignment = HorizontalAlignment.Left,
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1)
            };
            btnPdfTroubleshooting.Click += async (s, ev) =>
            {
                const string urlPdfTroubleshooting = "https://zoaakrwiqfudidmwziga.supabase.co/storage/v1/object/sign/DocumentosRDO/ANEXO%20C.pdf?token=eyJraWQiOiJzdG9yYWdlLXVybC1zaWduaW5nLWtleV83MzlkYzIwZi02YzRlLTRhMjQtYmM4ZC1jMGEwNjNmYTA1OTIiLCJhbGciOiJIUzI1NiJ9.eyJ1cmwiOiJEb2N1bWVudG9zUkRPL0FORVhPIEMucGRmIiwiaWF0IjoxNzc3Mzk4OTYxLCJleHAiOjQ5MzA5OTg5NjF9.GGBxcO6mFPah13VkbC_P3vuOYUL23P22LF2Le7000iM";
                await Windows.System.Launcher.LaunchUriAsync(new Uri(urlPdfTroubleshooting));
            };
            root.Children.Add(btnPdfTroubleshooting);

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
            // Prioridade 1: funcionário vinculado por ID salvo
            if (LocalSettingsService.ContainsKey("FuncionarioVinculadoId"))
            {
                var funcId = LocalSettingsService.Get<int?>("FuncionarioVinculadoId");
                if (funcId != null)
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var func = db.Funcionarios.Find(funcId.Value);
                    if (func != null)
                    {
                        NomeUsuarioTexto.Text = AbreviarNome(func.Nome);
                        PerfilUsuarioTexto.Text = string.IsNullOrWhiteSpace(func.Funcao) ? "Colaborador" : func.Funcao;
                        return;
                    }
                }
            }

            var nomeLogin = LocalSettingsService.Get<string>("NomeUsuario");
            var usuarioId = LocalSettingsService.Get<int?>("UsuarioId");

            if (!string.IsNullOrEmpty(nomeLogin))
                NomeUsuarioTexto.Text = AbreviarNome(nomeLogin);

            using var db2 = new RdoDbContext(DbContextHelper.GetOptions());

            // Prioridade 2: vincula por nome ao funcionário
            if (!string.IsNullOrEmpty(nomeLogin))
            {
                var funcMatch = db2.Funcionarios.FirstOrDefault(f => f.Ativo && f.Nome == nomeLogin);
                if (funcMatch != null)
                {
                    PerfilUsuarioTexto.Text = string.IsNullOrWhiteSpace(funcMatch.Funcao) ? "Colaborador" : funcMatch.Funcao;
                    LocalSettingsService.Set("FuncionarioVinculadoId", funcMatch.Id);
                    return;
                }
            }

            // Prioridade 3: vincula pelo email do usuário ao funcionário
            if (usuarioId.HasValue)
            {
                var usuario = db2.Usuarios.Find(usuarioId.Value);
                if (usuario != null)
                {
                    // Tenta encontrar funcionário pelo email (login)
                    var funcPorEmail = db2.Funcionarios.FirstOrDefault(f =>
                        f.Ativo && f.Contato.ToLower().Contains(usuario.Email.ToLower()));
                    if (funcPorEmail != null)
                    {
                        PerfilUsuarioTexto.Text = string.IsNullOrWhiteSpace(funcPorEmail.Funcao) ? "Colaborador" : funcPorEmail.Funcao;
                        LocalSettingsService.Set("FuncionarioVinculadoId", funcPorEmail.Id);
                        return;
                    }

                    // Fallback: perfil do sistema
                    PerfilUsuarioTexto.Text = usuario.Perfil switch
                    {
                        "Admin"      => "Administrador",
                        "Technician" => "Colaborador",
                        _            => string.IsNullOrWhiteSpace(usuario.Perfil) ? "Colaborador" : usuario.Perfil
                    };
                    return;
                }
            }

            PerfilUsuarioTexto.Text = "Colaborador";
        }

        private async void CarregarObras()
        {
            try
            {
            // DB em background — libera a UI thread imediatamente
            var (obras, empPorGrupo, rdoCounts, rascunhoCounts, rascunhoTotal, rdosMes) = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var o = db.Obras.Where(x => x.IsActive && !x.IsDeleted).ToList();

                var empresas = db.Empresas.Where(e => e.IsActive).ToList();
                var epg = new Dictionary<string, RDO.Data.Models.Empresa>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in empresas)
                {
                    var key = RDO.App.Services.LogoService.GetBaseNome(e.Nome);
                    if (!epg.ContainsKey(key)) epg[key] = e;
                }

                var obraIds = o.Select(x => x.Id).ToList();
                var rdc = db.Relatorios
                    .Where(r => !r.Rascunho && !r.IsDeleted && obraIds.Contains(r.ObraId))
                    .GroupBy(r => r.ObraId)
                    .ToDictionary(g => g.Key, g => g.Count());
                var rsc = db.Relatorios
                    .Where(r => r.Rascunho && !r.IsDeleted && obraIds.Contains(r.ObraId))
                    .GroupBy(r => r.ObraId)
                    .ToDictionary(g => g.Key, g => true);

                // Total real de rascunhos (não o número de obras com rascunho)
                var rascTotal = db.Relatorios
                    .Count(r => r.Rascunho && !r.IsDeleted && obraIds.Contains(r.ObraId));

                var agora = DateTime.Now;
                // Filtrado por obras ativas para não contar RDOs de obras excluídas
                var rdosMesCount = db.Relatorios
                    .Where(r => !r.Rascunho && !r.IsDeleted && obraIds.Contains(r.ObraId))
                    .ToList()
                    .Count(r => r.Data.Year == agora.Year && r.Data.Month == agora.Month);

                return (o, epg, rdc, rsc, rascTotal, rdosMesCount);
            });

            // Atualiza KPIs com dados já carregados
            AtualizarKpis(obras, rdoCounts, rascunhoCounts, rascunhoTotal, rdosMes);

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OBRAS] Erro ao carregar obras: {ex}");
                _ = RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_001, null, ex);
            }
        }

        private async void CarregarMeusRelatorios()
        {
            try
            {
            _todosRelatorios = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());

                var obraIds = db.Obras
                    .Where(o => o.IsActive && !o.IsDeleted)
                    .Select(o => o.Id)
                    .ToHashSet();

                var relatorios = db.Relatorios
                    .Where(r => !r.Rascunho && !r.IsDeleted && r.Status != "Rascunho")
                    .OrderByDescending(r => r.Data)
                    .ToList()
                    .Where(r => obraIds.Contains(r.ObraId))
                    .ToList();

                var relIds = relatorios.Select(r => r.Id).ToList();
                var presencasPorRel = db.PresencasFuncionarios
                    .Where(p => relIds.Contains(p.ReportId))
                    .GroupBy(p => p.ReportId)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.EmployeeName).Distinct().ToList());

                // Carrega obras em lote para evitar N+1
                var obrasDict = db.Obras
                    .Where(o => relatorios.Select(r => r.ObraId).Contains(o.Id))
                    .ToDictionary(o => o.Id);

                return relatorios.Select(r =>
                {
                    obrasDict.TryGetValue(r.ObraId, out var obra);
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
                        Data = r.Data,
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
            });

            PopularFiltrosRelatorios();
            AplicarFiltrosRelatorios();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RELATORIOS] Erro ao carregar: {ex}");
                _ = RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_001, null, ex);
            }
        }

        private void PopularFiltrosRelatorios()
        {
            // Filtros de dropdown removidos — apenas reaplica a busca textual
            AplicarFiltrosRelatorios();
        }

        private void AplicarFiltrosRelatorios()
        {
            if (MeusRelatoriosListView == null) return;

            var busca = BuscaRelBox?.Text?.Trim().ToLower() ?? "";

            var resultado = _todosRelatorios.AsEnumerable();

            if (!string.IsNullOrEmpty(busca))
                resultado = resultado.Where(r =>
                    r.ObraNome.ToLower().Contains(busca) ||
                    r.NumeroFormatado.ToLower().Contains(busca));

            // Ordenação padrão: mais recente primeiro
            var lista = resultado.OrderByDescending(r => r.Data).ToList();

            MeusRelatoriosListView.ItemsSource = lista;
            TotalObrasTexto.Text = lista.Count.ToString();
            if (ContadorRelTexto != null)
                ContadorRelTexto.Text = lista.Count == 1 ? "1 relatório" : $"{lista.Count} relatórios";
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
            var isInicio = !_mostraMeusRelatorios && !_mostraRascunhos;
            ObrasPanel.Visibility         = isInicio ? Visibility.Visible : Visibility.Collapsed;
            RelatoriosPanel.Visibility    = _mostraMeusRelatorios ? Visibility.Visible : Visibility.Collapsed;
            RascunhosPanel.Visibility     = _mostraRascunhos ? Visibility.Visible : Visibility.Collapsed;
            FiltroBar.Visibility          = isInicio ? Visibility.Visible : Visibility.Collapsed;
            KpiBar.Visibility             = isInicio ? Visibility.Visible : Visibility.Collapsed;

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

        private void AtualizarKpis(
            List<Obra> obras,
            Dictionary<int, int> rdoCounts,
            Dictionary<int, bool> rascunhoCounts,
            int rascunhoTotal,
            int rdosMes)
        {
            if (KpiEmExecucao == null) return;

            KpiEmExecucao.Text  = obras.Count(o => o.Status == "Em execução").ToString();
            KpiConcluidas.Text  = obras.Count(o => o.Status == "Concluída").ToString();
            KpiParalisadas.Text = obras.Count(o => o.Status == "Paralisada").ToString();
            KpiRdosMes.Text     = rdosMes.ToString();
            KpiRascunhos.Text   = rascunhoTotal.ToString();
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
                .Where(r => r.ObraId == obra.Id && !r.Rascunho && !r.IsDeleted)
                .OrderByDescending(r => r.Data)
                .ToList()
                .Select(r => new MeuRelatorioViewModel
                {
                    Id = r.Id, ObraId = r.ObraId,
                    Data = r.Data,
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

            // Badge/botão de status interativo
            Color StatusCorFor(string s) => s switch
            {
                "Em execução" => Color.FromArgb(255, 0, 120, 215),
                "Concluída"   => Color.FromArgb(255, 16, 124, 16),
                "Paralisada"  => Color.FromArgb(255, 200, 80, 0),
                _             => Color.FromArgb(255, 100, 100, 100)
            };

            // Texto do status exibido no badge
            var statusBadgeText = new TextBlock
            {
                Text = obra.Status.ToUpper(),
                FontSize = 12,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                CharacterSpacing = 120,
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusBadgeIcon = new FontIcon
            {
                Glyph = "", FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusBadgeContent = new StackPanel { Orientation = Orientation.Horizontal };
            statusBadgeContent.Children.Add(statusBadgeText);
            statusBadgeContent.Children.Add(statusBadgeIcon);

            var statusBtn = new Button
            {
                Background = new SolidColorBrush(StatusCorFor(obra.Status)),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(16, 8, 12, 8),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                VerticalAlignment = VerticalAlignment.Center,
                Content = statusBadgeContent
            };
            ToolTipService.SetToolTip(statusBtn, "Clique para alterar o status");

            // Botão "Editar obra" no hero
            var editarObraBtn = new Button
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                VerticalAlignment = VerticalAlignment.Center,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 6,
                    Children =
                    {
                        new FontIcon { Glyph = "", FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) },
                        new TextBlock { Text = "Editar", FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) }
                    }
                }
            };
            ToolTipService.SetToolTip(editarObraBtn, "Editar informações da obra");

            // Painel de ações agrupa status + editar
            var heroActionsPanel = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            heroActionsPanel.Children.Add(statusBtn);
            heroActionsPanel.Children.Add(editarObraBtn);

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
                heroImageBorder.Child = new Image
                {
                    Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(obra.ImagemPath)),
                    Stretch = Stretch.UniformToFill
                };
            }

            Grid.SetColumn(heroLeft, 0);
            heroGrid.Children.Add(heroLeft);
            if (heroImageBorder != null)
            {
                Grid.SetColumn(heroImageBorder, 1);
                heroGrid.Children.Add(heroImageBorder);
                Grid.SetColumn(heroActionsPanel, 2);
            }
            else
            {
                Grid.SetColumn(heroActionsPanel, 1);
            }
            heroGrid.Children.Add(heroActionsPanel);
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

                    // Badge de revisão (aparece a partir de Rev. 01)
                    if (rel.Revisao > 0)
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
                                var now = SyncService.GetPushTimestamp();
                                if (r.IsSynced || !r.IsDraft)
                                {
                                    // Soft-delete: propaga para API e outras máquinas no próximo sync
                                    r.IsDeleted = true;
                                    r.UpdatedAt = now;
                                    r.IsSynced = false;
                                    RDO.app.Services.SyncLogger.LogDebug($"[DELETE-REPORT] id={r.Id} updatedAt={now:O} since={SyncService.LoadLastSyncTime():O}");
                                    foreach (var x in db2.Climas.Where(c => c.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.Atividades.Where(a => a.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.Ocorrencias.Where(o => o.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.Assinaturas.Where(a => a.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.Fotos.Where(f => f.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.RelatorioEquipamentos.Where(re => re.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                    foreach (var x in db2.RelatorioAcompanhantes.Where(rc => rc.RelatorioId == r.Id).ToList())
                                        { x.IsDeleted = true; x.UpdatedAt = now; }
                                }
                                else
                                {
                                    // Rascunho local — nunca chegou à API, hard-delete é seguro
                                    db2.Climas.RemoveRange(db2.Climas.Where(c => c.RelatorioId == r.Id));
                                    db2.Atividades.RemoveRange(db2.Atividades.Where(a => a.RelatorioId == r.Id));
                                    db2.Ocorrencias.RemoveRange(db2.Ocorrencias.Where(o => o.RelatorioId == r.Id));
                                    db2.Assinaturas.RemoveRange(db2.Assinaturas.Where(a => a.RelatorioId == r.Id));
                                    db2.Fotos.RemoveRange(db2.Fotos.Where(f => f.RelatorioId == r.Id));
                                    db2.Relatorios.Remove(r);
                                }
                                db2.SaveChanges();

                                // Renumera os relatórios com Numero maior ao excluído
                                if (!r.Rascunho)
                                {
                                    var subsequentes = db2.Relatorios
                                        .Where(x => x.ObraId == r.ObraId && x.Numero > r.Numero && !x.IsDeleted && !x.Rascunho)
                                        .ToList();
                                    if (subsequentes.Count > 0)
                                    {
                                        foreach (var sub in subsequentes)
                                        {
                                            sub.Numero--;
                                            sub.UpdatedAt = now;
                                            sub.IsSynced = false;
                                        }
                                        db2.SaveChanges();
                                    }
                                }
                            }
                            capturedListaPanel.Children.Remove(capturedBorder);
                        };
                        btnNao.Click += (s2, ev2) => capturedBorder.Child = itemGrid;
                    };

                    listaPanel.Children.Add(itemBorder);
                }
                root.Children.Add(listaPanel);
            }

            // ── GALERIA DE FOTOS ─────────────────────────────────────────
            var relIdsGal = db.Relatorios
                .Where(r => r.ObraId == obra.Id && !r.Rascunho && !r.IsDeleted)
                .Select(r => r.Id).ToList();

            // Carrega fotos e mapeia para o número do RDO
            var rdoNumeroPorId = db.Relatorios
                .Where(r => relIdsGal.Contains(r.Id))
                .ToDictionary(r => r.Id, r => r.Numero);

            var fotos = db.Fotos
                .Where(f => relIdsGal.Contains(f.RelatorioId) && !f.IsDeleted && f.Type == "photo")
                .OrderBy(f => f.TiradaEm)
                .ToList()
                .Where(f => !string.IsNullOrEmpty(f.CaminhoArquivo) && System.IO.File.Exists(f.CaminhoArquivo))
                .ToList();

            if (fotos.Count > 0)
            {
                var galTitleRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal, Spacing = 12,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                galTitleRow.Children.Add(new TextBlock
                {
                    Text = "GALERIA DE FOTOS",
                    FontSize = 11, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                    CharacterSpacing = 150, VerticalAlignment = VerticalAlignment.Center
                });
                galTitleRow.Children.Add(new Border
                {
                    Background = (Brush)Application.Current.Resources["AccentBrush"],
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                    Padding = new Thickness(10, 3, 10, 3),
                    Child = new TextBlock
                    {
                        Text = fotos.Count.ToString(),
                        FontSize = 12, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                        Foreground = (Brush)Application.Current.Resources["AccentFgBrush"]
                    }
                });
                root.Children.Add(galTitleRow);

                var thumbRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

                for (int fi = 0; fi < fotos.Count; fi++)
                {
                    var capturedIndex = fi;
                    var capturedFotos = fotos;
                    var capturedObra  = obra.Nome;
                    var foto = fotos[fi];
                    rdoNumeroPorId.TryGetValue(foto.RelatorioId, out var rdoNum);

                    // Thumb container
                    var thumbBorder = new Border
                    {
                        Width = 128, Height = 96,
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                        Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                        BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                        BorderThickness = new Thickness(1)
                    };

                    var thumbGrid = new Grid();
                    var thumbImg = new Image { Stretch = Stretch.UniformToFill };
                    try { thumbImg.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(foto.CaminhoArquivo)); }
                    catch { }
                    thumbGrid.Children.Add(thumbImg);

                    // Overlay inferior com número do RDO
                    if (rdoNum > 0)
                    {
                        var badge = new Border
                        {
                            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                            Padding = new Thickness(6, 2, 6, 2),
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        badge.Child = new TextBlock
                        {
                            Text = $"RDO {rdoNum:D3}",
                            FontSize = 10, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                        };
                        thumbGrid.Children.Add(badge);
                    }

                    // Botão clicável invisível
                    var thumbBtn = new Button
                    {
                        Content = thumbGrid, Width = 128, Height = 96,
                        Padding = new Thickness(0), BorderThickness = new Thickness(0),
                        Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
                    };
                    thumbBtn.Click += async (s, ev) =>
                    {
                        _dialogPropriedadesObra?.Hide();
                        _dialogPropriedadesObra = null;
                        await MostrarVisualizadorFotos(capturedFotos, capturedIndex, capturedObra);
                    };

                    thumbBorder.Child = thumbBtn;
                    thumbRow.Children.Add(thumbBorder);
                }

                root.Children.Add(new ScrollViewer
                {
                    Content = thumbRow,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
                });
            }
            // ────────────────────────────────────────────────────────────

            // ── HISTÓRICO DE ATIVIDADE ──────────────────────────────────
            var histRelatorios = db.Relatorios
                .Where(r => r.ObraId == obra.Id && !r.IsDeleted)
                .OrderBy(r => r.CriadoEm)
                .ToList();

            var histUsuIds  = histRelatorios.Select(r => r.UsuarioId).Distinct().ToList();
            var histUsers   = db.Usuarios.Where(u => histUsuIds.Contains(u.Id))
                                         .ToDictionary(u => u.Id);

            var histTitleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 4, 0, 0) };
            histTitleRow.Children.Add(new TextBlock
            {
                Text = "HISTÓRICO DE ATIVIDADE",
                FontSize = 11, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                CharacterSpacing = 150, VerticalAlignment = VerticalAlignment.Center
            });
            histTitleRow.Children.Add(new Border
            {
                Background = (Brush)Application.Current.Resources["AppBorderBrush"],
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Padding = new Thickness(10, 3, 10, 3),
                Child = new TextBlock
                {
                    Text = histRelatorios.Count.ToString(),
                    FontSize = 12, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 },
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"]
                }
            });
            root.Children.Add(histTitleRow);

            if (histRelatorios.Count == 0)
            {
                root.Children.Add(new TextBlock
                {
                    Text = "Nenhuma atividade registrada.",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }
            else
            {
                var histStack = new StackPanel { Spacing = 0 };
                for (int hi = 0; hi < histRelatorios.Count; hi++)
                {
                    var hr = histRelatorios[hi];
                    bool isLast = hi == histRelatorios.Count - 1;
                    histUsers.TryGetValue(hr.UsuarioId, out var hUser);

                    var (dotColor, titulo) = (hr.Rascunho, hr.Revisao) switch
                    {
                        (true,  _) => (Color.FromArgb(255, 240, 165, 0),  $"Rascunho iniciado — RDO nº {hr.Numero:D3}"),
                        (false, 0) => (Color.FromArgb(255, 0, 163, 108),  $"RDO nº {hr.Numero:D3} publicado"),
                        _          => (Color.FromArgb(255, 130, 100, 210), $"RDO nº {hr.Numero:D3} — Revisão {hr.Revisao:D2}")
                    };

                    var itemGrid = new Grid { ColumnSpacing = 12 };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Coluna esquerda: dot + linha vertical
                    var dotCol = new Grid { VerticalAlignment = VerticalAlignment.Stretch };
                    var dot = new Border
                    {
                        Width = 12, Height = 12, CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                        Background = new SolidColorBrush(dotColor),
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 6, 0, 0)
                    };
                    dotCol.Children.Add(dot);
                    if (!isLast)
                    {
                        var line = new Border
                        {
                            Width = 2,
                            Background = (Brush)Application.Current.Resources["AppBorderBrush"],
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Margin = new Thickness(0, 18, 0, 0)
                        };
                        dotCol.Children.Add(line);
                    }
                    Grid.SetColumn(dotCol, 0);
                    itemGrid.Children.Add(dotCol);

                    // Coluna direita: texto
                    var textStack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 3, 0, 16) };
                    textStack.Children.Add(new TextBlock
                    {
                        Text = titulo,
                        FontSize = 13, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                        Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                        TextWrapping = TextWrapping.Wrap
                    });
                    var metaRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                    metaRow.Children.Add(new TextBlock
                    {
                        Text = hr.CriadoEm.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        FontSize = 11,
                        Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"]
                    });
                    if (hUser != null)
                    {
                        metaRow.Children.Add(new TextBlock
                        {
                            Text = "·", FontSize = 11,
                            Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"]
                        });
                        metaRow.Children.Add(new TextBlock
                        {
                            Text = hUser.Nome, FontSize = 11,
                            Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"]
                        });
                    }
                    textStack.Children.Add(metaRow);
                    Grid.SetColumn(textStack, 1);
                    itemGrid.Children.Add(textStack);

                    histStack.Children.Add(itemGrid);
                }
                root.Children.Add(histStack);
            }
            // ────────────────────────────────────────────────────────────

            var scroll = new ScrollViewer { Content = root, MaxHeight = 680, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var dialog = new ContentDialog
            {
                Title = "Propriedades da Obra",
                Content = scroll,
                PrimaryButtonText = "+ Novo RDO",
                SecondaryButtonText = "Desativar obra",
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
                                    Text = $"Ao salvar uma nova revisão será gerada: Rev. {novaRev:D2}.",
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 13,
                                    Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 165, 250))
                                },
                                new TextBlock
                                {
                                    Text = "Como deseja prosseguir?",
                                    FontSize = 13
                                }
                            }
                        },
                        PrimaryButtonText = "Nova revisão",
                        SecondaryButtonText = "Editar revisão atual",
                        CloseButtonText = "Cancelar",
                        XamlRoot = this.XamlRoot
                    };

                    var resultado = await avisoDialog.ShowAsync();
                    if (resultado == ContentDialogResult.Primary)
                        Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = obra.Id, RelatorioId = capturedRel.Id });
                    else if (resultado == ContentDialogResult.Secondary)
                        Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = obra.Id, RelatorioId = capturedRel.Id, EditarRevisaoAtual = true });
                };
            }

            // ── Handler: editar status da obra ──────────────────────────
            statusBtn.Click += async (s, ev) =>
            {
                dialog.Hide();

                var statusOpts = new[] { "Em execução", "Concluída", "Paralisada" };
                var comboStatus = new ComboBox
                {
                    ItemsSource = statusOpts,
                    SelectedItem = obra.Status,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                var statusDialog = new ContentDialog
                {
                    Title = "Alterar status da obra",
                    Content = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = obra.Nome,
                                FontSize = 13,
                                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                                TextWrapping = TextWrapping.Wrap
                            },
                            comboStatus
                        }
                    },
                    PrimaryButtonText = "Salvar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await statusDialog.ShowAsync() == ContentDialogResult.Primary
                    && comboStatus.SelectedItem is string novoStatus
                    && novoStatus != obra.Status)
                {
                    try
                    {
                        using var dbStatus = new RdoDbContext(DbContextHelper.GetOptions());
                        var obraDb = await dbStatus.Obras.FindAsync(obra.Id);
                        if (obraDb != null)
                        {
                            obraDb.Status = novoStatus;
                            obraDb.UpdatedAt = DateTime.UtcNow;
                            await dbStatus.SaveChangesAsync();
                        }
                        CarregarObras();

                        // Atualiza badge inline
                        statusBadgeText.Text = novoStatus.ToUpper();
                        statusBtn.Background = new SolidColorBrush(StatusCorFor(novoStatus));
                    }
                    catch (Exception ex)
                    {
                        await RDO.App.Services.ErrorDialogService.ShowAsync(
                            this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_002, null, ex);
                    }
                }
            };

            // ── Handler: editar obra completo ────────────────────────────
            editarObraBtn.Click += (s, ev) =>
            {
                dialog.Hide();
                Frame.Navigate(typeof(NovaObraPage), new NovaObraParams { ObraId = obra.Id, AbaOrigem = "Obras" });
            };

            var resultado = await dialog.ShowAsync();
            _dialogPropriedadesObra = null;

            if (resultado == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(RdoFormPage), obra.Id);
            }
            else if (resultado == ContentDialogResult.Secondary)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Desativar obra",
                    Content = new TextBlock
                    {
                        Text = $"Deseja realmente desativar \"{obra.Nome}\"?\n\nA obra não aparecerá mais na lista, mas seus relatórios serão preservados.",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 13
                    },
                    PrimaryButtonText = "Desativar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using var dbDes = new RdoDbContext(DbContextHelper.GetOptions());
                        var obraDb = await dbDes.Obras.FindAsync(obra.Id);
                        if (obraDb != null)
                        {
                            obraDb.Ativo = false;
                            obraDb.UpdatedAt = DateTime.UtcNow;
                            await dbDes.SaveChangesAsync();
                        }
                        CarregarObras();
                    }
                    catch (Exception ex)
                    {
                        await RDO.App.Services.ErrorDialogService.ShowAsync(
                            this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_002, null, ex);
                    }
                }
            }
        }

        private async Task MostrarVisualizadorFotos(
            System.Collections.Generic.List<Foto> fotos, int startIndex, string obraNome)
        {
            if (fotos.Count == 0) return;

            // Índice atual capturado num array para mutabilidade em closures
            var idx = new int[] { Math.Clamp(startIndex, 0, fotos.Count - 1) };

            // Controles internos do viewer
            var mainImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                MaxHeight = 480,
                MaxWidth  = 780
            };
            var captionText = new TextBlock
            {
                FontSize = 13, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"]
            };
            var counterText = new TextBlock
            {
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var rdoBadge = new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Background = (Brush)Application.Current.Resources["AccentBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            rdoBadge.Child = new TextBlock
            {
                FontSize = 11, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = (Brush)Application.Current.Resources["AccentFgBrush"]
            };

            void CarregarFoto(int i)
            {
                idx[0] = Math.Clamp(i, 0, fotos.Count - 1);
                var f = fotos[idx[0]];
                try
                {
                    mainImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(f.CaminhoArquivo));
                }
                catch { mainImage.Source = null; }

                captionText.Text = f.Legenda;
                captionText.Visibility = string.IsNullOrEmpty(f.Legenda) ? Visibility.Collapsed : Visibility.Visible;
                counterText.Text = $"{idx[0] + 1} / {fotos.Count}";

                if (!string.IsNullOrEmpty(f.AtividadeRelacionada))
                {
                    ((TextBlock)rdoBadge.Child).Text = f.AtividadeRelacionada;
                    rdoBadge.Visibility = Visibility.Visible;
                }
                else rdoBadge.Visibility = Visibility.Collapsed;
            }

            var btnPrev = new Button
            {
                Content = "", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 16, Width = 40, Height = 40, Padding = new Thickness(0),
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1),
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(20),
                VerticalAlignment = VerticalAlignment.Center
            };
            var btnNext = new Button
            {
                Content = "", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 16, Width = 40, Height = 40, Padding = new Thickness(0),
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1),
                Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"],
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            btnPrev.Click += (_, _) => CarregarFoto(idx[0] - 1);
            btnNext.Click += (_, _) => CarregarFoto(idx[0] + 1);
            ToolTipService.SetToolTip(btnPrev, "Foto anterior");
            ToolTipService.SetToolTip(btnNext, "Próxima foto");

            // Layout: prev | image | next
            var navGrid = new Grid { ColumnSpacing = 12 };
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(btnPrev,   0);
            Grid.SetColumn(mainImage, 1);
            Grid.SetColumn(btnNext,   2);
            navGrid.Children.Add(btnPrev);
            navGrid.Children.Add(mainImage);
            navGrid.Children.Add(btnNext);

            var content = new StackPanel { Spacing = 10, Width = 860 };
            content.Children.Add(navGrid);
            content.Children.Add(rdoBadge);
            content.Children.Add(captionText);
            content.Children.Add(counterText);

            CarregarFoto(idx[0]);

            var dialog = new ContentDialog
            {
                Title = $"Fotos — {obraNome}",
                Content = content,
                CloseButtonText = "Fechar",
                XamlRoot = this.XamlRoot
            };
            dialog.Resources["ContentDialogMaxWidth"] = 960.0;
            dialog.Resources["ContentDialogMinWidth"] = 860.0;

            // Navegação por teclado
            dialog.KeyDown += (_, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Left)  { CarregarFoto(idx[0] - 1); e.Handled = true; }
                if (e.Key == Windows.System.VirtualKey.Right) { CarregarFoto(idx[0] + 1); e.Handled = true; }
            };

            await dialog.ShowAsync();
        }

        private void BuscaRel_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltrosRelatorios();

        private async void ExportarCsv_Click(object sender, RoutedEventArgs e)
        {
            var lista = MeusRelatoriosListView?.ItemsSource as System.Collections.Generic.List<MeuRelatorioViewModel>;
            if (lista == null || lista.Count == 0)
            {
                var d = new ContentDialog
                {
                    Title = "Exportar CSV",
                    Content = "Nenhum relatório para exportar. Ajuste os filtros e tente novamente.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await d.ShowAsync();
                return;
            }

            // Escolhe onde salvar
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV", new System.Collections.Generic.List<string> { ".csv" });
            savePicker.SuggestedFileName = $"Relatorios_{DateTime.Now:yyyyMMdd_HHmm}";

            // WinUI 3: associa o picker à janela
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file == null) return;

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Número;Obra;Grupo;Status Obra;Data;Responsável;Status RDO;Revisão;Funcionários");
                foreach (var r in lista)
                {
                    sb.AppendLine(string.Join(";",
                        Csv(r.NumeroFormatado),
                        Csv(r.ObraNome),
                        Csv(r.ObraGrupo),
                        Csv(r.ObraStatus),
                        Csv(r.DataFormatada),
                        Csv(r.Responsavel),
                        Csv(r.Status),
                        r.Revisao.ToString(),
                        Csv(r.FuncionariosPresentes)));
                }

                await Windows.Storage.FileIO.WriteTextAsync(file, sb.ToString(),
                    Windows.Storage.Streams.UnicodeEncoding.Utf8);

                var ok = new ContentDialog
                {
                    Title = "Exportação concluída",
                    Content = $"{lista.Count} relatório(s) exportado(s) para:\n{file.Path}",
                    PrimaryButtonText = "Abrir arquivo",
                    CloseButtonText = "Fechar",
                    XamlRoot = this.XamlRoot
                };
                if (await ok.ShowAsync() == ContentDialogResult.Primary)
                    await Windows.System.Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex)
            {
                await RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.IO_001, null, ex);
            }

            static string Csv(string? v)
            {
                if (string.IsNullOrEmpty(v)) return "";
                if (v.Contains(';') || v.Contains('"') || v.Contains('\n'))
                    return $"\"{v.Replace("\"", "\"\"")}\"";
                return v;
            }
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
        {
            var nome = LocalSettingsService.Get<string>("NomeUsuario") ?? "?";
            AppLogger.LogInfo("AUTH", $"Logout: {nome}");
            Frame.Navigate(typeof(LoginPage));
        }

        private void BtnCadastros_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(CadastrosPage));

        private void BtnConfiguracoes_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(SettingsPage));

        private void BtnRascunhos_Click(object sender, RoutedEventArgs e)
        {
            _mostraMeusRelatorios = false;
            _mostraRascunhos = true;
            AtualizarMenuAtivo();
            CarregarRascunhos();
        }

        private async void CarregarRascunhos()
        {
            try
            {
            _todosRascunhos = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var rels = db.Relatorios.Where(r => r.Rascunho && !r.IsDeleted).OrderByDescending(r => r.CriadoEm).ToList();

                var obraIds   = rels.Select(r => r.ObraId).Distinct().ToList();
                var usuIds    = rels.Select(r => r.UsuarioId).Distinct().ToList();
                var obrasDict = db.Obras.Where(o => obraIds.Contains(o.Id)).ToDictionary(o => o.Id);
                var usersDict = db.Usuarios.Where(u => usuIds.Contains(u.Id)).ToDictionary(u => u.Id);
                var contagemPorObra = db.Relatorios
                    .Where(r => !r.Rascunho && obraIds.Contains(r.ObraId))
                    .GroupBy(r => r.ObraId)
                    .ToDictionary(g => g.Key, g => g.Count());

                return rels.Select(r =>
                {
                    obrasDict.TryGetValue(r.ObraId, out var obra);
                    usersDict.TryGetValue(r.UsuarioId, out var usuario);
                    var obraNome = obra?.Nome ?? "—";
                    var criador  = usuario?.Nome ?? "—";
                    var numero   = contagemPorObra.GetValueOrDefault(r.ObraId, 0) + 1;
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
                }).ToList();
            });

            PopularFiltrosRascunhos();
            AplicarFiltrosRascunhos();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RASCUNHOS] Erro ao carregar: {ex}");
                _ = RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_001, null, ex);
            }
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
                {
                    AppLogger.LogInfo("PDF", $"PDF gerado: relatorioId={relatorioId}  path={caminho}");
                    await Windows.System.Launcher.LaunchFileAsync(
                        await Windows.Storage.StorageFile.GetFileFromPathAsync(caminho));
                }
            }
            catch (Exception)
            {
                // Fecha o dialog pai (Propriedades da Obra) se estiver aberto,
                // pois WinUI 3 não permite dois ContentDialogs simultâneos.
                _dialogPropriedadesObra?.Hide();
                _dialogPropriedadesObra = null;

                await RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.PDF_001);
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
                            Text = $"Ao salvar uma nova revisão será gerada: Rev. {novaRev:D2}.",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 13,
                            FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 96, 165, 250))
                        },
                        new TextBlock
                        {
                            Text = "Como deseja prosseguir?",
                            FontSize = 13,
                            Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"]
                        }
                    }
                },
                PrimaryButtonText = "Nova revisão",
                SecondaryButtonText = "Editar revisão atual",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            var resultado = await avisoDialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary)
                Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = rel.ObraId, RelatorioId = rel.Id });
            else if (resultado == ContentDialogResult.Secondary)
                Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = rel.ObraId, RelatorioId = rel.Id, EditarRevisaoAtual = true });
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
                    await RDO.App.Services.ErrorDialogService.ShowAsync(this.XamlRoot, RDO.App.Services.AppErrorCodes.DB_003, null, ex);
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
            if (LocalSettingsService.ContainsKey("FuncionarioVinculadoId"))
                atualId = LocalSettingsService.Get<int?>("FuncionarioVinculadoId");

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
                LocalSettingsService.Set("FuncionarioVinculadoId", funcId);
                var nomeCompleto = funcionarios.First(f => f.Id == funcId).Nome;
                NomeUsuarioTexto.Text = AbreviarNome(nomeCompleto);
                PerfilUsuarioTexto.Text = "Vinculado";
            }
            else if (result == ContentDialogResult.Secondary)
            {
                LocalSettingsService.Remove("FuncionarioVinculadoId");
                NomeUsuarioTexto.Text = "Usuário";
                PerfilUsuarioTexto.Text = "Sem vínculo";
            }
        }
    }
}
