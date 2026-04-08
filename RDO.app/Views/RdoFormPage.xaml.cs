using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.EntityFrameworkCore;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using PathIO = System.IO.Path;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;

namespace RDO.App.Views
{
    public class RdoFormParams
    {
        public int ObraId { get; set; }
        public int RelatorioId { get; set; } // 0 = novo, >0 = editar
    }

    public sealed partial class RdoFormPage : Page
    {
        private int _obraId;
        private int _savedRelatorioId;
        private int _editRelatorioId; // 0 = novo, >0 = editar relatório existente
        private readonly List<Atividade> _atividades = new();
        private readonly List<Ocorrencia> _ocorrencias = new();
        private readonly List<Foto> _fotos = new();
        private readonly List<int> _funcionarioIds = new();
        private readonly List<int> _equipamentoIds = new();
        private readonly List<int> _acompanhanteIds = new();
        private readonly Dictionary<int, (TimePicker entrada, TimePicker saida, TimePicker intervalo)> _horasFuncionario = new();
        private readonly HashSet<Atividade> _atividadesConfirmadas = new();
        private readonly HashSet<Ocorrencia> _ocorrenciasConfirmadas = new();
        private DispatcherTimer? _pulseTimer;
        private bool _pulseState;

        public RdoFormPage()
        {
            this.InitializeComponent();
            DataPicker.Date = DateTimeOffset.Now;
            DataTexto.Text = DateTime.Now.ToString("dd/MM/yyyy");
            StatusBox.SelectedIndex = 0;
            this.Loaded += RdoFormPage_Loaded;
        }

        private async void RdoFormPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_obraId == 0) return;
            if (_editRelatorioId > 0) return; // em modo de edição, não verificar rascunho

            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var rascunho = db.Relatorios
                .FirstOrDefault(r => r.ObraId == _obraId && r.Rascunho);

            if (rascunho == null) return;

