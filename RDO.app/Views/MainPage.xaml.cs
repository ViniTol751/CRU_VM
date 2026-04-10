using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RDO.Data.Data;
using RDO.Data.Models;
using RDO.app.Services;
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
        public string ART { get; set; } = "";
        public DateTime DataInicio { get; set; }
        public DateTime? PrevisaoTermino { get; set; }
        public string? ImagemPath { get; set; }
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

    public class MeuRelatorioViewModel
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNome { get; set; } = "";
        public string DataFormatada { get; set; } = "";
        public string NumeroFormatado { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public sealed partial class MainPage : Page
    {
        internal static bool ShowMeusRelatoriosOnNavigate { get; set; }

        private bool _apenasMinhas = false;
        private bool _mostraMeusRelatorios = false;
        private List<ObraViewModel> _todasObras = new();

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
                CarregarMeusRelatorios();
            }
            else
            {
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
                AtualizarSyncUI(SyncEstado.Erro, resultado.Error ?? "Erro");
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
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
            => await SincronizarAsync();

        private void CarregarNomeVinculado()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("FuncionarioVinculadoId")) return;
            var funcId = (int?)ApplicationData.Current.LocalSettings.Values["FuncionarioVinculadoId"];
            if (funcId == null) return;
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var func = db.Funcionarios.Find(funcId.Value);
            if (func != null)
            {
                NomeUsuarioTexto.Text = AbreviarNome(func.Nome);
                PerfilUsuarioTexto.Text = "Vinculado";
            }
        }

        private void CarregarObras()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var obras = db.Obras.Where(o => o.Ativo).ToList();

            _todasObras = obras.Select(o => new ObraViewModel
            {
                Id = o.Id,
                Nome = o.Nome,
                Grupo = o.Grupo,
                Endereco = o.Endereco,
                Status = o.Status,
                Responsavel = o.Responsavel,
                Contratante = o.Contratante,
                ART = o.ART,
                DataInicio = o.DataInicio,
                PrevisaoTermino = o.PrevisaoTermino,
                ImagemPath = o.ImagemPath,
                QtdRdos = db.Relatorios.Count(r => r.ObraId == o.Id && !r.Rascunho),
                TemRascunho = db.Relatorios.Any(r => r.ObraId == o.Id && r.Rascunho)
            }).ToList();

            AplicarFiltros();
        }

        private void CarregarMeusRelatorios()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            int? funcId = null;
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("FuncionarioVinculadoId"))
                funcId = (int?)ApplicationData.Current.LocalSettings.Values["FuncionarioVinculadoId"];

            var relatorios = db.Relatorios
                .Where(r => !r.Rascunho && (
                    r.UsuarioId == 1 ||
                    (funcId.HasValue && r.Assinaturas.Any(a => a.FuncionarioId == funcId.Value))
                ))
                .OrderByDescending(r => r.Data)
                .ToList();

            var lista = relatorios.Select(r => new MeuRelatorioViewModel
            {
                Id = r.Id,
                ObraId = r.ObraId,
                ObraNome = db.Obras.Find(r.ObraId)?.Nome ?? "—",
                DataFormatada = r.Data.ToString("dd/MM/yyyy"),
                NumeroFormatado = $"RDO nº {r.Numero:D3}",
                Status = r.Status
            }).ToList();

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
            ObrasItemsControl.ItemsSource = lista;
            TotalObrasTexto.Text = lista.Count.ToString();
        }

        private void AtualizarMenuAtivo()
        {
            var cor = new SolidColorBrush(ThemeManager.Current == Microsoft.UI.Xaml.ElementTheme.Dark
                ? Color.FromArgb(30, 0, 200, 160)
                : Color.FromArgb(30, 0, 82, 204));
            var transp = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            BtnInicio.Background = (!_apenasMinhas && !_mostraMeusRelatorios) ? cor : transp;
            BtnMinhasObras.Background = _mostraMeusRelatorios ? cor : transp;

            // Mostra/esconde painéis
            ObrasPanel.Visibility = _mostraMeusRelatorios ? Visibility.Collapsed : Visibility.Visible;
            RelatoriosPanel.Visibility = _mostraMeusRelatorios ? Visibility.Visible : Visibility.Collapsed;
            FiltroBar.Visibility = _mostraMeusRelatorios ? Visibility.Collapsed : Visibility.Visible;

            if (_mostraMeusRelatorios)
            {
                TituloPagina.Text = "Meus relatórios";
                SubtituloLista.Text = "Relatórios criados por você ou com sua participação";
                BtnNovaObra.Visibility = Visibility.Collapsed;
            }
            else
            {
                TituloPagina.Text = "Início";
                SubtituloLista.Text = "Todas as obras";
                BtnNovaObra.Visibility = Visibility.Visible;
            }
        }

        private void AtualizarIconeTema()
        {
            if (BtnTema == null) return;
            BtnTema.Content = ThemeManager.Current == Microsoft.UI.Xaml.ElementTheme.Dark
                ? "\uE706"  // sol
                : "\uE708"; // lua
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
                    Status = r.Status
                }).ToList();

            // ── Layout: duas colunas de info ──
            var root = new StackPanel { Spacing = 16, Width = 1060 };

            // Nome + grupo
            root.Children.Add(new TextBlock { Text = obra.Nome, FontSize = 20, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"], TextWrapping = TextWrapping.Wrap });
            root.Children.Add(new TextBlock { Text = obra.Grupo, FontSize = 13, Foreground = (Brush)Application.Current.Resources["AccentBrush"] });

            if (obra.TemRascunho)
            {
                var avisoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                avisoRow.Children.Add(new FontIcon { Glyph = "\uE7BA", FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)) });
                avisoRow.Children.Add(new TextBlock { Text = "Há um rascunho salvo para esta obra.", FontSize = 12, Foreground = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)), VerticalAlignment = VerticalAlignment.Center });
                root.Children.Add(new Border { Background = new SolidColorBrush(Color.FromArgb(255, 42, 31, 0)), BorderBrush = new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)), BorderThickness = new Thickness(1), CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6), Padding = new Thickness(10), Child = avisoRow });
            }

            // Info em grid 2 colunas
            var infoGrid = new Grid { ColumnSpacing = 16, RowSpacing = 10 };
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var infoItems = new[]
            {
                ("ENDEREÇO", obra.Endereco),
                ("RESPONSÁVEL FOCUS", obra.Responsavel),
                ("RESP. CLIENTE", obra.Contratante),
                ("ART / RRT", obra.ART),
                ("PERÍODO", $"{obra.DataInicio:dd/MM/yyyy}" + (obra.PrevisaoTermino.HasValue ? $" → {obra.PrevisaoTermino:dd/MM/yyyy}" : ""))
            };

            for (int i = 0; i < infoItems.Length; i++)
            {
                var (lbl, val) = infoItems[i];
                int row = i / 2;
                int col = i % 2;
                if (infoGrid.RowDefinitions.Count <= row)
                    infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var sp = new StackPanel { Spacing = 2 };
                sp.Children.Add(new TextBlock { Text = lbl, FontSize = 9, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"], CharacterSpacing = 100 });
                sp.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(val) ? "—" : val, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"], TextWrapping = TextWrapping.Wrap });
                Grid.SetRow(sp, row);
                Grid.SetColumn(sp, col);
                if (i == infoItems.Length - 1 && infoItems.Length % 2 != 0)
                    Grid.SetColumnSpan(sp, 2);
                infoGrid.Children.Add(sp);
            }

            root.Children.Add(new Border { Background = (Brush)Application.Current.Resources["AppBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6), Padding = new Thickness(16, 14, 16, 14), Child = infoGrid });

            // ── Separador + cabeçalho da lista ──
            root.Children.Add(new Border { Height = 1, Background = (Brush)Application.Current.Resources["AppBorderBrush"], Margin = new Thickness(0, 4, 0, 4) });
            var relHeader = new Grid();
            relHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            relHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var relHeaderTitle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
            relHeaderTitle.Children.Add(new TextBlock { Text = "RELATÓRIOS PUBLICADOS", FontSize = 10, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"], CharacterSpacing = 120, VerticalAlignment = VerticalAlignment.Center });
            relHeaderTitle.Children.Add(new Border { Background = (Brush)Application.Current.Resources["AccentBrush"], CornerRadius = new Microsoft.UI.Xaml.CornerRadius(10), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = relatoriosBd.Count.ToString(), FontSize = 11, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["AccentFgBrush"] } });
            Grid.SetColumn(relHeaderTitle, 0);
            relHeader.Children.Add(relHeaderTitle);
            root.Children.Add(relHeader);

            // Coleta de botões que precisam da referência do dialog
            var botoesEditar = new List<(Button btn, MeuRelatorioViewModel rel)>();
            StackPanel? listaPanel = null;

            if (relatoriosBd.Count == 0)
            {
                root.Children.Add(new TextBlock { Text = "Nenhum relatório publicado ainda.", FontSize = 12, FontStyle = Windows.UI.Text.FontStyle.Italic, Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"] });
            }
            else
            {
                listaPanel = new StackPanel { Spacing = 6 };

                foreach (var rel in relatoriosBd)
                {
                    var capturedRel = rel;
                    var itemBorder = new Border
                    {
                        Background = (Brush)Application.Current.Resources["AppBgBrush"],
                        BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                        BorderThickness = new Thickness(1),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                        Padding = new Thickness(14, 10, 14, 10)
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
                    var infoTexts = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
                    infoTexts.Children.Add(new TextBlock { Text = rel.NumeroFormatado, FontSize = 14, FontWeight = new Windows.UI.Text.FontWeight { Weight = 700 }, Foreground = (Brush)Application.Current.Resources["TextPrimaryBrush"] });
                    infoTexts.Children.Add(new TextBlock { Text = rel.DataFormatada, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextTertiaryBrush"] });
                    infoRel.Children.Add(infoTexts);
                    infoRel.Children.Add(statusBadge);

                    var btnEditar = new Button { Content = "\uE70F", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 14, Width = 36, Height = 36, Padding = new Thickness(0), Background = (Brush)Application.Current.Resources["EditBtnBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), Foreground = (Brush)Application.Current.Resources["AccentBrush"], VerticalAlignment = VerticalAlignment.Center };
                    ToolTipService.SetToolTip(btnEditar, "Editar relatório");
                    botoesEditar.Add((btnEditar, capturedRel));

                    var btnExportar = new Button { Content = "\uE8C8", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 14, Width = 36, Height = 36, Padding = new Thickness(0), Background = (Brush)Application.Current.Resources["EditBtnBgBrush"], BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"], BorderThickness = new Thickness(1), Foreground = (Brush)Application.Current.Resources["AccentBrush"], VerticalAlignment = VerticalAlignment.Center, Tag = capturedRel.Id };
                    ToolTipService.SetToolTip(btnExportar, "Exportar PDF");
                    btnExportar.Click += BtnExportarPdfItem_Click;

                    var btnExcluir = new Button { Content = "\uE74D", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets"), FontSize = 13, Width = 36, Height = 36, Padding = new Thickness(0), Background = new SolidColorBrush(Color.FromArgb(255, 38, 16, 16)), BorderBrush = new SolidColorBrush(Color.FromArgb(255, 90, 30, 30)), BorderThickness = new Thickness(1), Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)), VerticalAlignment = VerticalAlignment.Center };
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
                Title = obra.Nome,
                Content = scroll,
                PrimaryButtonText = "+ Novo RDO",
                CloseButtonText = "Fechar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            // WinUI 3: sobrescreve o MaxWidth padrão (~548 px) para permitir modal mais largo
            dialog.Resources["ContentDialogMaxWidth"] = 1160.0;
            dialog.Resources["ContentDialogMinWidth"] = 960.0;

            foreach (var (btn, rel) in botoesEditar)
            {
                var capturedRel = rel;
                btn.Click += (s, ev) =>
                {
                    dialog.Hide();
                    Frame.Navigate(typeof(RdoFormPage), new RdoFormParams { ObraId = obra.Id, RelatorioId = capturedRel.Id });
                };
            }

            var resultado = await dialog.ShowAsync();
            if (resultado == ContentDialogResult.Primary)
                Frame.Navigate(typeof(RdoFormPage), obra.Id);
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
            CarregarObras();
            AtualizarMenuAtivo();
        }

        private void BtnMinhasObras_Click(object sender, RoutedEventArgs e)
        {
            _mostraMeusRelatorios = true;
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
            => Frame.Navigate(typeof(RascunhosPage));

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            AtualizarMenuAtivo();
            AtualizarIconeTema();
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
