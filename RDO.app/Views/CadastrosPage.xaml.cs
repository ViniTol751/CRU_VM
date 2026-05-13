using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RDO.App.Services;
using RDO.app.Services;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Empresa = RDO.Data.Models.Empresa;

namespace RDO.App.Views
{
    public class AcompanhanteListItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = "";
        public string Cargo { get; set; } = "";
        public string Grupo { get; set; } = "";
        public string Contato { get; set; } = "";
        public bool Ativo { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNome { get; set; }
    }

    public class NovaObraParams
    {
        public int? ObraId { get; set; }
        public string AbaOrigem { get; set; } = "Obras";
    }

    public sealed partial class CadastrosPage : Page
    {
        // Sinaliza que um sync ocorreu e a próxima visita deve forçar reload de todas as abas
        internal static bool PendingRefresh { get; set; }

        private const string EmpresaPadrao = "Focus Engenharia Elétrica";
        private CadastrosParams? _params;
        private string _abaAtual = "Obras";
        private readonly HashSet<string> _abasCarregadas = new();

        // Direção de ordenação por aba (true = A→Z)
        private bool _sortAscEmpresas     = true;
        private bool _sortAscObras        = true;
        private bool _sortAscFuncionarios = true;
        private bool _sortAscEquipamentos = true;
        private bool _sortAscAcomp        = true;

        public CadastrosPage()
        {
            this.InitializeComponent();
            MostrarAba("Obras");
        }

        protected override void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            string alvo;
            if (e.Parameter is CadastrosParams cp)
            {
                _params = cp;
                var a = cp.AbaInicial == "Terceiros" ? "Acompanhantes" : cp.AbaInicial;
                alvo = string.IsNullOrEmpty(a) ? "Obras" : a;
            }
            else if (e.Parameter is string abaStr)
            {
                _params = null;
                alvo = abaStr;
            }
            else
            {
                _params = null;
                alvo = "Obras";
            }

            if (alvo != _abaAtual)
                MostrarAba(alvo);
            else
                RecarregarAbaAtual();
        }

        // Chamado pelo MainPage após sync bem-sucedido quando esta página já está ativa
        public void RefreshFromSync() => RecarregarAbaAtual();

        // Força recarga da aba visível (após CRUD global)
        private void RecarregarAbaAtual()
        {
            _abasCarregadas.Remove(_abaAtual);
            CarregarAbaSeNecessario(_abaAtual);
        }

        private void CarregarAbaSeNecessario(string aba)
        {
            // Sync ocorreu enquanto esta página estava em memória — descarta cache de todas as abas
            if (PendingRefresh)
            {
                _abasCarregadas.Clear();
                PendingRefresh = false;
            }
            if (_abasCarregadas.Contains(aba)) return;
            _abasCarregadas.Add(aba);
            switch (aba)
            {
                case "Empresas":      _ = FiltrarEmpresasAsync(BuscaEmpresasBox?.Text ?? "");      break;
                case "Obras":         _ = FiltrarObrasAsync(BuscaObrasBox?.Text ?? "");            break;
                case "Funcionarios":  _ = FiltrarFuncionariosAsync(BuscaFuncionariosBox?.Text ?? ""); break;
                case "Equipamentos":  _ = FiltrarEquipamentosAsync(BuscaEquipamentosBox?.Text ?? ""); break;
                case "Acompanhantes": _ = FiltrarAcompanhantesAsync(BuscaAcompanhantesBox?.Text ?? ""); break;
            }
        }

        private void MostrarAba(string aba)
        {
            _abaAtual = aba;
            PainelEmpresas.Visibility = aba == "Empresas" ? Visibility.Visible : Visibility.Collapsed;
            PainelFuncionarios.Visibility = aba == "Funcionarios" ? Visibility.Visible : Visibility.Collapsed;
            PainelEquipamentos.Visibility = aba == "Equipamentos" ? Visibility.Visible : Visibility.Collapsed;
            PainelAcompanhantes.Visibility = aba == "Acompanhantes" ? Visibility.Visible : Visibility.Collapsed;
            PainelObras.Visibility = aba == "Obras" ? Visibility.Visible : Visibility.Collapsed;

            var cor = (Brush)Application.Current.Resources["AccentBrush"];
            var transp = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var muted = (Brush)Application.Current.Resources["TextTertiaryBrush"];

            BtnAbaEmpresas.BorderBrush = aba == "Empresas" ? cor : transp;
            BtnAbaEmpresas.BorderThickness = aba == "Empresas" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            BtnAbaEmpresas.Foreground = aba == "Empresas" ? cor : muted;

            BtnAbaObras.BorderBrush = aba == "Obras" ? cor : transp;
            BtnAbaObras.BorderThickness = aba == "Obras" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            BtnAbaObras.Foreground = aba == "Obras" ? cor : muted;

            BtnAbaFuncionarios.BorderBrush = aba == "Funcionarios" ? cor : transp;
            BtnAbaFuncionarios.BorderThickness = aba == "Funcionarios" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            BtnAbaFuncionarios.Foreground = aba == "Funcionarios" ? cor : muted;

            BtnAbaEquipamentos.BorderBrush = aba == "Equipamentos" ? cor : transp;
            BtnAbaEquipamentos.BorderThickness = aba == "Equipamentos" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            BtnAbaEquipamentos.Foreground = aba == "Equipamentos" ? cor : muted;

            BtnAbaAcompanhantes.BorderBrush = aba == "Acompanhantes" ? cor : transp;
            BtnAbaAcompanhantes.BorderThickness = aba == "Acompanhantes" ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            BtnAbaAcompanhantes.Foreground = aba == "Acompanhantes" ? cor : muted;

            AtualizarBannerSemEmpresas();
            CarregarAbaSeNecessario(aba);
        }