            var dialog = new ContentDialog
            {
                Title = "Rascunho encontrado",
                Content = $"Existe um rascunho salvo em {rascunho.CriadoEm:dd/MM/yyyy HH:mm}. Deseja continuar de onde parou?",
                PrimaryButtonText = "Continuar rascunho",
                SecondaryButtonText = "Descartar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                CarregarRascunho(rascunho);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                using var db2 = new RdoDbContext(DbContextHelper.GetOptions());
                var r = await db2.Relatorios.FindAsync(rascunho.Id);
                if (r != null) { db2.Relatorios.Remove(r); await db2.SaveChangesAsync(); }
            }
        }

        protected override void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            int obraId = 0;
            if (e.Parameter is int id)
            {
                obraId = id;
            }
            else if (e.Parameter is RdoFormParams p)
            {
                obraId = p.ObraId;
                _editRelatorioId = p.RelatorioId;
            }

            if (obraId == 0) return;
            _obraId = obraId;

            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var obra = db.Obras.Find(obraId);
            if (obra != null)
            {
                TituloObra.Text = obra.Nome;
                if (_editRelatorioId > 0)
                {
                    var relEdicao = db.Relatorios.Find(_editRelatorioId);
                    NumeroRdoTexto.Text = relEdicao != null ? $"Nº {relEdicao.Numero:D3}" : "—";
                }
                else
                {
                    var numero = db.Relatorios.Count(r => r.ObraId == obraId && !r.Rascunho) + 1;
                    NumeroRdoTexto.Text = $"Nº {numero:D3}";
                }
                for (int i = 0; i < ResponsavelBox.Items.Count; i++)
                {
                    if ((ResponsavelBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.Responsavel)
                    { ResponsavelBox.SelectedIndex = i; break; }
                }
            }

            if (_editRelatorioId > 0)
                CarregarRelatorioParaEdicao(_editRelatorioId);
        }

        private void CarregarRelatorioParaEdicao(int relatorioId)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var rel = db.Relatorios
                .Include(r => r.Climas)
                .Include(r => r.Atividades)
                .Include(r => r.Ocorrencias)
                .Include(r => r.Assinaturas)
                .Include(r => r.Equipamentos)
                .Include(r => r.Fotos)
                .FirstOrDefault(r => r.Id == relatorioId);

            if (rel == null) return;

            DataPicker.Date = rel.Data;
            DataTexto.Text = rel.Data.ToString("dd/MM/yyyy");

            for (int i = 0; i < StatusBox.Items.Count; i++)
            {
                if ((StatusBox.Items[i] as ComboBoxItem)?.Content.ToString() == rel.Status)
                { StatusBox.SelectedIndex = i; break; }
            }

            var manha = rel.Climas.FirstOrDefault(c => c.Periodo == "Manhã");
            var tarde = rel.Climas.FirstOrDefault(c => c.Periodo == "Tarde");
            var noite = rel.Climas.FirstOrDefault(c => c.Periodo == "Noite");
            if (manha != null)
            {
                ManhaEnsolarado.IsChecked = manha.Tempo == "Ensolarado";
                ManhaNublado.IsChecked = manha.Tempo == "Nublado";
                ManhaChuvoso.IsChecked = manha.Tempo == "Chuvoso";
                ManhaPraticavel.IsChecked = manha.Condicao == "Praticável";
                ManhaImpraticavel.IsChecked = manha.Condicao == "Impraticável";
            }
            if (tarde != null)
            {
                TardeEnsolarado.IsChecked = tarde.Tempo == "Ensolarado";
                TardeNublado.IsChecked = tarde.Tempo == "Nublado";
                TardeChuvoso.IsChecked = tarde.Tempo == "Chuvoso";
                TardePraticavel.IsChecked = tarde.Condicao == "Praticável";
                TardeImpraticavel.IsChecked = tarde.Condicao == "Impraticável";
            }
            if (noite != null)
            {
                NoiteEnsolarado.IsChecked = noite.Tempo == "Ensolarado";
                NoiteNublado.IsChecked = noite.Tempo == "Nublado";
                NoiteChuvoso.IsChecked = noite.Tempo == "Chuvoso";
                NoitePraticavel.IsChecked = noite.Condicao == "Praticável";
                NoiteImpraticavel.IsChecked = noite.Condicao == "Impraticável";
            }

            foreach (var a in rel.Atividades)
            {
                var nova = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _atividades.Add(nova);
                AdicionarLinhaAtividade(nova, preConfirm: true);
            }

            foreach (var o in rel.Ocorrencias)
            {
                var nova = new Ocorrencia { Descricao = o.Descricao, HoraInicio = o.HoraInicio, HoraFim = o.HoraFim, Tags = o.Tags };
                _ocorrencias.Add(nova);
                AdicionarLinhaOcorrencia(nova, preConfirm: true);
            }

            foreach (var assin in rel.Assinaturas.Where(a => a.FuncionarioId.HasValue))
            {
                if (_funcionarioIds.Contains(assin.FuncionarioId!.Value)) continue;
                _funcionarioIds.Add(assin.FuncionarioId.Value);
                var func = db.Funcionarios.Find(assin.FuncionarioId.Value);
                if (func != null)
                {
                    AdicionarLinhaEquipe(func);
                    if (_horasFuncionario.TryGetValue(func.Id, out var horas))
                    {
                        if (TimeSpan.TryParse(assin.HoraEntrada, out var e)) horas.entrada.Time = e;
                        if (TimeSpan.TryParse(assin.HoraSaida, out var s)) horas.saida.Time = s;
                        if (TimeSpan.TryParse(assin.HoraIntervalo, out var iv)) horas.intervalo.Time = iv;
                    }
                }
            }

            foreach (var re in rel.Equipamentos)
            {
                if (_equipamentoIds.Contains(re.EquipamentoCadastradoId)) continue;
                _equipamentoIds.Add(re.EquipamentoCadastradoId);
                var eq = db.EquipamentosCadastrados.Find(re.EquipamentoCadastradoId);
                if (eq != null) AdicionarLinhaEquipamento(eq);
            }

            foreach (var f in rel.Fotos)
            {
                var fotoExistente = new Foto { CaminhoArquivo = f.CaminhoArquivo, Legenda = f.Legenda, TiradaEm = f.TiradaEm };
                _fotos.Add(fotoExistente);
                AdicionarFotoExistente(fotoExistente);
            }

            // Carrega acompanhantes via join-table (novo) com fallback para o campo legado
            var idsAcomp = db.RelatorioAcompanhantes
                .Where(ra => ra.RelatorioId == relatorioId)
                .Select(ra => ra.AcompanhanteId)
                .ToList();
            if (idsAcomp.Count == 0 && rel.AcompanhanteId.HasValue)
                idsAcomp.Add(rel.AcompanhanteId.Value);
            foreach (var acId in idsAcomp)
            {
                if (_acompanhanteIds.Contains(acId)) continue;
                _acompanhanteIds.Add(acId);
                var ac = db.Acompanhantes.Find(acId);
                if (ac != null) AdicionarLinhaAcompanhante(ac);
            }
        }

        private void AdicionarFotoExistente(Foto foto)
        {
            BitmapImage bitmap;
            try { bitmap = new BitmapImage(new Uri(foto.CaminhoArquivo)); }
            catch { return; }

            var card = new Border
            {
                Width = 160,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 30, 45, 74)),
                BorderThickness = new Thickness(1)
            };

            var cardStack = new StackPanel();
            var imgBorder = new Border { Height = 120, CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6, 6, 0, 0) };
            var imgGrid = new Grid();
            var img = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };

            var remBtn = new Button
            {
                Content = "✕",
                Width = 26, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 4, 0),
                Background = new SolidColorBrush(Color.FromArgb(200, 10, 14, 26)),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(0),
                FontSize = 10
            };
            remBtn.Click += (s, ev) =>
            {
                _fotos.Remove(foto);
                FotosWrapPanel.Children.Remove(card);
                ContadorFotos.Text = FotosWrapPanel.Children.Count.ToString();
            };

            imgGrid.Children.Add(img);
            imgGrid.Children.Add(remBtn);
            imgBorder.Child = imgGrid;

            var legendaBox = new TextBox
            {
                PlaceholderText = "Legenda...",
                Text = foto.Legenda,
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromArgb(255, 10, 14, 26)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 30, 45, 74)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(8, 6, 8, 6)
            };
            legendaBox.TextChanged += (s, ev) => foto.Legenda = legendaBox.Text;

            cardStack.Children.Add(imgBorder);
            cardStack.Children.Add(legendaBox);
            card.Child = cardStack;

            FotosWrapPanel.Children.Add(card);
            ContadorFotos.Text = FotosWrapPanel.Children.Count.ToString();
        }

        private void CarregarRascunho(Relatorio rascunho)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            DataPicker.Date = rascunho.Data;

            for (int i = 0; i < StatusBox.Items.Count; i++)
            {
                if ((StatusBox.Items[i] as ComboBoxItem)?.Content.ToString() == rascunho.Status)
                { StatusBox.SelectedIndex = i; break; }
            }

            // Restaurar clima
            var climas = db.Climas.Where(c => c.RelatorioId == rascunho.Id).ToList();
            var manha = climas.FirstOrDefault(c => c.Periodo == "Manhã");
            var tarde = climas.FirstOrDefault(c => c.Periodo == "Tarde");
            var noite = climas.FirstOrDefault(c => c.Periodo == "Noite");
            if (manha != null)
            {
                ManhaEnsolarado.IsChecked = manha.Tempo == "Ensolarado";
                ManhaNublado.IsChecked = manha.Tempo == "Nublado";
                ManhaChuvoso.IsChecked = manha.Tempo == "Chuvoso";
                ManhaPraticavel.IsChecked = manha.Condicao == "Praticável";
                ManhaImpraticavel.IsChecked = manha.Condicao == "Impraticável";
            }
            if (tarde != null)
            {
                TardeEnsolarado.IsChecked = tarde.Tempo == "Ensolarado";
                TardeNublado.IsChecked = tarde.Tempo == "Nublado";
                TardeChuvoso.IsChecked = tarde.Tempo == "Chuvoso";
                TardePraticavel.IsChecked = tarde.Condicao == "Praticável";
                TardeImpraticavel.IsChecked = tarde.Condicao == "Impraticável";
            }
            if (noite != null)
            {
                NoiteEnsolarado.IsChecked = noite.Tempo == "Ensolarado";
                NoiteNublado.IsChecked = noite.Tempo == "Nublado";
                NoiteChuvoso.IsChecked = noite.Tempo == "Chuvoso";
                NoitePraticavel.IsChecked = noite.Condicao == "Praticável";
                NoiteImpraticavel.IsChecked = noite.Condicao == "Impraticável";
            }

            // Restaurar atividades — cria novos objetos (Id=0) para evitar conflito de PK ao re-salvar
            var atividades = db.Atividades.Where(a => a.RelatorioId == rascunho.Id).ToList();
            foreach (var a in atividades)
            {
                var nova = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _atividades.Add(nova);
                AdicionarLinhaAtividade(nova);
            }

            // Restaurar ocorrências — cria novos objetos (Id=0)
            var ocorrencias = db.Ocorrencias.Where(o => o.RelatorioId == rascunho.Id).ToList();
            foreach (var o in ocorrencias)
            {
                var nova = new Ocorrencia { Descricao = o.Descricao, HoraInicio = o.HoraInicio, HoraFim = o.HoraFim, Tags = o.Tags };
                _ocorrencias.Add(nova);
                AdicionarLinhaOcorrencia(nova);
            }

            // Remove o rascunho e todos os seus dependentes do banco
            db.Climas.RemoveRange(climas);
            db.Atividades.RemoveRange(atividades);
            db.Ocorrencias.RemoveRange(ocorrencias);
            var r = db.Relatorios.Find(rascunho.Id);
            if (r != null) db.Relatorios.Remove(r);
            db.SaveChanges();
        }

        // ── CREA + STATUS ─────────────────────────────────────────────────────
        private static readonly Dictionary<string, string> _creaMap = new()
        {
            { "Bruno Pires",          "5063435630" },
            { "Felipe Prado",         "5063687510" },
            { "Juliana Bertoni",      "5063687927" },
            { "Maicon Salomão",       "5070334847" },
            { "Murilo Franco",        "5068975820" },
            { "Wellington Bortolozo","5070173544"  },
            { "Wesley Gregório",      "5070948640" }
        };

        private void ResponsavelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var nome = (ResponsavelBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            CreaTexto.Text = _creaMap.TryGetValue(nome, out var crea) ? crea : "—";
        }

        private void StatusBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            switch (status)
            {
                case "Publicado":
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30));
                    StatusBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80));
                    StatusBadge.BorderThickness = new Thickness(1);
                    StatusBadgeTexto.Text = "✅ Publicado";
                    StatusBadgeTexto.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100));
                    IniciarPulse();
                    break;
                case "Rascunho":
                    PararPulse();
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(255, 50, 45, 0));
                    StatusBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 180, 0));
                    StatusBadge.BorderThickness = new Thickness(1);
                    StatusBadgeTexto.Text = "✏ Rascunho";
                    StatusBadgeTexto.Foreground = new SolidColorBrush(Color.FromArgb(255, 240, 220, 0));
                    break;
                default:
                    PararPulse();
                    StatusBadge.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    StatusBadge.BorderThickness = new Thickness(0);
                    StatusBadgeTexto.Text = "";
                    break;
            }
        }

        private void IniciarPulse()
        {
            PararPulse();
            _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _pulseTimer.Tick += (s, ev) =>
            {
                _pulseState = !_pulseState;
                StatusBadge.Background = new SolidColorBrush(_pulseState
                    ? Color.FromArgb(255, 25, 90, 45)
                    : Color.FromArgb(255, 10, 60, 30));
                StatusBadge.BorderBrush = new SolidColorBrush(_pulseState
                    ? Color.FromArgb(255, 0, 230, 110)
                    : Color.FromArgb(255, 0, 180, 80));
            };
            _pulseTimer.Start();
        }

        private void PararPulse()
        {
            _pulseTimer?.Stop();
            _pulseTimer = null;
            _pulseState = false;
        }

        protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            PararPulse();
            base.OnNavigatedFrom(e);
        }

        private void DataPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            DataTexto.Text = args.NewDate.HasValue
                ? args.NewDate.Value.ToString("dd/MM/yyyy")
                : DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void AbrirCalendarioBtn_Click(object sender, RoutedEventArgs e)
            => DataPicker.IsCalendarOpen = true;

        // ── EQUIPE ────────────────────────────────────────────────────────────
        private async void AdicionarFuncionario_Click(object sender, RoutedEventArgs e)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.Funcionarios.Where(f => f.Ativo).ToList();

            var combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Selecione o funcionário"
            };
            foreach (var f in lista)
                combo.Items.Add(new ComboBoxItem { Content = f.Nome, Tag = f.Id });

            var linkCadastro = new HyperlinkButton
            {
                Content = "Não encontrou? Cadastrar novo funcionário →",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160))
            };

            var form = new StackPanel { Spacing = 12, Width = 420 };
            form.Children.Add(CriarCampoLabel("FUNCIONÁRIO *", combo));
            form.Children.Add(linkCadastro);

            var dialog = new ContentDialog
            {
                Title = "Adicionar funcionário",
                Content = form,
                PrimaryButtonText = "Adicionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            linkCadastro.Click += (s, ev) =>
            {
                dialog.Hide();
                Frame.Navigate(typeof(CadastrosPage), "Funcionarios");
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (combo.SelectedItem is ComboBoxItem item && item.Tag is int id)
                {
                    if (_funcionarioIds.Contains(id)) return;
                    var func = lista.First(f => f.Id == id);
                    _funcionarioIds.Add(id);
                    AdicionarLinhaEquipe(func);
                }
            }
        }

        private void AdicionarLinhaEquipe(Funcionario func)
        {
            var border = new Border
            {
                Background = TR("AppBgBrush"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) }); // entrada
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) }); // saída
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(245) }); // intervalo
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // hs trab
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) }); // remover

            var info = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = func.Nome,
                FontSize = 13,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("TextPrimaryBrush")
            });
            info.Children.Add(new TextBlock
            {
                Text = func.Funcao,
                FontSize = 11,
                Foreground = TR("TextTertiaryBrush")
            });

            var empresa = new TextBlock
            {
                Text = func.Empresa,
                FontSize = 12,
                Foreground = TR("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var entradaBox = CriarTimePicker(8, 0);
            var saidaBox = CriarTimePicker(17, 0);
            var intervBox = CriarTimePicker(1, 0);

            var hsTrab = new TextBlock
            {
                Text = "08:00",
                FontSize = 13,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            void Calc()
            {
                var total = saidaBox.Time - entradaBox.Time - intervBox.Time;
                hsTrab.Text = total.TotalMinutes > 0
                    ? $"{(int)total.TotalHours:D2}:{total.Minutes:D2}" : "00:00";
            }

            entradaBox.TimeChanged += (s, ev) => Calc();
            saidaBox.TimeChanged += (s, ev) => Calc();
            intervBox.TimeChanged += (s, ev) => Calc();
            _horasFuncionario[func.Id] = (entradaBox, saidaBox, intervBox);

            var remBtn = CriarBotaoRemover();
            remBtn.Click += (s, ev) =>
            {
                _funcionarioIds.Remove(func.Id);
                _horasFuncionario.Remove(func.Id);
                FuncionariosPanel.Children.Remove(border);
                ContadorEquipe.Text = FuncionariosPanel.Children.Count.ToString();
                ReconstruirZebra(FuncionariosPanel);
            };

            Grid.SetColumn(info, 0);
            Grid.SetColumn(empresa, 1);
            Grid.SetColumn(entradaBox, 2);
            Grid.SetColumn(saidaBox, 3);
            Grid.SetColumn(intervBox, 4);
            Grid.SetColumn(hsTrab, 5);
            Grid.SetColumn(remBtn, 6);
            grid.Children.Add(info);
            grid.Children.Add(empresa);
            grid.Children.Add(entradaBox);
            grid.Children.Add(saidaBox);
            grid.Children.Add(intervBox);
            grid.Children.Add(hsTrab);
            grid.Children.Add(remBtn);

            border.Child = grid;
            FuncionariosPanel.Children.Add(border);
            ContadorEquipe.Text = FuncionariosPanel.Children.Count.ToString();
        }


        // ── EQUIPAMENTOS ──────────────────────────────────────────────────────
        private async void AdicionarEquipamento_Click(object sender, RoutedEventArgs e)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.EquipamentosCadastrados.Where(eq => eq.Ativo).ToList();

            var combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Selecione o equipamento"
            };
            foreach (var eq in lista)
                combo.Items.Add(new ComboBoxItem { Content = $"{eq.NumeroSerie} — {eq.Nome}", Tag = eq.Id });

            var linkCadastro = new HyperlinkButton
            {
                Content = "Não encontrou? Cadastrar novo equipamento →",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160))
            };

            var form = new StackPanel { Spacing = 12, Width = 420 };
            form.Children.Add(CriarCampoLabel("EQUIPAMENTO *", combo));
            form.Children.Add(linkCadastro);

            var dialog = new ContentDialog
            {
                Title = "Adicionar equipamento",
                Content = form,
                PrimaryButtonText = "Adicionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            linkCadastro.Click += (s, ev) =>
            {
                dialog.Hide();
                Frame.Navigate(typeof(CadastrosPage), "Equipamentos");
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (combo.SelectedItem is ComboBoxItem item && item.Tag is int id)
                {
                    if (_equipamentoIds.Contains(id)) return;
                    var eq = lista.First(x => x.Id == id);
                    _equipamentoIds.Add(id);
                    AdicionarLinhaEquipamento(eq);
                }
            }
        }

        private void AdicionarLinhaEquipamento(EquipamentoCadastrado eq)
        {
            var border = new Border
            {
                Background = TR("AppBgBrush"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            var patrimonio = new TextBlock
            {
                Text = eq.NumeroSerie,
                FontSize = 12,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("AccentBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var nome = new TextBlock
            {
                Text = eq.Nome,
                FontSize = 13,
                Foreground = TR("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var fab = new TextBlock
            {
                Text = $"{eq.Fabricante} — {eq.Modelo}",
                FontSize = 11,
                Foreground = TR("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var remBtn = CriarBotaoRemover();
            remBtn.Click += (s, ev) =>
            {
                _equipamentoIds.Remove(eq.Id);
                EquipamentosPanel.Children.Remove(border);
                ContadorEquipamentos.Text = EquipamentosPanel.Children.Count.ToString();
                ReconstruirZebra(EquipamentosPanel);
            };

            Grid.SetColumn(patrimonio, 0);
            Grid.SetColumn(nome, 1);
            Grid.SetColumn(fab, 2);
            Grid.SetColumn(remBtn, 4);
            grid.Children.Add(patrimonio);
            grid.Children.Add(nome);
            grid.Children.Add(fab);
            grid.Children.Add(remBtn);
            border.Child = grid;
            EquipamentosPanel.Children.Add(border);
            ContadorEquipamentos.Text = EquipamentosPanel.Children.Count.ToString();
        }

        // ── ACOMPANHANTES ─────────────────────────────────────────────────────
        private async void AdicionarAcompanhante_Click(object sender, RoutedEventArgs e)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.Acompanhantes.Where(a => a.Ativo).ToList();

            var combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PlaceholderText = "Selecione o acompanhante"
            };
            foreach (var a in lista)
                combo.Items.Add(new ComboBoxItem { Content = $"{a.Nome} — {a.Cargo}", Tag = a.Id });

            var link = new HyperlinkButton
            {
                Content = "Não encontrou? Cadastrar novo acompanhante →",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160))
            };

            var form = new StackPanel { Spacing = 12, Width = 420 };
            form.Children.Add(CriarCampoLabel("ACOMPANHANTE *", combo));
            form.Children.Add(link);

            var dialog = new ContentDialog
            {
                Title = "Adicionar acompanhante técnico",
                Content = form,
                PrimaryButtonText = "Adicionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            link.Click += (s, ev) =>
            {
                dialog.Hide();
                Frame.Navigate(typeof(CadastrosPage), "Acompanhantes");
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (combo.SelectedItem is ComboBoxItem item && item.Tag is int id)
                {
                    if (_acompanhanteIds.Contains(id)) return;
                    var ac = lista.First(a => a.Id == id);
                    _acompanhanteIds.Add(id);
                    AdicionarLinhaAcompanhante(ac);
                    ContadorAcompanhantes.Text = AcompanhantesPanel.Children.Count.ToString();
                }
            }
        }

        private void AdicionarLinhaAcompanhante(Acompanhante ac)
        {
            var border = new Border
            {
                Background = TR("AppBgBrush"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            var nomeStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            nomeStack.Children.Add(new TextBlock
            {
                Text = ac.Nome,
                FontSize = 13,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("TextPrimaryBrush")
            });

            var cargo = new TextBlock
            {
                Text = ac.Cargo,
                FontSize = 12,
                Foreground = TR("TextTertiaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var grupoBadge = new Border
            {
                Background = TR("AccentSubtleBgBrush"),
                BorderBrush = TR("AccentBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(2),
                Padding = new Thickness(8, 3, 8, 3),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            grupoBadge.Child = new TextBlock
            {
                Text = ac.Grupo,
                FontSize = 11,
                Foreground = TR("AccentBrush")
            };

            var remBtn = CriarBotaoRemover();
            remBtn.Click += (s, ev) =>
            {
                _acompanhanteIds.Remove(ac.Id);
                AcompanhantesPanel.Children.Remove(border);
                ContadorAcompanhantes.Text = AcompanhantesPanel.Children.Count.ToString();
                ReconstruirZebra(AcompanhantesPanel);
            };

            Grid.SetColumn(nomeStack, 0);
            Grid.SetColumn(cargo, 1);
            Grid.SetColumn(grupoBadge, 2);
            Grid.SetColumn(remBtn, 4);
            grid.Children.Add(nomeStack);
            grid.Children.Add(cargo);
            grid.Children.Add(grupoBadge);
            grid.Children.Add(remBtn);
            border.Child = grid;
            AcompanhantesPanel.Children.Add(border);
            ContadorAcompanhantes.Text = AcompanhantesPanel.Children.Count.ToString();
        }

        // ── ATIVIDADES ────────────────────────────────────────────────────────
        private async void AdicionarAtividade_Click(object sender, RoutedEventArgs e)
        {
            if (_atividades.Any(a => !_atividadesConfirmadas.Contains(a)))
            {
                var aviso = new ContentDialog
                {
                    Title = "Item pendente",
                    Content = "Confirme a atividade anterior clicando em ✓ antes de adicionar uma nova.",
                    CloseButtonText = "Entendi",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                await aviso.ShowAsync();
                return;
            }
            var atividade = new Atividade { Status = "Em andamento" };
            _atividades.Add(atividade);
            AdicionarLinhaAtividade(atividade);
        }

        private void AdicionarLinhaAtividade(Atividade atividade, bool preConfirm = false)
        {
            var idx = _atividades.IndexOf(atividade) + 1;
            var border = new Border
            {
                Background = TR("PanelBgBrush"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var row = new Grid { ColumnSpacing = 6 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            var numBadge = new Border
            {
                Width = 24, Height = 24,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160)),
                VerticalAlignment = VerticalAlignment.Top
            };
            numBadge.Child = new TextBlock
            {
                Text = idx.ToString(),
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = new SolidColorBrush(Color.FromArgb(255, 10, 14, 26)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var descBox = new TextBox
            {
                PlaceholderText = "Descreva a atividade executada...",
                AcceptsReturn = true,
                MinHeight = 36,
                TextWrapping = TextWrapping.Wrap,
                Background = TR("InputBgBrush"),
                BorderBrush = TR("AppBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Foreground = TR("TextPrimaryBrush"),
                Padding = new Thickness(10, 8, 10, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            descBox.TextChanged += (s, ev) => atividade.Descricao = descBox.Text;
            if (!string.IsNullOrEmpty(atividade.Descricao))
                descBox.Text = atividade.Descricao;

            // Botão confirmar / editar
            var saveBtn = new Button
            {
                Content = "✓",
                Width = 34, Height = 34,
                Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100)),
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 14
            };
            ToolTipService.SetToolTip(saveBtn, "Confirmar atividade");

            var upBtn = CriarBotaoOrdem("↑");
            upBtn.VerticalAlignment = VerticalAlignment.Top;
            upBtn.Visibility = Visibility.Collapsed;
            ToolTipService.SetToolTip(upBtn, "Mover para cima");
            upBtn.Click += (s, ev) =>
            {
                var i = _atividades.IndexOf(atividade);
                if (i <= 0) return;
                (_atividades[i], _atividades[i - 1]) = (_atividades[i - 1], _atividades[i]);
                AtividadesPanel.Children.Remove(border);
                AtividadesPanel.Children.Insert(i - 1, border);
                RenumerarAtividades();
            };

            var downBtn = CriarBotaoOrdem("↓");
            downBtn.VerticalAlignment = VerticalAlignment.Top;
            downBtn.Visibility = Visibility.Collapsed;
            ToolTipService.SetToolTip(downBtn, "Mover para baixo");
            downBtn.Click += (s, ev) =>
            {
                var i = _atividades.IndexOf(atividade);
                if (i >= _atividades.Count - 1) return;
                (_atividades[i], _atividades[i + 1]) = (_atividades[i + 1], _atividades[i]);
                AtividadesPanel.Children.Remove(border);
                AtividadesPanel.Children.Insert(i + 1, border);
                RenumerarAtividades();
            };

            if (preConfirm)
            {
                _atividadesConfirmadas.Add(atividade);
                descBox.IsReadOnly = true;
                descBox.Opacity = 0.75;
                saveBtn.Content = "✏";
                saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
                ToolTipService.SetToolTip(saveBtn, "Editar descrição");
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 160, 90));
                upBtn.Visibility = Visibility.Visible;
                downBtn.Visibility = Visibility.Visible;
            }
            saveBtn.Click += (s, ev) =>
            {
                bool isConfirmed = _atividadesConfirmadas.Contains(atividade);
                if (!isConfirmed)
                {
                    _atividadesConfirmadas.Add(atividade);
                    descBox.IsReadOnly = true;
                    descBox.Opacity = 0.75;
                    saveBtn.Content = "✏";
                    saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                    saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                    saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
                    ToolTipService.SetToolTip(saveBtn, "Editar descrição");
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 160, 90));
                    upBtn.Visibility = Visibility.Visible;
                    downBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    _atividadesConfirmadas.Remove(atividade);
                    descBox.IsReadOnly = false;
                    descBox.Opacity = 1.0;
                    saveBtn.Content = "✓";
                    saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30));
                    saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80));
                    saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100));
                    ToolTipService.SetToolTip(saveBtn, "Confirmar atividade");
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160));
                    upBtn.Visibility = Visibility.Collapsed;
                    downBtn.Visibility = Visibility.Collapsed;
                }
            };

            var remBtn = CriarBotaoRemover();
            remBtn.VerticalAlignment = VerticalAlignment.Top;
            remBtn.Click += (s, ev) =>
            {
                _atividades.Remove(atividade);
                _atividadesConfirmadas.Remove(atividade);
                AtividadesPanel.Children.Remove(border);
                ContadorAtividades.Text = AtividadesPanel.Children.Count.ToString();
                RenumerarAtividades();
            };

            Grid.SetColumn(numBadge, 0);
            Grid.SetColumn(descBox, 1);
            Grid.SetColumn(saveBtn, 2);
            Grid.SetColumn(upBtn, 3);
            Grid.SetColumn(downBtn, 4);
            Grid.SetColumn(remBtn, 5);
            row.Children.Add(numBadge);
            row.Children.Add(descBox);
            row.Children.Add(saveBtn);
            row.Children.Add(upBtn);
            row.Children.Add(downBtn);
            row.Children.Add(remBtn);

            border.Child = row;
            AtividadesPanel.Children.Add(border);
            ContadorAtividades.Text = AtividadesPanel.Children.Count.ToString();
        }

        // ── OCORRÊNCIAS ───────────────────────────────────────────────────────
        private async void AdicionarOcorrencia_Click(object sender, RoutedEventArgs e)
        {
            if (_ocorrencias.Any(o => !_ocorrenciasConfirmadas.Contains(o)))
            {
                var aviso = new ContentDialog
                {
                    Title = "Item pendente",
                    Content = "Confirme a ocorrência anterior clicando em ✓ antes de adicionar uma nova.",
                    CloseButtonText = "Entendi",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };
                await aviso.ShowAsync();
                return;
            }
            var ocorrencia = new Ocorrencia();
            _ocorrencias.Add(ocorrencia);
            AdicionarLinhaOcorrencia(ocorrencia);
        }

        private void AdicionarLinhaOcorrencia(Ocorrencia ocorrencia, bool preConfirm = false)
        {
            var idx = _ocorrencias.IndexOf(ocorrencia) + 1;
            var border = new Border
            {
                Background = TR("PanelBgBrush"),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 122, 122)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var mainStack = new StackPanel { Spacing = 8 };

            var topRow = new Grid { ColumnSpacing = 6 };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            var numBadge = new Border
            {
                Width = 24, Height = 24,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(255, 255, 122, 122)),
                VerticalAlignment = VerticalAlignment.Top
            };
            numBadge.Child = new TextBlock
            {
                Text = idx.ToString(),
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var descBox = new TextBox
            {
                PlaceholderText = "Descreva a ocorrência...",
                AcceptsReturn = true,
                MinHeight = 36,
                TextWrapping = TextWrapping.Wrap,
                Background = TR("InputBgBrush"),
                BorderBrush = TR("AppBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Foreground = TR("TextPrimaryBrush"),
                Padding = new Thickness(10, 8, 10, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            descBox.TextChanged += (s, ev) => ocorrencia.Descricao = descBox.Text;
            if (!string.IsNullOrEmpty(ocorrencia.Descricao))
                descBox.Text = ocorrencia.Descricao;

            var upBtnOc = CriarBotaoOrdem("↑");
            upBtnOc.VerticalAlignment = VerticalAlignment.Top;
            upBtnOc.Visibility = Visibility.Collapsed;
            ToolTipService.SetToolTip(upBtnOc, "Mover para cima");
            upBtnOc.Click += (s, ev) =>
            {
                var i = _ocorrencias.IndexOf(ocorrencia);
                if (i <= 0) return;
                (_ocorrencias[i], _ocorrencias[i - 1]) = (_ocorrencias[i - 1], _ocorrencias[i]);
                OcorrenciasPanel.Children.Remove(border);
                OcorrenciasPanel.Children.Insert(i - 1, border);
                RenumerarOcorrencias();
            };

            var downBtnOc = CriarBotaoOrdem("↓");
            downBtnOc.VerticalAlignment = VerticalAlignment.Top;
            downBtnOc.Visibility = Visibility.Collapsed;
            ToolTipService.SetToolTip(downBtnOc, "Mover para baixo");
            downBtnOc.Click += (s, ev) =>
            {
                var i = _ocorrencias.IndexOf(ocorrencia);
                if (i >= _ocorrencias.Count - 1) return;
                (_ocorrencias[i], _ocorrencias[i + 1]) = (_ocorrencias[i + 1], _ocorrencias[i]);
                OcorrenciasPanel.Children.Remove(border);
                OcorrenciasPanel.Children.Insert(i + 1, border);
                RenumerarOcorrencias();
            };

            // Botão confirmar / editar ocorrência
            var saveBtnOc = new Button
            {
                Content = "✓",
                Width = 34, Height = 34,
                Background = new SolidColorBrush(Color.FromArgb(255, 60, 20, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 180, 60, 60)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 140, 140)),
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 14
            };
            ToolTipService.SetToolTip(saveBtnOc, "Confirmar ocorrência");
            if (preConfirm)
            {
                _ocorrenciasConfirmadas.Add(ocorrencia);
                descBox.IsReadOnly = true;
                descBox.Opacity = 0.75;
                saveBtnOc.Content = "✏";
                saveBtnOc.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                saveBtnOc.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                saveBtnOc.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
                ToolTipService.SetToolTip(saveBtnOc, "Editar ocorrência");
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 80, 80));
                upBtnOc.Visibility = Visibility.Visible;
                downBtnOc.Visibility = Visibility.Visible;
            }
            saveBtnOc.Click += (s, ev) =>
            {
                bool isConfirmed = _ocorrenciasConfirmadas.Contains(ocorrencia);
                if (!isConfirmed)
                {
                    _ocorrenciasConfirmadas.Add(ocorrencia);
                    descBox.IsReadOnly = true;
                    descBox.Opacity = 0.75;
                    saveBtnOc.Content = "✏";
                    saveBtnOc.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                    saveBtnOc.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                    saveBtnOc.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
                    ToolTipService.SetToolTip(saveBtnOc, "Editar ocorrência");
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 200, 80, 80));
                    upBtnOc.Visibility = Visibility.Visible;
                    downBtnOc.Visibility = Visibility.Visible;
                }
                else
                {
                    _ocorrenciasConfirmadas.Remove(ocorrencia);
                    descBox.IsReadOnly = false;
                    descBox.Opacity = 1.0;
                    saveBtnOc.Content = "✓";
                    saveBtnOc.Background = new SolidColorBrush(Color.FromArgb(255, 60, 20, 20));
                    saveBtnOc.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 180, 60, 60));
                    saveBtnOc.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 140, 140));
                    ToolTipService.SetToolTip(saveBtnOc, "Confirmar ocorrência");
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 122, 122));
                    upBtnOc.Visibility = Visibility.Collapsed;
                    downBtnOc.Visibility = Visibility.Collapsed;
                }
            };

            var remBtn = CriarBotaoRemover();
            remBtn.VerticalAlignment = VerticalAlignment.Top;
            remBtn.Click += (s, ev) =>
            {
                _ocorrencias.Remove(ocorrencia);
                _ocorrenciasConfirmadas.Remove(ocorrencia);
                OcorrenciasPanel.Children.Remove(border);
                ContadorOcorrencias.Text = OcorrenciasPanel.Children.Count.ToString();
                RenumerarOcorrencias();
            };

            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            Grid.SetColumn(numBadge, 0);
            Grid.SetColumn(descBox, 1);
            Grid.SetColumn(saveBtnOc, 2);
            Grid.SetColumn(upBtnOc, 3);
            Grid.SetColumn(downBtnOc, 4);
            Grid.SetColumn(remBtn, 5);
            topRow.Children.Add(numBadge);
            topRow.Children.Add(descBox);
            topRow.Children.Add(saveBtnOc);
            topRow.Children.Add(upBtnOc);
            topRow.Children.Add(downBtnOc);
            topRow.Children.Add(remBtn);

            var horariosRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, Margin = new Thickness(38, 0, 0, 0) };

            var inicioBox = CriarTimePicker(8, 0);
            if (!string.IsNullOrEmpty(ocorrencia.HoraInicio) && TimeSpan.TryParse(ocorrencia.HoraInicio, out var tInicio))
                inicioBox.Time = tInicio;
            ocorrencia.HoraInicio = inicioBox.Time.ToString(@"hh\:mm");
            inicioBox.TimeChanged += (s, ev) => ocorrencia.HoraInicio = inicioBox.Time.ToString(@"hh\:mm");

            var fimBox = CriarTimePicker(9, 0);
            if (!string.IsNullOrEmpty(ocorrencia.HoraFim) && TimeSpan.TryParse(ocorrencia.HoraFim, out var tFim))
                fimBox.Time = tFim;
            ocorrencia.HoraFim = fimBox.Time.ToString(@"hh\:mm");
            fimBox.TimeChanged += (s, ev) => ocorrencia.HoraFim = fimBox.Time.ToString(@"hh\:mm");

            var inicioStack = new StackPanel { Spacing = 2 };
            inicioStack.Children.Add(new TextBlock { Text = "INÍCIO", FontSize = 9, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }, Foreground = TR("TextSecondaryBrush"), CharacterSpacing = 80 });
            inicioStack.Children.Add(inicioBox);

            var fimStack = new StackPanel { Spacing = 2 };
            fimStack.Children.Add(new TextBlock { Text = "FIM", FontSize = 9, FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 }, Foreground = TR("TextSecondaryBrush"), CharacterSpacing = 80 });
            fimStack.Children.Add(fimBox);

            horariosRow.Children.Add(inicioStack);
            horariosRow.Children.Add(fimStack);

            mainStack.Children.Add(topRow);
            mainStack.Children.Add(horariosRow);
            border.Child = mainStack;

            OcorrenciasPanel.Children.Add(border);
            ContadorOcorrencias.Text = OcorrenciasPanel.Children.Count.ToString();
        }

        // ── FOTOS ─────────────────────────────────────────────────────────────
        private async void AdicionarFoto_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var foto = new Foto { CaminhoArquivo = file.Path, TiradaEm = DateTime.Now };
            _fotos.Add(foto);

            var bitmap = new BitmapImage();
            using var stream = await file.OpenReadAsync();
            await bitmap.SetSourceAsync(stream);

            var card = new Border
            {
                Width = 160,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 30, 45, 74)),
                BorderThickness = new Thickness(1)
            };

            var cardStack = new StackPanel();
            var imgBorder = new Border
            {
                Height = 120,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6, 6, 0, 0)
            };
            var imgGrid = new Grid();
            var img = new Image { Source = bitmap, Stretch = Stretch.UniformToFill };

            var remBtn = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 4, 0),
                Background = new SolidColorBrush(Color.FromArgb(200, 10, 14, 26)),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(0),
                FontSize = 10
            };
            remBtn.Click += (s, ev) =>
            {
                _fotos.Remove(foto);
                FotosWrapPanel.Children.Remove(card);
                ContadorFotos.Text = FotosWrapPanel.Children.Count.ToString();
            };

            imgGrid.Children.Add(img);
            imgGrid.Children.Add(remBtn);
            imgBorder.Child = imgGrid;

            var legendaBox = new TextBox
            {
                PlaceholderText = "Legenda...",
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromArgb(255, 10, 14, 26)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 30, 45, 74)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(8, 6, 8, 6)
            };
            legendaBox.TextChanged += (s, ev) => foto.Legenda = legendaBox.Text;

            cardStack.Children.Add(imgBorder);
            cardStack.Children.Add(legendaBox);
            card.Child = cardStack;

            FotosWrapPanel.Children.Add(card);
            ContadorFotos.Text = FotosWrapPanel.Children.Count.ToString();
        }


        // ── SALVAR ────────────────────────────────────────────────────────────
        private async void SalvarBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var statusEscolhido = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                if (statusEscolhido != "Publicado")
                {
                    await MostrarErro("O relatório só pode ser salvo quando o status for \"Publicado\".");
                    return;
                }

                using var db = new RdoDbContext(DbContextHelper.GetOptions());

                Relatorio relatorio;
                if (_editRelatorioId > 0)
                {
                    relatorio = await db.Relatorios.FindAsync(_editRelatorioId)
                        ?? throw new Exception("Relatório não encontrado.");
                    relatorio.Data = DataPicker.Date?.DateTime ?? DateTime.Now;
                    relatorio.Status = statusEscolhido;
                    relatorio.AcompanhanteId = null; // substituído pelo join-table
                    relatorio.Rascunho = false;

                    db.Climas.RemoveRange(db.Climas.Where(c => c.RelatorioId == _editRelatorioId));
                    db.Atividades.RemoveRange(db.Atividades.Where(a => a.RelatorioId == _editRelatorioId));
                    db.Ocorrencias.RemoveRange(db.Ocorrencias.Where(o => o.RelatorioId == _editRelatorioId));
                    db.Assinaturas.RemoveRange(db.Assinaturas.Where(a => a.RelatorioId == _editRelatorioId));
                    db.Fotos.RemoveRange(db.Fotos.Where(f => f.RelatorioId == _editRelatorioId));
                    db.RelatorioEquipamentos.RemoveRange(db.RelatorioEquipamentos.Where(re => re.RelatorioId == _editRelatorioId));
                    db.RelatorioAcompanhantes.RemoveRange(db.RelatorioAcompanhantes.Where(ra => ra.RelatorioId == _editRelatorioId));
                    await db.SaveChangesAsync();
                }
                else
                {
                    var numero = db.Relatorios.Count(r => r.ObraId == _obraId && !r.Rascunho) + 1;
                    relatorio = new Relatorio
                    {
                        ObraId = _obraId,
                        UsuarioId = 1,
                        Numero = numero,
                        Data = DataPicker.Date?.DateTime ?? DateTime.Now,
                        ObsGerais = "",
                        Status = statusEscolhido,
                        AcompanhanteId = null, // usa join-table
                        Sincronizado = false,
                        Rascunho = false,
                        CriadoEm = DateTime.Now
                    };
                    db.Relatorios.Add(relatorio);
                    await db.SaveChangesAsync();
                }

                void SalvarClima(string periodo, ToggleButton ens, ToggleButton nub, ToggleButton chu, ToggleButton prat, ToggleButton imprat)
                {
                    var tempo = ens.IsChecked == true ? "Ensolarado" :
                                nub.IsChecked == true ? "Nublado" :
                                chu.IsChecked == true ? "Chuvoso" : "";
                    var condicao = prat.IsChecked == true ? "Praticável" :
                                   imprat.IsChecked == true ? "Impraticável" : "";
                    if (!string.IsNullOrEmpty(tempo) || !string.IsNullOrEmpty(condicao))
                        db.Climas.Add(new ClimaDetalhe
                        {
                            RelatorioId = relatorio.Id,
                            Periodo = periodo,
                            Ativo = true,
                            Tempo = tempo,
                            Condicao = condicao
                        });
                }
                SalvarClima("Manhã", ManhaEnsolarado, ManhaNublado, ManhaChuvoso, ManhaPraticavel, ManhaImpraticavel);
                SalvarClima("Tarde", TardeEnsolarado, TardeNublado, TardeChuvoso, TardePraticavel, TardeImpraticavel);
                SalvarClima("Noite", NoiteEnsolarado, NoiteNublado, NoiteChuvoso, NoitePraticavel, NoiteImpraticavel);

                var naoConfirmadas = _atividades.Count(a => !_atividadesConfirmadas.Contains(a) && !string.IsNullOrWhiteSpace(a.Descricao));
                var naoConfirmadasOc = _ocorrencias.Count(o => !_ocorrenciasConfirmadas.Contains(o));
                if (naoConfirmadas > 0 || naoConfirmadasOc > 0)
                {
                    var aviso = new ContentDialog
                    {
                        Title = "Itens não confirmados",
                        Content = $"Há {naoConfirmadas + naoConfirmadasOc} item(ns) sem confirmação (✓) que não serão incluídos no relatório. Deseja salvar mesmo assim?",
                        PrimaryButtonText = "Salvar sem eles",
                        CloseButtonText = "Cancelar",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.XamlRoot
                    };
                    if (await aviso.ShowAsync() != ContentDialogResult.Primary) return;
                }

                foreach (var a in _atividades.Where(a => _atividadesConfirmadas.Contains(a)))
                { a.RelatorioId = relatorio.Id; db.Atividades.Add(a); }

                foreach (var o in _ocorrencias.Where(o => _ocorrenciasConfirmadas.Contains(o)))
                { o.RelatorioId = relatorio.Id; db.Ocorrencias.Add(o); }

                var pastaFotos = PathIO.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData), "RDOApp", "Fotos");
                Directory.CreateDirectory(pastaFotos);
                foreach (var f in _fotos)
                {
                    var destino = PathIO.Combine(pastaFotos, PathIO.GetFileName(f.CaminhoArquivo));
                    if (File.Exists(f.CaminhoArquivo) &&
                        !string.Equals(PathIO.GetFullPath(f.CaminhoArquivo), PathIO.GetFullPath(destino), StringComparison.OrdinalIgnoreCase))
                        File.Copy(f.CaminhoArquivo, destino, overwrite: true);
                    f.RelatorioId = relatorio.Id;
                    f.CaminhoArquivo = destino;
                    db.Fotos.Add(new Foto { RelatorioId = relatorio.Id, CaminhoArquivo = f.CaminhoArquivo, Legenda = f.Legenda, TiradaEm = f.TiradaEm });
                }

                foreach (var eqId in _equipamentoIds)
                {
                    db.RelatorioEquipamentos.Add(new RelatorioEquipamento
                    {
                        RelatorioId = relatorio.Id,
                        EquipamentoCadastradoId = eqId
                    });
                }

                foreach (var acId in _acompanhanteIds)
                {
                    db.RelatorioAcompanhantes.Add(new RelatorioAcompanhante
                    {
                        RelatorioId = relatorio.Id,
                        AcompanhanteId = acId
                    });
                }

                // Salvar funcionários da equipe como registros de Assinatura
                if (_funcionarioIds.Count > 0)
                {
                    var funcionarios = db.Funcionarios
                        .Where(f => _funcionarioIds.Contains(f.Id))
                        .ToList();
                    foreach (var func in funcionarios)
                    {
                        string horaE = "08:00", horaS = "17:00", horaI = "01:00";
                        if (_horasFuncionario.TryGetValue(func.Id, out var h))
                        {
                            horaE = h.entrada.Time.ToString(@"hh\:mm");
                            horaS = h.saida.Time.ToString(@"hh\:mm");
                            horaI = h.intervalo.Time.ToString(@"hh\:mm");
                        }
                        db.Assinaturas.Add(new Assinatura
                        {
                            RelatorioId = relatorio.Id,
                            NomeAssinante = func.Nome,
                            Cargo = func.Funcao,
                            CPF = "",
                            FuncionarioId = func.Id,
                            DataAssinatura = DateTime.Now,
                            Assinado = true,
                            HoraEntrada = horaE,
                            HoraSaida = horaS,
                            HoraIntervalo = horaI
                        });
                    }
                }

                await db.SaveChangesAsync();

                _savedRelatorioId = relatorio.Id;
                MainPage.ShowMeusRelatoriosOnNavigate = true;

                var dialog = new ContentDialog
                {
                    Title = _editRelatorioId > 0 ? "RDO atualizado!" : "RDO salvo!",
                    Content = $"Relatório nº {relatorio.Numero:D3} salvo com sucesso.",
                    PrimaryButtonText = "Exportar PDF",
                    CloseButtonText = "Fechar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                    await ExportarPdf(_savedRelatorioId);
                Frame.GoBack();
            }
            catch (Exception ex)
            {
                var causa = ex.InnerException?.Message ?? ex.Message;
                await MostrarErro($"Erro ao salvar: {causa}");
            }
        }

        // ── VOLTAR + RASCUNHO ─────────────────────────────────────────────────
        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
        {
            bool temConteudo =
                _funcionarioIds.Count > 0 ||
                _equipamentoIds.Count > 0 ||
                _acompanhanteIds.Count > 0 ||
                _atividades.Count > 0 ||
                _ocorrencias.Count > 0 ||
                _fotos.Count > 0 ||
                ManhaEnsolarado.IsChecked == true || ManhaNublado.IsChecked == true || ManhaChuvoso.IsChecked == true ||
                TardeEnsolarado.IsChecked == true || TardeNublado.IsChecked == true || TardeChuvoso.IsChecked == true ||
                NoiteEnsolarado.IsChecked == true || NoiteNublado.IsChecked == true || NoiteChuvoso.IsChecked == true;

            if (temConteudo)
                SalvarRascunho();

            Frame.GoBack();
        }

        private void SalvarRascunho()
        {
            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());

                var anterior = db.Relatorios
                    .FirstOrDefault(r => r.ObraId == _obraId && r.Rascunho);
                if (anterior != null)
                {
                    db.Climas.RemoveRange(db.Climas.Where(c => c.RelatorioId == anterior.Id));
                    db.Atividades.RemoveRange(db.Atividades.Where(a => a.RelatorioId == anterior.Id));
                    db.Ocorrencias.RemoveRange(db.Ocorrencias.Where(o => o.RelatorioId == anterior.Id));
                    db.Relatorios.Remove(anterior);
                }

                var numero = db.Relatorios
                    .Where(r => r.ObraId == _obraId && !r.Rascunho)
                    .Count() + 1;

                var rascunho = new Relatorio
                {
                    ObraId = _obraId,
                    UsuarioId = 1,
                    Numero = numero,
                    Data = DataPicker.Date?.DateTime ?? DateTime.Now,
                    ObsGerais = "",
                    Status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Rascunho",
                    AcompanhanteId = _acompanhanteIds.Count > 0 ? _acompanhanteIds[0] : (int?)null,
                    Sincronizado = false,
                    Rascunho = true,
                    CriadoEm = DateTime.Now
                };

                db.Relatorios.Add(rascunho);
                db.SaveChanges();

                void SalvarClima(string periodo, ToggleButton ens, ToggleButton nub, ToggleButton chu, ToggleButton prat, ToggleButton imprat)
                {
                    var tempo = ens.IsChecked == true ? "Ensolarado" :
                                nub.IsChecked == true ? "Nublado" :
                                chu.IsChecked == true ? "Chuvoso" : "";
                    var condicao = prat.IsChecked == true ? "Praticável" :
                                   imprat.IsChecked == true ? "Impraticável" : "";
                    if (!string.IsNullOrEmpty(tempo) || !string.IsNullOrEmpty(condicao))
                        db.Climas.Add(new ClimaDetalhe
                        {
                            RelatorioId = rascunho.Id,
                            Periodo = periodo,
                            Ativo = true,
                            Tempo = tempo,
                            Condicao = condicao
                        });
                }
                SalvarClima("Manhã", ManhaEnsolarado, ManhaNublado, ManhaChuvoso, ManhaPraticavel, ManhaImpraticavel);
                SalvarClima("Tarde", TardeEnsolarado, TardeNublado, TardeChuvoso, TardePraticavel, TardeImpraticavel);
                SalvarClima("Noite", NoiteEnsolarado, NoiteNublado, NoiteChuvoso, NoitePraticavel, NoiteImpraticavel);

                foreach (var a in _atividades)
                { a.RelatorioId = rascunho.Id; db.Atividades.Add(a); }

                foreach (var o in _ocorrencias)
                { o.RelatorioId = rascunho.Id; db.Ocorrencias.Add(o); }

                db.SaveChanges();
            }
            catch { /* silencioso */ }
        }

        // ── HELPERS ───────────────────────────────────────────────────────────
        // Retorna brush do tema ativo (respeitando RequestedTheme setado pelo ThemeManager)
        private static Brush TR(string key)
        {
            if ((Application.Current as App)?.MainWindow?.Content is FrameworkElement root)
            {
                var themeKey = root.ActualTheme == ElementTheme.Light ? "Light" : "Dark";
                if (Application.Current.Resources.ThemeDictionaries.TryGetValue(themeKey, out var dict)
                    && dict is ResourceDictionary rd
                    && rd.TryGetValue(key, out var res)
                    && res is Brush b)
                    return b;
            }
            return Application.Current.Resources.TryGetValue(key, out var fallback) && fallback is Brush fb
                ? fb
                : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private static void ReconstruirZebra(StackPanel panel, bool rounded = false)
        {
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is Border b)
                    b.Background = i % 2 == 1 ? TR("AppBgBrush") : TR("InputBgBrush");
            }
        }

        private static Button CriarBotaoRemover() => new Button
        {
            Content = "🗑",
            Width = 34,
            Height = 34,
            Background = new SolidColorBrush(Color.FromArgb(255, 26, 18, 18)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 58, 26, 26)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 102, 102)),
            Padding = new Thickness(0)
        };

        private static async void DestacaItensPendentes(Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Border b)
                {
                    var original = b.BorderBrush;
                    b.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0));
                    await Task.Delay(600);
                    b.BorderBrush = original;
                }
            }
        }

        private static Button CriarBotaoOrdem(string seta) => new Button
        {
            Content = seta,
            Width = 26,
            Height = 26,
            Background = new SolidColorBrush(Color.FromArgb(255, 20, 30, 50)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 40, 65, 110)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230)),
            Padding = new Thickness(0),
            FontSize = 13
        };

        private void RenumerarAtividades()
        {
            for (int i = 0; i < AtividadesPanel.Children.Count; i++)
            {
                if (AtividadesPanel.Children[i] is Border b && b.Child is Grid g &&
                    g.Children.Count > 0 && g.Children[0] is Border nb && nb.Child is TextBlock tb)
                    tb.Text = (i + 1).ToString();
            }
            ContadorAtividades.Text = AtividadesPanel.Children.Count.ToString();
        }

        private void RenumerarOcorrencias()
        {
            for (int i = 0; i < OcorrenciasPanel.Children.Count; i++)
            {
                if (OcorrenciasPanel.Children[i] is Border b && b.Child is StackPanel sp &&
                    sp.Children.Count > 0 && sp.Children[0] is Grid g &&
                    g.Children.Count > 0 && g.Children[0] is Border nb && nb.Child is TextBlock tb)
                    tb.Text = (i + 1).ToString();
            }
            ContadorOcorrencias.Text = OcorrenciasPanel.Children.Count.ToString();
        }

        private static TimePicker CriarTimePicker(int hora, int minuto) => new TimePicker
        {
            Time = new TimeSpan(hora, minuto, 0),
            Background = TR("InputBgBrush"),
            BorderBrush = TR("AppBorderBrush"),
            BorderThickness = new Thickness(1),
            Foreground = TR("TextPrimaryBrush"),
            MinWidth = 140,
            ClockIdentifier = "24HourClock"
        };

        private static StackPanel CriarCampoLabel(string label, Control input)
        {
            var sp = new StackPanel { Spacing = 6 };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 9,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = new SolidColorBrush(Color.FromArgb(255, 138, 180, 212)),
                CharacterSpacing = 100
            });
            input.HorizontalAlignment = HorizontalAlignment.Stretch;
            sp.Children.Add(input);
            return sp;
        }

        private async Task MostrarErro(string mensagem)
        {
            var dialog = new ContentDialog
            {
                Title = "Atenção",
                Content = mensagem,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ExportarPdf(int relatorioId)
        {
            try
            {
                var caminho = await RDO.App.Services.RdoPdfExportService.ExportAsync(relatorioId);
                if (caminho != null)
                    await Windows.System.Launcher.LaunchFileAsync(
                        await Windows.Storage.StorageFile.GetFileFromPathAsync(caminho));
            }
            catch (Exception ex)
            {
                await MostrarErro($"Erro ao gerar PDF: {ex.Message}");
            }
        }
    }
}