        // ── EMPRESAS ──────────────────────────────────────────────────────────
        private async Task FiltrarEmpresasAsync(string termo)
        {
            var cfg = LogosConfig.Load();
            var asc = _sortAscEmpresas;
            var lista = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var q = db.Empresas.Where(e => e.IsActive).ToList();
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    var t = termo.ToLower();
                    q = q.Where(e => e.Nome.ToLower().Contains(t)).ToList();
                }
                return asc ? q.OrderBy(e => e.Nome).ToList() : q.OrderByDescending(e => e.Nome).ToList();
            });

            EmpresasListView.ItemsSource = lista;
            EmpresasCountText.Text = $"{lista.Count} registro(s)";

            // Logos em segundo plano — escaneia NAS uma única vez
            var nasFiles = await LogoService.GetNasFilesAsync(cfg);
            await Task.Run(() =>
            {
                foreach (var e in lista)
                    e.LogoUrl = LogoService.ResolveLogoUrlFast(cfg, e.ImagemPath, e.Nome, nasFiles);
            });
            EmpresasListView.ItemsSource = null;
            EmpresasListView.ItemsSource = lista;
        }

        private async void BuscaEmpresas_TextChanged(object sender, TextChangedEventArgs e)
        {
            await FiltrarEmpresasAsync(BuscaEmpresasBox.Text);
            LimparBuscaEmpresasBtn.Visibility =
                string.IsNullOrEmpty(BuscaEmpresasBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaEmpresas_Click(object sender, RoutedEventArgs e)
        {
            BuscaEmpresasBox.Text = "";
            LimparBuscaEmpresasBtn.Visibility = Visibility.Collapsed;
        }

        private async void SortEmpresas_Click(object sender, RoutedEventArgs e)
        {
            _sortAscEmpresas = !_sortAscEmpresas;
            SortEmpresasBtn.Content = _sortAscEmpresas ? "A↑" : "Z↓";
            ToolTipService.SetToolTip(SortEmpresasBtn, _sortAscEmpresas ? "Ordenar A → Z" : "Ordenar Z → A");
            await FiltrarEmpresasAsync(BuscaEmpresasBox?.Text ?? "");
        }

        private async void AdicionarEmpresa_Click(object sender, RoutedEventArgs e)
            => await AbrirModalEmpresa(null);

        private async void EditarEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Empresa empresa)
                await AbrirModalEmpresa(empresa);
        }

        private async void ExcluirEmpresa_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Empresa empresa) return;
            if (await ConfirmarExclusao(empresa.Nome))
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var item = await db.Empresas.FindAsync(empresa.Id);
                    if (item != null) { item.IsActive = false; item.UpdatedAt = SyncService.GetPushTimestamp(); await db.SaveChangesAsync(); }
                    _ = FiltrarEmpresasAsync(BuscaEmpresasBox?.Text ?? "");
                    AtualizarBannerSemEmpresas();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMPRESA] Erro ao excluir: {ex}");
                    await MostrarErro(AppErrorCodes.DB_003, ex);
                }
            }
        }

        private async Task AbrirModalEmpresa(Empresa? existente)
        {
            var nomeBox = new TextBox { PlaceholderText = "Nome da empresa" };
            string? logoPathSelecionado = existente?.ImagemPath;

            var previewBorder = new Microsoft.UI.Xaml.Controls.Border
            {
                Width = 80, Height = 80,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                Background = (Brush)Application.Current.Resources["AccentSubtleBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1)
            };
            var previewGrid = new Grid();
            var previewIcon = new FontIcon
            {
                Glyph = "\uE731", FontSize = 28,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var previewImg = new Image { Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
            previewGrid.Children.Add(previewIcon);
            previewGrid.Children.Add(previewImg);
            previewBorder.Child = previewGrid;

            // Load existing logo if editing: local first, then NAS
            if (existente != null)
            {
                var cfg = LogosConfig.Load();
                var logoUrl = LogoService.ResolveLogoUrl(cfg, existente.ImagemPath, existente.Nome);
                if (!string.IsNullOrEmpty(logoUrl))
                {
                    if (logoUrl.StartsWith(@"\\"))
                        previewImg.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                            new Uri("file:" + logoUrl.Replace('\\', '/')));
                    else
                        previewImg.Source = await ImageHelper.CarregarAsync(logoUrl);
                    previewIcon.Visibility = Visibility.Collapsed;
                }
            }

            var logoNomeTexto = new TextBlock
            {
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                TextTrimming = TextTrimming.CharacterEllipsis,
                Text = existente?.ImagemPath != null ? Path.GetFileName(existente.ImagemPath) : "Nenhum arquivo selecionado"
            };

            var btnSelecionarLogo = new Button
            {
                Content = "Selecionar logo (.PNG)",
                Height = 36,
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1)
            };
            btnSelecionarLogo.Click += async (s, ev) =>
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                    (Application.Current as App)?.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add(".png");
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    logoPathSelecionado = file.Path;
                    logoNomeTexto.Text = file.Name;
                    previewImg.Source = await ImageHelper.CarregarAsync(file.Path);
                    previewIcon.Visibility = Visibility.Collapsed;
                }
            };

            var avisoTexto = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 80, 80)),
                FontSize = 12,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            if (existente != null)
            {
                nomeBox.Text = existente.Nome;
            }

            var logoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };            logoRow.Children.Add(previewBorder);
            var logoInfo = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            logoInfo.Children.Add(btnSelecionarLogo);
            logoInfo.Children.Add(logoNomeTexto);
            logoRow.Children.Add(logoInfo);

            var logoSection = new StackPanel { Spacing = 6 };
            logoSection.Children.Add(new TextBlock
            {
                Text = "LOGO DA EMPRESA",
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                CharacterSpacing = 150
            });
            logoSection.Children.Add(logoRow);

            var form = new StackPanel { Spacing = 16, Width = 480 };
            form.Children.Add(CriarCampo("NOME *", nomeBox));
            form.Children.Add(logoSection);
            form.Children.Add(avisoTexto);

            var dialog = new ContentDialog
            {
                Title = existente == null ? "Nova empresa" : "Editar empresa",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nomeBox.Text))
                {
                    avisoTexto.Text = "O nome é obrigatório.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                string? imagemDestino = existente?.ImagemPath;

                if (logoPathSelecionado != null && logoPathSelecionado != existente?.ImagemPath)
                {
                    try
                    {
                        var pastaEmpresas = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "RDOApp", "Imagens", "Empresas");
                        Directory.CreateDirectory(pastaEmpresas);
                        var destino = Path.Combine(pastaEmpresas, Path.GetFileName(logoPathSelecionado));
                        File.Copy(logoPathSelecionado, destino, overwrite: true);
                        imagemDestino = destino;
                    }
                    catch (IOException ex)
                    {
                        await MostrarErro(AppErrorCodes.IO_001, ex);
                        return;
                    }
                }

                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    if (existente != null)
                    {
                        var item = await db.Empresas.FindAsync(existente.Id);
                        if (item != null)
                        {
                            item.Nome = nomeBox.Text.Trim();
                            if (imagemDestino != null) item.ImagemPath = imagemDestino;
                            item.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        db.Empresas.Add(new Empresa
                        {
                            Nome = nomeBox.Text.Trim(),
                            ImagemPath = imagemDestino,
                            IsActive = true,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    await db.SaveChangesAsync();
                    AppLogger.LogInfo("DB", $"Empresa salva: \"{nomeBox.Text.Trim()}\"");
                    RecarregarAbaAtual();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMPRESA] Erro ao salvar: {ex}");
                    await MostrarErro(AppErrorCodes.DB_002, ex);
                }
            }
        }

        private void AtualizarBannerSemEmpresas()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var temEmpresas = db.Empresas.Any(e => e.IsActive);
            BannerSemEmpresas.Visibility = temEmpresas ? Visibility.Collapsed : Visibility.Visible;
        }

        // ── OBRAS ─────────────────────────────────────────────────────────────────
        private async Task FiltrarObrasAsync(string termo)
        {
            var asc = _sortAscObras;
            var lista = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var q = db.Obras.Where(o => o.IsActive).ToList();
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    var t = termo.ToLower();
                    q = q.Where(o =>
                        o.Nome.ToLower().Contains(t) ||
                        o.Grupo.ToLower().Contains(t) ||
                        o.Contratante.ToLower().Contains(t) ||
                        o.Responsavel.ToLower().Contains(t)).ToList();
                }
                // Ordena pelo padrão CS-NNN-AA: primeiro pelo ano (2 dígitos), depois pelo número
                // Ex: CS-001-25 < CS-025-25 < CS-001-26 < CS-099-26
                return asc
                    ? q.OrderBy(o => ExtrairChaveContrato(o.Nome)).ToList()
                    : q.OrderByDescending(o => ExtrairChaveContrato(o.Nome)).ToList();
            });
            ObrasListViewCadastro.ItemsSource = lista;
            ObrasCountText.Text = $"{lista.Count} registro(s)";
        }

        /// <summary>
        /// Extrai uma chave de ordenação numérica do nome de obra no formato PREFIXO-NNN-AA.
        /// Retorna (ano * 10000 + número) para ordenação cronológica correta.
        /// Obras fora do padrão ficam no final com chave int.MaxValue.
        /// </summary>
        private static int ExtrairChaveContrato(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return int.MaxValue;
            // Espera formato: QUALQUER-NNN-AA  (ex: CS-001-26, RDO-025-25)
            var partes = nome.Trim().Split('-');
            if (partes.Length >= 3
                && int.TryParse(partes[^1], out var ano)
                && int.TryParse(partes[^2], out var num))
            {
                return ano * 10000 + num;
            }
            return int.MaxValue;
        }

        private async void BuscaObras_TextChanged(object sender, TextChangedEventArgs e)
        {
            await FiltrarObrasAsync(BuscaObrasBox.Text);
            LimparBuscaObrasBtn.Visibility =
                string.IsNullOrEmpty(BuscaObrasBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaObras_Click(object sender, RoutedEventArgs e)
        {
            BuscaObrasBox.Text = "";
            LimparBuscaObrasBtn.Visibility = Visibility.Collapsed;
        }

        private async void SortObras_Click(object sender, RoutedEventArgs e)
        {
            _sortAscObras = !_sortAscObras;
            // Ícone de calendário — tooltip indica a direção
            ToolTipService.SetToolTip(SortObrasBtn,
                _sortAscObras ? "Mais antigas primeiro (por nº contrato)" : "Mais recentes primeiro (por nº contrato)");
            await FiltrarObrasAsync(BuscaObrasBox?.Text ?? "");
        }

        private void AdicionarObra_Click(object sender, RoutedEventArgs e)
            => Frame.Navigate(typeof(NovaObraPage), new NovaObraParams { ObraId = null, AbaOrigem = "Obras" });

        private void EditarObra_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Obra o)
                Frame.Navigate(typeof(NovaObraPage), new NovaObraParams { ObraId = o.Id, AbaOrigem = "Obras" });
        }

        private async void ExcluirObra_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Obra o) return;
            if (await ConfirmarExclusao(o.Nome))
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var item = await db.Obras.FindAsync(o.Id);
                    if (item != null)
                    {
                        var ts = SyncService.GetPushTimestamp();
                        var since = SyncService.LoadLastSyncTime();
                        item.Ativo = false; item.IsDeleted = true; item.UpdatedAt = ts;
                        await db.SaveChangesAsync();
                        RDO.app.Services.SyncLogger.LogDebug($"[DELETE-OBRA] id={item.Id} updatedAt={ts:O} since={since:O} incluiNoProximoPush={ts >= since}");
                    }
                    _ = FiltrarObrasAsync(BuscaObrasBox?.Text ?? "");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OBRA] Erro ao excluir: {ex}");
                    await MostrarErro(AppErrorCodes.DB_003, ex);
                }
            }
        }

        private bool _mostraObrasInativas = false;

        private void BtnMostrarInativas_Click(object sender, RoutedEventArgs e)
        {
            _mostraObrasInativas = !_mostraObrasInativas;
            BtnMostrarInativas.Content = _mostraObrasInativas ? "Ocultar inativas" : "Mostrar inativas";
            PainelObrasInativas.Visibility = _mostraObrasInativas ? Visibility.Visible : Visibility.Collapsed;
            if (_mostraObrasInativas)
                CarregarObrasInativas();
        }

        private async void CarregarObrasInativas()
        {
            try
            {
                var lista = await Task.Run(() =>
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    // ToList() antes do OrderBy — Nome é [NotMapped] e não pode ser traduzido para SQL
                    return db.Obras
                        .Where(o => !o.IsActive && !o.IsDeleted)
                        .ToList()
                        .OrderBy(o => o.Nome)
                        .ToList();
                });
                if (ObrasInativasListView != null)
                {
                    ObrasInativasListView.ItemsSource = lista;
                    ContadorInativasTexto.Text = lista.Count > 0
                        ? $"{lista.Count} obra(s) inativa(s)"
                        : "Nenhuma obra inativa";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OBRAS INATIVAS] Erro: {ex}");
            }
        }

        private async void ReativarObra_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            int obraId;
            if (btn.Tag is Obra obra)
                obraId = obra.Id;
            else if (btn.DataContext is Obra obraCtx)
                obraId = obraCtx.Id;
            else return;

            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var item = await db.Obras.FindAsync(obraId);
                if (item != null)
                {
                    item.Ativo = true;
                    item.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
                _ = FiltrarObrasAsync(BuscaObrasBox?.Text ?? "");
                CarregarObrasInativas();
            }
            catch (Exception ex)
            {
                await MostrarErro(AppErrorCodes.DB_002, ex);
            }
        }

        // ── FUNCIONÁRIOS ──────────────────────────────────────────────────────
        private async Task FiltrarFuncionariosAsync(string termo)
        {
            var asc = _sortAscFuncionarios;
            var lista = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var q = db.Funcionarios.Where(f => f.Ativo).ToList();
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    var t = termo.ToLower();
                    q = q.Where(f =>
                        f.Nome.ToLower().Contains(t) ||
                        f.Funcao.ToLower().Contains(t) ||
                        f.Empresa.ToLower().Contains(t) ||
                        f.Contato.ToLower().Contains(t)).ToList();
                }
                return asc ? q.OrderBy(f => f.Nome).ToList() : q.OrderByDescending(f => f.Nome).ToList();
            });
            FuncionariosListView.ItemsSource = lista;
            FuncionariosCountText.Text = $"{lista.Count} registro(s)";
        }

        private async void BuscaFuncionarios_TextChanged(object sender, TextChangedEventArgs e)
        {
            await FiltrarFuncionariosAsync(BuscaFuncionariosBox.Text);
            LimparBuscaFuncionariosBtn.Visibility =
                string.IsNullOrEmpty(BuscaFuncionariosBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaFuncionarios_Click(object sender, RoutedEventArgs e)
        {
            BuscaFuncionariosBox.Text = "";
            LimparBuscaFuncionariosBtn.Visibility = Visibility.Collapsed;
        }

        private async void SortFuncionarios_Click(object sender, RoutedEventArgs e)
        {
            _sortAscFuncionarios = !_sortAscFuncionarios;
            SortFuncionariosBtn.Content = _sortAscFuncionarios ? "A↑" : "Z↓";
            ToolTipService.SetToolTip(SortFuncionariosBtn, _sortAscFuncionarios ? "Ordenar A → Z" : "Ordenar Z → A");
            await FiltrarFuncionariosAsync(BuscaFuncionariosBox?.Text ?? "");
        }

        private async void AdicionarFuncionario_Click(object sender, RoutedEventArgs e)
            => await AbrirModalFuncionario(null);

        private async void EditarFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Funcionario f)
                await AbrirModalFuncionario(f);
        }

        private async Task AbrirModalFuncionario(Funcionario? existente)
        {
            var tipoBox = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Selecione o tipo"
            };
            tipoBox.Items.Add("Próprio");
            tipoBox.Items.Add("Terceiro");

            var empresaBox = new TextBox
            {
                PlaceholderText = "Nome da empresa",
                IsEnabled = false,
                Opacity = 0.5
            };

            var nomeBox = new TextBox { PlaceholderText = "Nome completo" };
            var funcaoBox = new TextBox { PlaceholderText = "Ex: Engenheiro, Técnico..." };
            var contatoBox = new TextBox { PlaceholderText = "Telefone ou e-mail" };
            var avisoTexto = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 80, 80)),
                FontSize = 12,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            tipoBox.SelectionChanged += (s, e) =>
            {
                var tipo = tipoBox.SelectedItem?.ToString();
                if (tipo == "Próprio")
                {
                    empresaBox.Text = EmpresaPadrao;
                    empresaBox.IsEnabled = false;
                    empresaBox.Opacity = 0.6;
                }
                else if (tipo == "Terceiro")
                {
                    empresaBox.Text = "";
                    empresaBox.IsEnabled = true;
                    empresaBox.Opacity = 1;
                }
            };

            if (existente != null)
            {
                nomeBox.Text = existente.Nome;
                funcaoBox.Text = existente.Funcao;
                contatoBox.Text = existente.Contato;
                tipoBox.SelectedIndex = existente.Tipo == "Próprio" ? 0 : 1;
                empresaBox.Text = existente.Empresa;
                if (existente.Tipo == "Terceiro")
                {
                    empresaBox.IsEnabled = true;
                    empresaBox.Opacity = 1;
                }
            }

            var form = new StackPanel { Spacing = 16, Width = 640 };
            var linha1 = new Grid { ColumnSpacing = 16 };
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            var campoTipo = CriarCampo("TIPO *", tipoBox);
            var campoEmpresa = CriarCampo("EMPRESA *", empresaBox);
            Grid.SetColumn(campoTipo, 0);
            Grid.SetColumn(campoEmpresa, 1);
            linha1.Children.Add(campoTipo);
            linha1.Children.Add(campoEmpresa);
            form.Children.Add(linha1);
            form.Children.Add(CriarCampo("NOME *", nomeBox));
            form.Children.Add(CriarCampo("FUNÇÃO *", funcaoBox));
            form.Children.Add(CriarCampo("CONTATO *", contatoBox));
            form.Children.Add(avisoTexto);

            var dialog = new ContentDialog
            {
                Title = existente == null ? "Novo funcionário" : "Editar funcionário",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var erros = new System.Collections.Generic.List<string>();
                if (tipoBox.SelectedItem == null) erros.Add("Tipo");
                if (string.IsNullOrWhiteSpace(empresaBox.Text)) erros.Add("Empresa");
                if (string.IsNullOrWhiteSpace(nomeBox.Text)) erros.Add("Nome");
                if (string.IsNullOrWhiteSpace(funcaoBox.Text)) erros.Add("Função");
                if (string.IsNullOrWhiteSpace(contatoBox.Text)) erros.Add("Contato");
                if (erros.Count > 0)
                {
                    avisoTexto.Text = $"Preencha os campos obrigatórios: {string.Join(", ", erros)}.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                if (existente != null)
                {
                    var item = await db.Funcionarios.FindAsync(existente.Id);
                    if (item != null)
                    {
                        item.Nome = nomeBox.Text.Trim();
                        item.Funcao = funcaoBox.Text.Trim();
                        item.Tipo = tipoBox.SelectedItem!.ToString()!;
                        item.Empresa = empresaBox.Text.Trim();
                        item.Contato = contatoBox.Text.Trim();
                    }
                }
                else
                {
                    db.Funcionarios.Add(new Funcionario
                    {
                        Nome = nomeBox.Text.Trim(),
                        Funcao = funcaoBox.Text.Trim(),
                        Tipo = tipoBox.SelectedItem!.ToString()!,
                        Empresa = empresaBox.Text.Trim(),
                        Contato = contatoBox.Text.Trim(),
                        Ativo = true
                    });
                }
                await db.SaveChangesAsync();
                AppLogger.LogInfo("DB", $"Funcionário salvo: \"{nomeBox.Text.Trim()}\"");
                RecarregarAbaAtual();
            }
        }

        private async void ExcluirFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Funcionario f) return;
            if (await ConfirmarExclusao(f.Nome))
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var item = await db.Funcionarios.FindAsync(f.Id);
                    if (item != null) { item.Ativo = false; item.IsDeleted = true; item.UpdatedAt = SyncService.GetPushTimestamp(); await db.SaveChangesAsync(); }
                    _ = FiltrarFuncionariosAsync(BuscaFuncionariosBox?.Text ?? "");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FUNCIONARIO] Erro ao excluir: {ex}");
                    await MostrarErro(AppErrorCodes.DB_003, ex);
                }
            }
        }

        // ── EQUIPAMENTOS ─────────────────────────────────────────────────────
        private async Task FiltrarEquipamentosAsync(string termo)
        {
            var asc = _sortAscEquipamentos;
            var lista = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var duplicatas = db.EquipamentosCadastrados
                    .Where(e => e.Ativo).ToList()
                    .GroupBy(e => e.NumeroSerie.Trim().ToLower())
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderByDescending(e => e.Id).Skip(1))
                    .ToList();
                if (duplicatas.Count > 0)
                {
                    foreach (var d in duplicatas) d.Ativo = false;
                    db.SaveChanges();
                }
                var q = db.EquipamentosCadastrados.Where(e => e.Ativo).ToList();
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    var t = termo.ToLower();
                    q = q.Where(e =>
                        e.Nome.ToLower().Contains(t) ||
                        e.Fabricante.ToLower().Contains(t) ||
                        e.NumeroSerie.ToLower().Contains(t) ||
                        e.Modelo.ToLower().Contains(t)).ToList();
                }
                // Ordena pelo número numérico do patrimônio (FC-01 → FC-XX)
                return asc
                    ? q.OrderBy(e => ExtrairNumeroPatrimonio(e.NumeroSerie)).ToList()
                    : q.OrderByDescending(e => ExtrairNumeroPatrimonio(e.NumeroSerie)).ToList();
            });
            EquipamentosListView.ItemsSource = lista;
            EquipamentosCountText.Text = $"{lista.Count} registro(s)";
        }

        /// <summary>
        /// Extrai o número inteiro do patrimônio no formato FC-XX.
        /// Retorna int.MaxValue para patrimônios fora do padrão.
        /// </summary>
        private static int ExtrairNumeroPatrimonio(string serie)
        {
            if (string.IsNullOrWhiteSpace(serie)) return int.MaxValue;
            var idx = serie.LastIndexOf('-');
            if (idx >= 0 && idx < serie.Length - 1
                && int.TryParse(serie[(idx + 1)..], out var num))
                return num;
            return int.MaxValue;
        }

        private async void BuscaEquipamentos_TextChanged(object sender, TextChangedEventArgs e)
        {
            await FiltrarEquipamentosAsync(BuscaEquipamentosBox.Text);
            LimparBuscaEquipamentosBtn.Visibility =
                string.IsNullOrEmpty(BuscaEquipamentosBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaEquipamentos_Click(object sender, RoutedEventArgs e)
        {
            BuscaEquipamentosBox.Text = "";
            LimparBuscaEquipamentosBtn.Visibility = Visibility.Collapsed;
        }

        private async void SortEquipamentos_Click(object sender, RoutedEventArgs e)
        {
            _sortAscEquipamentos = !_sortAscEquipamentos;
            ToolTipService.SetToolTip(SortEquipamentosBtn,
                _sortAscEquipamentos ? "Menor patrimônio primeiro (FC-01 → FC-XX)" : "Maior patrimônio primeiro (FC-XX → FC-01)");
            await FiltrarEquipamentosAsync(BuscaEquipamentosBox?.Text ?? "");
        }

        private async void AdicionarEquipamento_Click(object sender, RoutedEventArgs e)
            => await AbrirModalEquipamento(null);

        private async void EditarEquipamento_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EquipamentoCadastrado eq)
                await AbrirModalEquipamento(eq);
        }

        private async Task AbrirModalEquipamento(EquipamentoCadastrado? existente)
        {
            var patrimonioBox = new TextBox { PlaceholderText = "Ex: FC-02" };
            var nomeBox = new TextBox { PlaceholderText = "Nome do equipamento" };
            var fabricanteBox = new TextBox { PlaceholderText = "Ex: FLUKE, MEGABRAS..." };
            var modeloBox = new TextBox { PlaceholderText = "Ex: 435II, EM-5248..." };
            var avisoTexto = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 80, 80)),
                FontSize = 12,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            if (existente != null)
            {
                patrimonioBox.Text = existente.NumeroSerie;
                nomeBox.Text = existente.Nome;
                fabricanteBox.Text = existente.Fabricante;
                modeloBox.Text = existente.Modelo;
            }

            var form = new StackPanel { Spacing = 16, Width = 560 };
            var linha1 = new Grid { ColumnSpacing = 16 };
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            var campoPatrimonio = CriarCampo("Nº PATRIMÔNIO *", patrimonioBox);
            var campoNome = CriarCampo("NOME *", nomeBox);
            Grid.SetColumn(campoPatrimonio, 0);
            Grid.SetColumn(campoNome, 1);
            linha1.Children.Add(campoPatrimonio);
            linha1.Children.Add(campoNome);
            form.Children.Add(linha1);

            var linha2 = new Grid { ColumnSpacing = 16 };
            linha2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var campoFabricante = CriarCampo("FABRICANTE", fabricanteBox);
            var campoModelo = CriarCampo("MODELO", modeloBox);
            Grid.SetColumn(campoFabricante, 0);
            Grid.SetColumn(campoModelo, 1);
            linha2.Children.Add(campoFabricante);
            linha2.Children.Add(campoModelo);
            form.Children.Add(linha2);
            form.Children.Add(avisoTexto);

            var dialog = new ContentDialog
            {
                Title = existente == null ? "Novo equipamento" : "Editar equipamento",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var erros = new System.Collections.Generic.List<string>();
                if (string.IsNullOrWhiteSpace(patrimonioBox.Text)) erros.Add("Nº Patrimônio");
                if (string.IsNullOrWhiteSpace(nomeBox.Text)) erros.Add("Nome");
                if (erros.Count > 0)
                {
                    avisoTexto.Text = $"Preencha os campos obrigatórios: {string.Join(", ", erros)}.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                }
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                if (existente != null)
                {
                    var item = await db.EquipamentosCadastrados.FindAsync(existente.Id);
                    if (item != null)
                    {
                        item.NumeroSerie = patrimonioBox.Text.Trim();
                        item.Nome = nomeBox.Text.Trim();
                        item.Fabricante = fabricanteBox.Text.Trim();
                        item.Modelo = modeloBox.Text.Trim();
                    }
                }
                else
                {
                    var serieNovo = patrimonioBox.Text.Trim().ToLower();
                    var jaExiste = db.EquipamentosCadastrados
                        .Any(e => e.Ativo && e.SerialNumber.ToLower() == serieNovo);
                    if (jaExiste)
                    {
                        var aviso = new ContentDialog
                        {
                            Title = "Patrimônio duplicado",
                            Content = $"Já existe um equipamento ativo com o número de patrimônio \"{patrimonioBox.Text.Trim()}\". Use um número diferente.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await aviso.ShowAsync();
                        return;
                    }
                    db.EquipamentosCadastrados.Add(new EquipamentoCadastrado
                    {
                        NumeroSerie = patrimonioBox.Text.Trim(),
                        Nome = nomeBox.Text.Trim(),
                        Fabricante = fabricanteBox.Text.Trim(),
                        Modelo = modeloBox.Text.Trim(),
                        Ativo = true
                    });
                }
                await db.SaveChangesAsync();
                AppLogger.LogInfo("DB", $"Equipamento salvo: \"{patrimonioBox.Text.Trim()}\" / \"{nomeBox.Text.Trim()}\"");
                RecarregarAbaAtual();
            }
        }

        private async void ExcluirEquipamento_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not EquipamentoCadastrado eq) return;
            if (await ConfirmarExclusao(eq.Nome))
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var item = await db.EquipamentosCadastrados.FindAsync(eq.Id);
                    if (item != null) { item.Ativo = false; item.IsDeleted = true; item.UpdatedAt = SyncService.GetPushTimestamp(); await db.SaveChangesAsync(); }
                    RecarregarAbaAtual();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[EQUIPAMENTO] Erro ao excluir: {ex}");
                    await MostrarErro(AppErrorCodes.DB_003, ex);
                }
            }
        }

        // ── ACOMPANHANTES ────────────────────────────────────────────────────
        private async Task FiltrarAcompanhantesAsync(string termo)
        {
            var asc = _sortAscAcomp;
            var lista = await Task.Run(() =>
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var acomps = db.Acompanhantes.Where(a => a.Ativo).ToList();
                var empDict = db.Empresas.Where(e => e.IsActive)
                    .ToDictionary(e => e.Id, e => e.Nome);
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    var t = termo.ToLower();
                    acomps = acomps.Where(a =>
                        a.Nome.ToLower().Contains(t) ||
                        a.Cargo.ToLower().Contains(t) ||
                        a.Grupo.ToLower().Contains(t)).ToList();
                }
                var items = acomps.Select(a => new AcompanhanteListItem
                {
                    Id = a.Id, Nome = a.Nome, Cargo = a.Cargo, Grupo = a.Grupo,
                    Contato = a.Contato, Ativo = a.Ativo, EmpresaId = a.EmpresaId,
                    EmpresaNome = a.EmpresaId.HasValue && empDict.TryGetValue(a.EmpresaId.Value, out var en) ? en : null
                });
                return asc ? items.OrderBy(a => a.Nome).ToList() : items.OrderByDescending(a => a.Nome).ToList();
            });
            AcompanhantesListView.ItemsSource = lista;
            AcompanhantesCountText.Text = $"{lista.Count} registro(s)";
        }

        private async void BuscaAcompanhantes_TextChanged(object sender, TextChangedEventArgs e)
        {
            await FiltrarAcompanhantesAsync(BuscaAcompanhantesBox.Text);
            LimparBuscaAcompanhantesBtn.Visibility =
                string.IsNullOrEmpty(BuscaAcompanhantesBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaAcompanhantes_Click(object sender, RoutedEventArgs e)
        {
            BuscaAcompanhantesBox.Text = "";
            LimparBuscaAcompanhantesBtn.Visibility = Visibility.Collapsed;
        }

        private async void SortAcompanhantes_Click(object sender, RoutedEventArgs e)
        {
            _sortAscAcomp = !_sortAscAcomp;
            SortAcompanhantesBtn.Content = _sortAscAcomp ? "A↑" : "Z↓";
            ToolTipService.SetToolTip(SortAcompanhantesBtn, _sortAscAcomp ? "Ordenar A → Z" : "Ordenar Z → A");
            await FiltrarAcompanhantesAsync(BuscaAcompanhantesBox?.Text ?? "");
        }

        private async void AdicionarAcompanhante_Click(object sender, RoutedEventArgs e)
            => await AbrirModalAcompanhante(null);

        private async void EditarAcompanhante_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is AcompanhanteListItem item)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var a = await db.Acompanhantes.FindAsync(item.Id);
                if (a != null) await AbrirModalAcompanhante(a);
            }
        }

        private async Task AbrirModalAcompanhante(Acompanhante? existente)
        {
            // Load active empresas
            List<Empresa> empresas;
            using (var dbEmpresas = new RdoDbContext(DbContextHelper.GetOptions()))
            {
                empresas = dbEmpresas.Empresas.Where(e => e.IsActive).OrderBy(e => e.Nome).ToList();
            }

            var nomeBox = new TextBox { PlaceholderText = "Nome completo" };
            var cargoBox = new TextBox { PlaceholderText = "Ex: Fiscal, Engenheiro..." };
            var grupoBox = new TextBox { PlaceholderText = "Ex: Ambev, Cargill..." };

            var empresaComboBox = new AutoSuggestBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Escreva para buscar uma empresa...",
                TextMemberPath = "Nome",
                DisplayMemberPath = "Nome"
            };

            var btnLimparEmpresa = new Button
            {
                Content = new FontIcon { Glyph = "", FontSize = 13 },
                Width = 40, Height = 40,
                Padding = new Thickness(0),
                Background = (Brush)Application.Current.Resources["PanelBgBrush"],
                BorderBrush = (Brush)Application.Current.Resources["AppBorderBrush"],
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top
            };
            ToolTipService.SetToolTip(btnLimparEmpresa, "Limpar empresa");

            var empresaGrid = new Grid { ColumnSpacing = 8 };
            empresaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            empresaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(empresaComboBox, 0);
            Grid.SetColumn(btnLimparEmpresa, 1);
            empresaGrid.Children.Add(empresaComboBox);
            empresaGrid.Children.Add(btnLimparEmpresa);

            Empresa? empresaSelecionada = null;

            btnLimparEmpresa.Click += (s, ev) =>
            {
                empresaComboBox.Text = "";
                empresaSelecionada = null;
                grupoBox.Text = "";
                grupoBox.IsReadOnly = false;
                empresaComboBox.IsSuggestionListOpen = false;
                empresaComboBox.Focus(FocusState.Programmatic);
            };

            // Mostrar toda a lista quando recebe foco e está vazio
            empresaComboBox.GotFocus += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(empresaComboBox.Text))
                {
                    empresaComboBox.ItemsSource = empresas;
                }
            };

            empresaComboBox.TextChanged += (s, ev) =>
            {
                if (ev.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                {
                    var termo = empresaComboBox.Text.ToLower();
                    if (string.IsNullOrWhiteSpace(termo))
                    {
                        empresaComboBox.ItemsSource = empresas;
                    }
                    else
                    {
                        empresaComboBox.ItemsSource = empresas.Where(e => e.Nome.ToLower().Contains(termo)).ToList();
                    }
                    empresaSelecionada = null;
                    grupoBox.IsReadOnly = false;
                }
            };

            empresaComboBox.SuggestionChosen += (s, ev) =>
            {
                if (ev.SelectedItem is Empresa empSel)
                {
                    empresaSelecionada = empSel;
                    empresaComboBox.Text = empSel.Nome;
                    grupoBox.Text = LogoService.GetBaseNome(empSel.Nome);
                    grupoBox.IsReadOnly = true;
                }
            };

            if (existente != null)
            {
                nomeBox.Text = existente.Nome;
                cargoBox.Text = existente.Cargo;
                grupoBox.Text = existente.Grupo;

                // Pre-select empresa if linked
                if (existente.EmpresaId.HasValue)
                {
                    var empExistente = empresas.FirstOrDefault(e => e.Id == existente.EmpresaId.Value);
                    if (empExistente != null)
                    {
                        empresaSelecionada = empExistente;
                        empresaComboBox.Text = empExistente.Nome;
                        grupoBox.Text = LogoService.GetBaseNome(empExistente.Nome);
                        grupoBox.IsReadOnly = true;
                    }
                }
            }

            var form = new StackPanel { Spacing = 16, Width = 560, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0)) };

            // Fechar o auto-suggest se o usuário clicar no fundo do dialog
            form.PointerPressed += (s, ev) => 
            {
                empresaComboBox.IsSuggestionListOpen = false;
            };

            // Row 1: Nome + Cargo
            var linha1 = new Grid { ColumnSpacing = 16 };
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var campoNome = CriarCampo("NOME *", nomeBox);
            var campoCargo = CriarCampo("CARGO", cargoBox);
            Grid.SetColumn(campoNome, 0);
            Grid.SetColumn(campoCargo, 1);
            linha1.Children.Add(campoNome);
            linha1.Children.Add(campoCargo);
            form.Children.Add(linha1);

            // Warning aviso visual
            var avisoTexto = new TextBlock
            {
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 80, 80)),
                FontSize = 12,
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            // Empresa: AutoSuggestBox + botão Limpar visível
            form.Children.Add(CriarCampo("EMPRESA *", empresaGrid));

            form.Children.Add(avisoTexto);

            // Warning if no empresas
            if (empresas.Count == 0)
            {
                var avisoEmpresas = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                avisoEmpresas.Children.Add(new FontIcon
                {
                    Glyph = "\uE7BA", FontSize = 14,
                    Foreground = (Brush)Application.Current.Resources["AccentBrush"],
                    VerticalAlignment = VerticalAlignment.Center
                });
                avisoEmpresas.Children.Add(new TextBlock
                {
                    Text = "Nenhuma empresa cadastrada. Cadastre uma empresa primeiro.",
                    FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                });
                form.Children.Insert(2, avisoEmpresas); // insert after empresa combobox row
            }

            var dialog = new ContentDialog
            {
                Title = existente == null ? "Novo acompanhante técnico" : "Editar acompanhante técnico",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
                MinWidth = 640
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var erros = new List<string>();

                if (string.IsNullOrWhiteSpace(nomeBox.Text)) erros.Add("Nome");

                if (string.IsNullOrWhiteSpace(empresaComboBox.Text))
                {
                    avisoTexto.Text = "O campo empresa não pode ficar em branco. Escolha ou busque a empresa.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                var match = empresas.FirstOrDefault(e => e.Nome.Equals(empresaComboBox.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    empresaSelecionada = match;
                }
                else
                {
                    avisoTexto.Text = "Empresa não encontrada. Por favor, crie a empresa antes de prosseguir com o responsável.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }

                if (erros.Count > 0)
                {
                    avisoTexto.Text = $"Preencha os campos obrigatórios: {string.Join(", ", erros)}.";
                    avisoTexto.Visibility = Visibility.Visible;
                    args.Cancel = true;
                    return;
                }
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                int? empresaId = empresaSelecionada?.Id;

                if (existente != null)
                {
                    var item = await db.Acompanhantes.FindAsync(existente.Id);
                    if (item != null)
                    {
                        item.Nome = nomeBox.Text.Trim();
                        item.Cargo = cargoBox.Text.Trim();
                        item.Grupo = grupoBox.Text.Trim();
                        item.EmpresaId = empresaId;
                    }
                }
                else
                {
                    db.Acompanhantes.Add(new Acompanhante
                    {
                        Nome = nomeBox.Text.Trim(),
                        Cargo = cargoBox.Text.Trim(),
                        Grupo = grupoBox.Text.Trim(),
                        Ativo = true,
                        EmpresaId = empresaId
                    });
                }
                await db.SaveChangesAsync();
                AppLogger.LogInfo("DB", $"Acompanhante salvo: \"{nomeBox.Text.Trim()}\" / cargo={cargoBox.Text.Trim()}");
                RecarregarAbaAtual();
            }
        }

        private async void ExcluirAcompanhante_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not AcompanhanteListItem item) return;
            if (await ConfirmarExclusao(item.Nome))
            {
                try
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var a = await db.Acompanhantes.FindAsync(item.Id);
                    if (a != null) { a.Ativo = false; a.IsDeleted = true; a.UpdatedAt = SyncService.GetPushTimestamp(); await db.SaveChangesAsync(); }
                    RecarregarAbaAtual();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ACOMPANHANTE] Erro ao excluir: {ex}");
                    await MostrarErro(AppErrorCodes.DB_003, ex);
                }
            }
        }

        // ── EXPORTAR CSV ─────────────────────────────────────────────────────
        private async void ExportarEmpresas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var lista = db.Empresas.Where(x => x.IsActive).OrderBy(x => x.Nome).ToList();
                var sb = new StringBuilder();
                sb.AppendLine("Nome;Logo");
                foreach (var item in lista)
                    sb.AppendLine($"\"{item.Nome}\";\"{item.ImagemPath ?? ""}\"");
                await SalvarCsvAsync("empresas", sb.ToString());
            }
            catch (Exception ex) { await MostrarErro(AppErrorCodes.IO_001, ex); }
        }

        private async void ExportarObras_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lista = ObrasListViewCadastro.ItemsSource as System.Collections.IEnumerable;
                if (lista == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Nome;Grupo;Contratante;Responsável;Status;Tipo Contrato;Endereço;Data Início;Previsão Término");
                foreach (Obra o in lista)
                    sb.AppendLine($"\"{o.Nome}\";\"{o.Grupo}\";\"{o.Contratante}\";\"{o.Responsavel}\";\"{o.Status}\";\"{o.TipoContrato}\";\"{o.Endereco}\";\"{o.DataInicio:dd/MM/yyyy}\";\"{o.PrevisaoTermino?.ToString("dd/MM/yyyy") ?? ""}\"");
                await SalvarCsvAsync("obras", sb.ToString());
            }
            catch (Exception ex) { await MostrarErro(AppErrorCodes.IO_001, ex); }
        }

        private async void ExportarFuncionarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lista = FuncionariosListView.ItemsSource as System.Collections.IEnumerable;
                if (lista == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Nome;Função;Tipo;Empresa;Contato");
                foreach (Funcionario f in lista)
                    sb.AppendLine($"\"{f.Nome}\";\"{f.Funcao}\";\"{f.Tipo}\";\"{f.Empresa}\";\"{f.Contato}\"");
                await SalvarCsvAsync("funcionarios", sb.ToString());
            }
            catch (Exception ex) { await MostrarErro(AppErrorCodes.IO_001, ex); }
        }

        private async void ExportarEquipamentos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lista = EquipamentosListView.ItemsSource as System.Collections.IEnumerable;
                if (lista == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Patrimônio;Nome;Fabricante;Modelo");
                foreach (EquipamentoCadastrado eq in lista)
                    sb.AppendLine($"\"{eq.NumeroSerie}\";\"{eq.Nome}\";\"{eq.Fabricante}\";\"{eq.Modelo}\"");
                await SalvarCsvAsync("equipamentos", sb.ToString());
            }
            catch (Exception ex) { await MostrarErro(AppErrorCodes.IO_001, ex); }
        }

        private async void ExportarAcompanhantes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var lista = AcompanhantesListView.ItemsSource as System.Collections.IEnumerable;
                if (lista == null) return;
                var sb = new StringBuilder();
                sb.AppendLine("Nome;Cargo;Grupo/Cliente;Empresa;Contato");
                foreach (AcompanhanteListItem a in lista)
                    sb.AppendLine($"\"{a.Nome}\";\"{a.Cargo}\";\"{a.Grupo}\";\"{a.EmpresaNome ?? ""}\";\"{a.Contato}\"");
                await SalvarCsvAsync("acompanhantes", sb.ToString());
            }
            catch (Exception ex) { await MostrarErro(AppErrorCodes.IO_001, ex); }
        }

        private async Task SalvarCsvAsync(string prefixo, string conteudo)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("CSV", new System.Collections.Generic.List<string> { ".csv" });
            picker.SuggestedFileName = $"{prefixo}_{DateTime.Now:yyyyMMdd_HHmm}";
            var file = await picker.PickSaveFileAsync();
            if (file == null) return;
            await Windows.Storage.FileIO.WriteTextAsync(file, conteudo, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            AppLogger.LogInfo("IO", $"CSV exportado: {file.Path}  ({conteudo.Length} bytes, prefixo={prefixo})");
            var dialog = new ContentDialog
            {
                Title = "Exportado",
                Content = $"Arquivo salvo em:\n{file.Path}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // ── NAVEGAÇÃO ABAS ────────────────────────────────────────────────────
        private void BtnAbaEmpresas_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Empresas");

        private void IrParaEmpresas_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Empresas");

        private void BtnAbaObras_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Obras");
        private void BtnAbaFuncionarios_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Funcionarios");
        private void BtnAbaEquipamentos_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Equipamentos");
        private void BtnAbaAcompanhantes_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Acompanhantes");


        // ── HELPERS ───────────────────────────────────────────────────────────
        private static StackPanel CriarCampo(string label, FrameworkElement input)
        {
            var sp = new StackPanel { Spacing = 6 };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                CharacterSpacing = 150
            });
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            sp.Children.Add(input);
            return sp;
        }

        private async Task<bool> ConfirmarExclusao(string nome)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmar exclusão",
                Content = $"Deseja excluir \"{nome}\"?",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private async Task MostrarErro(string code, Exception? ex = null)
            => await ErrorDialogService.ShowAsync(this.XamlRoot, code, null, ex);

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
        {
            // Se veio de NovaObraPage com estado salvo, retorna o estado para restaurar o formulário
            if (_params?.VoltarPara == "NovaObra" && _params.EstadoNovaObra != null)
                Frame.Navigate(typeof(NovaObraPage), _params.EstadoNovaObra);
            else
                Frame.GoBack();
        }


    }
}
