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
using RDO.App.Services;
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
        public bool EditarRevisaoAtual { get; set; } // true = não incrementa revisão
    }

    public sealed partial class RdoFormPage : Page
    {
        private int _obraId;
        private int _savedRelatorioId;
        private int _editRelatorioId; // 0 = novo, >0 = editar relatório existente
        private bool _editarRevisaoAtual; // true = não incrementa revisão ao salvar
        private readonly List<Atividade> _atividades = new();
        private readonly Dictionary<Atividade, List<Atividade>> _subAtividades = new();
        private readonly HashSet<Atividade> _subAtividadesConfirmadas = new();
        private readonly Dictionary<Atividade, StackPanel> _subPanels = new();
        private readonly List<Ocorrencia> _ocorrencias = new();
        private readonly List<Foto> _fotos = new();
        private readonly List<Foto> _documentos = new();
        private readonly List<int> _funcionarioIds = new();
        private readonly List<int> _equipamentoIds = new();
        private readonly List<int> _acompanhanteIds = new();
        private readonly Dictionary<int, (TimePicker entrada, TimePicker saida, TimePicker intervalo)> _horasFuncionario = new();
        private readonly HashSet<Atividade> _atividadesConfirmadas = new();
        private readonly HashSet<Ocorrencia> _ocorrenciasConfirmadas = new();
        private DispatcherTimer? _pulseTimer;
        private bool _pulseState;
        private bool _isInitialized;

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
            // Retornando de CadastrosPage via GoBack — preserva todo o estado do formulário
            if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back && _isInitialized)
                return;

            // Nova navegação: sempre limpa o formulário (evita dados do relatório anterior)
            LimparFormulario();

            int obraId = 0;
            if (e.Parameter is int id)
            {
                obraId = id;
            }
            else if (e.Parameter is RdoFormParams p)
            {
                obraId = p.ObraId;
                _editRelatorioId = p.RelatorioId;
                _editarRevisaoAtual = p.EditarRevisaoAtual;
            }

            if (obraId == 0) return;
            _obraId = obraId;
            _isInitialized = true;

            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var obra = db.Obras.Find(obraId);
            if (obra != null)
            {
                TituloObra.Text = obra.Nome;
                if (_editRelatorioId > 0)
                {
                    var relEdicao = db.Relatorios.Find(_editRelatorioId);
                    NumeroRdoTexto.Text = relEdicao != null ? $"Nº {relEdicao.Numero:D3}" : "—";
                    // Mostra revisão atual
                    if (relEdicao != null && relEdicao.Revisao >= 0)
                    {
                        RevisaoBadge.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        RevisaoTexto.Text = _editarRevisaoAtual
                            ? $"Rev. {relEdicao.Revisao:D2} (editando)"
                            : $"Rev. {relEdicao.Revisao:D2} → Rev. {relEdicao.Revisao + 1:D2}";
                    }
                }
                else
                {
                    var numero = db.Relatorios.Count(r => r.ObraId == obraId && !r.Rascunho && !r.IsDeleted) + 1;
                    NumeroRdoTexto.Text = $"Nº {numero:D3}";
                    // Novo relatório sempre começa em Rev. 00
                    RevisaoBadge.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                ResponsavelText.Text = string.IsNullOrEmpty(obra.Responsavel) ? "Não definido" : obra.Responsavel;
                ResponsavelClienteText.Text = string.IsNullOrEmpty(obra.ResponsavelCliente) ? "Não definido" : obra.ResponsavelCliente;
                AtualizarCrea(obra.Responsavel);
            }

            if (_editRelatorioId > 0)
                CarregarRelatorioParaEdicao(_editRelatorioId);
        }

        private void LimparFormulario()
        {
            PararPulse();

            // Esconde badge de revisão (será reconfigurado ao carregar)
            if (RevisaoBadge != null)
                RevisaoBadge.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

            // Coleções internas
            _atividades.Clear();
            _ocorrencias.Clear();
            _fotos.Clear();
            _documentos.Clear();
            _funcionarioIds.Clear();
            _equipamentoIds.Clear();
            _acompanhanteIds.Clear();
            _horasFuncionario.Clear();
            _atividadesConfirmadas.Clear();
            _subAtividades.Clear();
            _subAtividadesConfirmadas.Clear();
            _subPanels.Clear();
            _ocorrenciasConfirmadas.Clear();
            _savedRelatorioId = 0;
            _editRelatorioId = 0;
            _isInitialized = false;

            // Painéis dinâmicos
            FuncionariosPanel.Children.Clear();
            EquipamentosPanel.Children.Clear();
            AcompanhantesPanel.Children.Clear();
            AtividadesPanel.Children.Clear();
            OcorrenciasPanel.Children.Clear();
            FotosWrapPanel.Children.Clear();
            DocumentosPanel.Children.Clear();

            // Contadores
            ContadorEquipe.Text = "0";
            ContadorEquipamentos.Text = "0";
            ContadorAcompanhantes.Text = "0";
            ContadorAtividades.Text = "0";
            ContadorOcorrencias.Text = "0";
            ContadorFotos.Text = "0";
            ContadorDocumentos.Text = "0";

            // Clima
            ManhaEnsolarado.IsChecked = false; ManhaNublado.IsChecked = false; ManhaChuvoso.IsChecked = false;
            ManhaPraticavel.IsChecked = false; ManhaImpraticavel.IsChecked = false;
            TardeEnsolarado.IsChecked = false; TardeNublado.IsChecked = false; TardeChuvoso.IsChecked = false;
            TardePraticavel.IsChecked = false; TardeImpraticavel.IsChecked = false;
            NoiteEnsolarado.IsChecked = false; NoiteNublado.IsChecked = false; NoiteChuvoso.IsChecked = false;
            NoitePraticavel.IsChecked = false; NoiteImpraticavel.IsChecked = false;

            // Campos de obra (serão recarregados pelo OnNavigatedTo)
            TituloObra.Text = "";
            NumeroRdoTexto.Text = "—";
            ResponsavelText.Text = "—";
            ResponsavelClienteText.Text = "—";
            CreaTexto.Text = "—";

            // Data e status
            DataPicker.Date = DateTimeOffset.Now;
            DataTexto.Text = DateTime.Now.ToString("dd/MM/yyyy");
            StatusBox.SelectedIndex = 0;
        }

        private void CarregarRelatorioParaEdicao(int relatorioId)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var rel = db.Relatorios
                .Include(r => r.WeatherDetails)
                .Include(r => r.Activities)
                .Include(r => r.Occurrences)
                .Include(r => r.Signatures)
                .Include(r => r.Equipments)
                .Include(r => r.Photos)
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

            var dbIdToAtividade = new Dictionary<int, Atividade>();
            foreach (var a in rel.Atividades.Where(a => a.ParentId == null))
            {
                var nova = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _atividades.Add(nova);
                AdicionarLinhaAtividade(nova, preConfirm: true);
                dbIdToAtividade[a.Id] = nova;
            }
            foreach (var a in rel.Atividades.Where(a => a.ParentId != null))
            {
                if (!dbIdToAtividade.TryGetValue(a.ParentId!.Value, out var pai)) continue;
                if (!_subPanels.TryGetValue(pai, out var sp)) continue;
                var sub = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _subAtividades[pai].Add(sub);
                AdicionarLinhaSubAtividade(pai, sub, sp, preConfirm: true);
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

            foreach (var f in rel.Fotos.Where(f => f.Type != "document"))
            {
                var fotoExistente = new Foto { CaminhoArquivo = f.CaminhoArquivo, Legenda = f.Legenda, TiradaEm = f.TiradaEm };
                _fotos.Add(fotoExistente);
                AdicionarFotoExistente(fotoExistente);
            }

            foreach (var d in rel.Fotos.Where(f => f.Type == "document"))
            {
                var docExistente = new Foto { CaminhoArquivo = d.CaminhoArquivo, Legenda = d.Legenda, TiradaEm = d.TiradaEm };
                _documentos.Add(docExistente);
                AdicionarDocumentoExistente(docExistente);
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

        private void AdicionarDocumentoExistente(Foto doc)
        {
            var card = CriarCardDocumento(doc);
            if (card == null) return;
            DocumentosPanel.Children.Add(card);
            ContadorDocumentos.Text = DocumentosPanel.Children.Count.ToString();
        }

        private Border? CriarCardDocumento(Foto doc)
        {
            var nomeArquivo = PathIO.GetFileName(doc.CaminhoArquivo);
            var extensao = PathIO.GetExtension(doc.CaminhoArquivo).TrimStart('.').ToUpper();

            var card = new Border
            {
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 30, 45, 74)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 8, 8)
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            // Ícone / badge de extensão
            var iconBorder = new Border
            {
                Width = 28, Height = 28,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(255, 0, 82, 204)),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = new TextBlock
            {
                Text = extensao.Length > 0 ? extensao.Substring(0, Math.Min(3, extensao.Length)) : "DOC",
                FontSize = 8,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBorder, 0);

            // Nome do arquivo + legenda
            var infoStack = new StackPanel { Margin = new Thickness(10, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            infoStack.Children.Add(new TextBlock
            {
                Text = nomeArquivo,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = new SolidColorBrush(Colors.White)
            });
            var legendaBox = new TextBox
            {
                PlaceholderText = "Descrição...",
                Text = doc.Legenda ?? "",
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 160, 180, 210)),
                Padding = new Thickness(0, 2, 0, 0)
            };
            legendaBox.TextChanged += (s, ev) => doc.Legenda = legendaBox.Text;
            infoStack.Children.Add(legendaBox);
            Grid.SetColumn(infoStack, 1);

            // Botão remover
            var remBtn = new Button
            {
                Content = "✕",
                Width = 26, Height = 26,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(60, 200, 60, 60)),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new Thickness(0),
                FontSize = 10
            };
            remBtn.Click += (s, ev) =>
            {
                _documentos.Remove(doc);
                DocumentosPanel.Children.Remove(card);
                ContadorDocumentos.Text = DocumentosPanel.Children.Count.ToString();
            };
            Grid.SetColumn(remBtn, 2);

            row.Children.Add(iconBorder);
            row.Children.Add(infoStack);
            row.Children.Add(remBtn);
            card.Child = row;
            return card;
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
            var rascIdToAtividade = new Dictionary<int, Atividade>();
            foreach (var a in atividades.Where(a => a.ParentId == null))
            {
                var nova = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _atividades.Add(nova);
                AdicionarLinhaAtividade(nova);
                rascIdToAtividade[a.Id] = nova;
            }
            foreach (var a in atividades.Where(a => a.ParentId != null))
            {
                if (!rascIdToAtividade.TryGetValue(a.ParentId!.Value, out var pai)) continue;
                if (!_subPanels.TryGetValue(pai, out var sp)) continue;
                var sub = new Atividade { Descricao = a.Descricao, Local = a.Local, Status = a.Status };
                _subAtividades[pai].Add(sub);
                AdicionarLinhaSubAtividade(pai, sub, sp);
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

        private void AtualizarCrea(string? nome)
        {
            var nomeStr = nome ?? "";
            CreaTexto.Text = _creaMap.TryGetValue(nomeStr, out var crea) ? crea : "—";
        }

        private async void Responsaveis_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Campo bloqueado",
                Content = "Para alterar os responsáveis do relatório, altere os responsáveis no cadastro da Obra correspondente. As informações são preenchidas automaticamente a partir do cadastro da obra.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
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
                Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
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
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
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
                Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
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
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
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
                Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
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
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            var nomeStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
            nomeStack.Children.Add(new TextBlock
            {
                Text = ac.Nome,
                FontSize = 13,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("TextPrimaryBrush")
            });

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
            Grid.SetColumn(grupoBadge, 1);
            Grid.SetColumn(remBtn, 2);
            grid.Children.Add(nomeStack);

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
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
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
                Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
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
                RealtarCard(border);
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
                RealtarCard(border);
            };

            // Painel de sub-itens (aparece ao confirmar)
            var subPanel = new StackPanel { Spacing = 4, Margin = new Thickness(30, 6, 0, 0) };
            _subPanels[atividade] = subPanel;
            if (!_subAtividades.ContainsKey(atividade))
                _subAtividades[atividade] = new List<Atividade>();

            // Botão adicionar sub-item
            var addSubBtn = new Button
            {
                Content = "+ sub-item",
                FontSize = 11,
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush(Color.FromArgb(255, 20, 30, 55)),
                BorderBrush = TR("AppBorderBrush"),
                BorderThickness = new Thickness(1),
                Foreground = TR("TextSecondaryBrush"),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Margin = new Thickness(30, 4, 0, 0),
                Visibility = Visibility.Collapsed
            };
            addSubBtn.Click += (s, ev) =>
            {
                var sub = new Atividade { Status = "Em andamento" };
                _subAtividades[atividade].Add(sub);
                AdicionarLinhaSubAtividade(atividade, sub, subPanel);
            };

            void ConfirmarEstado()
            {
                _atividadesConfirmadas.Add(atividade);
                descBox.IsReadOnly = true;
                descBox.Opacity = 0.75;
                saveBtn.Content = "✏";
                saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
                ToolTipService.SetToolTip(saveBtn, "Editar descrição");
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
                upBtn.Visibility = Visibility.Visible;
                downBtn.Visibility = Visibility.Visible;
                addSubBtn.Visibility = Visibility.Visible;
            }

            void EditarEstado()
            {
                _atividadesConfirmadas.Remove(atividade);
                descBox.IsReadOnly = false;
                descBox.Opacity = 1.0;
                saveBtn.Content = "✓";
                saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30));
                saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80));
                saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100));
                ToolTipService.SetToolTip(saveBtn, "Confirmar atividade");
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235));
                upBtn.Visibility = Visibility.Collapsed;
                downBtn.Visibility = Visibility.Collapsed;
                addSubBtn.Visibility = Visibility.Collapsed;
            }

            if (preConfirm)
                ConfirmarEstado();

            saveBtn.Click += (s, ev) =>
            {
                if (!_atividadesConfirmadas.Contains(atividade))
                    ConfirmarEstado();
                else
                    EditarEstado();
            };

            var remBtn = CriarBotaoRemover();
            remBtn.VerticalAlignment = VerticalAlignment.Top;
            remBtn.Click += (s, ev) =>
            {
                _atividades.Remove(atividade);
                _atividadesConfirmadas.Remove(atividade);
                _subAtividades.Remove(atividade);
                _subPanels.Remove(atividade);
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

            var outer = new StackPanel { Spacing = 0 };
            outer.Children.Add(row);
            outer.Children.Add(subPanel);
            outer.Children.Add(addSubBtn);

            border.Child = outer;
            AtividadesPanel.Children.Add(border);
            ContadorAtividades.Text = AtividadesPanel.Children.Count.ToString();
        }

        private void AdicionarLinhaSubAtividade(Atividade pai, Atividade sub, StackPanel subPanel, bool preConfirm = false)
        {
            var paiIdx = _atividades.IndexOf(pai) + 1;
            var subIdx = _subAtividades[pai].IndexOf(sub) + 1;

            var subRow = new Grid { ColumnSpacing = 6 };
            subRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            subRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            subRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            subRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });

            var letraBadge = new TextBlock
            {
                Text = $"{paiIdx}.{subIdx}",
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = TR("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var descBox = new TextBox
            {
                PlaceholderText = "Sub-item...",
                AcceptsReturn = false,
                MinHeight = 30,
                TextWrapping = TextWrapping.Wrap,
                Background = TR("InputBgBrush"),
                BorderBrush = TR("AppBorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Foreground = TR("TextPrimaryBrush"),
                Padding = new Thickness(8, 6, 8, 6),
                FontSize = 12
            };
            descBox.TextChanged += (s, ev) => sub.Descricao = descBox.Text;
            if (!string.IsNullOrEmpty(sub.Descricao))
                descBox.Text = sub.Descricao;

            var saveBtn = new Button
            {
                Content = "✓", Width = 28, Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80)),
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100)),
                Padding = new Thickness(0), FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top
            };

            var remBtn = new Button
            {
                Content = "🗑", Width = 28, Height = 28,
                Background = TR("DangerBgBrush"),
                BorderBrush = TR("DangerBorderBrush"),
                Foreground = TR("DangerFgBrush"),
                Padding = new Thickness(0), FontSize = 11,
                VerticalAlignment = VerticalAlignment.Top
            };

            void ConfirmarSub()
            {
                _subAtividadesConfirmadas.Add(sub);
                descBox.IsReadOnly = true;
                descBox.Opacity = 0.75;
                saveBtn.Content = "✏";
                saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 20, 40, 75));
                saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 90, 150));
                saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 160, 230));
            }

            if (preConfirm) ConfirmarSub();

            saveBtn.Click += (s, ev) =>
            {
                if (!_subAtividadesConfirmadas.Contains(sub)) ConfirmarSub();
                else
                {
                    _subAtividadesConfirmadas.Remove(sub);
                    descBox.IsReadOnly = false;
                    descBox.Opacity = 1.0;
                    saveBtn.Content = "✓";
                    saveBtn.Background = new SolidColorBrush(Color.FromArgb(255, 10, 60, 30));
                    saveBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 180, 80));
                    saveBtn.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 220, 100));
                }
            };

            remBtn.Click += (s, ev) =>
            {
                _subAtividades[pai].Remove(sub);
                _subAtividadesConfirmadas.Remove(sub);
                subPanel.Children.Remove(subRow);
                RenumerarSubAtividades(pai, subPanel);
            };

            Grid.SetColumn(letraBadge, 0);
            Grid.SetColumn(descBox, 1);
            Grid.SetColumn(saveBtn, 2);
            Grid.SetColumn(remBtn, 3);
            subRow.Children.Add(letraBadge);
            subRow.Children.Add(descBox);
            subRow.Children.Add(saveBtn);
            subRow.Children.Add(remBtn);
            subPanel.Children.Add(subRow);
        }

        private void RenumerarSubAtividades(Atividade pai, StackPanel subPanel)
        {
            var paiIdx = _atividades.IndexOf(pai) + 1;
            for (int i = 0; i < subPanel.Children.Count; i++)
            {
                if (subPanel.Children[i] is Grid g && g.Children.Count > 0 && g.Children[0] is TextBlock tb)
                    tb.Text = $"{paiIdx}.{i + 1}";
            }
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

            var 
                sRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20, Margin = new Thickness(38, 0, 0, 0) };

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

            sRow.Children.Add(inicioStack);
            sRow.Children.Add(fimStack);
            mainStack.Children.Add(topRow);
            mainStack.Children.Add(sRow);
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


        private async void AdicionarDocumento_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".doc");
            picker.FileTypeFilter.Add(".docx");
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            var doc = new Foto
            {
                CaminhoArquivo = file.Path,
                TiradaEm = DateTime.Now,
                Type = "document"
            };
            _documentos.Add(doc);

            var card = CriarCardDocumento(doc);
            if (card == null) return;
            DocumentosPanel.Children.Add(card);
            ContadorDocumentos.Text = DocumentosPanel.Children.Count.ToString();
        }

        // ── SALVAR ────────────────────────────────────────────────────────────
        private async void SalvarBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var statusEscolhido = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                if (statusEscolhido != "Publicado")
                {
                    await MostrarErro(AppErrorCodes.FORM_001);
                    return;
                }

                using var db = new RdoDbContext(DbContextHelper.GetOptions());

                Relatorio relatorio;
                if (_editRelatorioId > 0)
                {
                    // ── Modo edição ──────────────────────────────────────────
                    relatorio = await db.Relatorios.FindAsync(_editRelatorioId)
                        ?? throw new Exception("Relatório não encontrado.");
                    relatorio.Data = DataPicker.Date?.DateTime ?? DateTime.Now;
                    relatorio.Status = statusEscolhido;
                    relatorio.AcompanhanteId = null;
                    relatorio.Rascunho = false;
                    relatorio.UpdatedAt = RDO.app.Services.SyncService.GetPushTimestamp();
                    relatorio.IsSynced = false;
                    if (!_editarRevisaoAtual)
                        relatorio.Revisao = relatorio.Revisao + 1;

                    // Remove dependentes antigos — serão salvos no SaveChangesAsync final
                    db.Climas.RemoveRange(db.Climas.Where(c => c.RelatorioId == _editRelatorioId));
                    db.Atividades.RemoveRange(db.Atividades.Where(a => a.RelatorioId == _editRelatorioId));
                    db.Ocorrencias.RemoveRange(db.Ocorrencias.Where(o => o.RelatorioId == _editRelatorioId));
                    db.Assinaturas.RemoveRange(db.Assinaturas.Where(a => a.RelatorioId == _editRelatorioId));
                    db.Fotos.RemoveRange(db.Fotos.Where(f => f.RelatorioId == _editRelatorioId));
                    db.RelatorioEquipamentos.RemoveRange(db.RelatorioEquipamentos.Where(re => re.RelatorioId == _editRelatorioId));
                    db.RelatorioAcompanhantes.RemoveRange(db.RelatorioAcompanhantes.Where(ra => ra.RelatorioId == _editRelatorioId));

                    // Salva remoções e atualização do relatório para liberar as FKs
                    // antes de reinserir os dependentes
                    await db.SaveChangesAsync(); // ← (1/2) necessário para evitar conflito de FK
                }
                else
                {
                    // ── Modo novo ────────────────────────────────────────────
                    var numero = db.Relatorios.Count(r => r.ObraId == _obraId && !r.Rascunho && !r.IsDeleted) + 1;
                    relatorio = new Relatorio
                    {
                        ObraId = _obraId,
                        UsuarioId = ObterUsuarioLogadoId(),
                        Numero = numero,
                        Data = DataPicker.Date?.DateTime ?? DateTime.Now,
                        ObsGerais = "",
                        Status = statusEscolhido,
                        AcompanhanteId = null,
                        Sincronizado = false,
                        Rascunho = false,
                        CriadoEm = DateTime.UtcNow
                    };
                    db.Relatorios.Add(relatorio);

                    // Necessário para obter relatorio.Id antes de inserir dependentes
                    await db.SaveChangesAsync(); // ← (1/2) necessário para ter o Id
                }

                // ── Adiciona dependentes usando relatorio.Id já disponível ──

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

                // Passo 1: salva atividades raiz para obter seus IDs
                foreach (var a in _atividades.Where(a => _atividadesConfirmadas.Contains(a)))
                {
                    a.RelatorioId = relatorio.Id;
                    a.ParentId = null;
                    db.Atividades.Add(a);
                }
                await db.SaveChangesAsync(); // IDs necessários para os sub-itens

                // Passo 2: salva sub-atividades confirmadas com ParentId definido
                foreach (var a in _atividades.Where(a => _atividadesConfirmadas.Contains(a)))
                {
                    if (!_subAtividades.TryGetValue(a, out var subs)) continue;
                    foreach (var sub in subs.Where(s => _subAtividadesConfirmadas.Contains(s)))
                    {
                        sub.RelatorioId = relatorio.Id;
                        sub.ParentId = a.Id;
                        db.Atividades.Add(sub);
                    }
                }

                foreach (var o in _ocorrencias.Where(o => _ocorrenciasConfirmadas.Contains(o)))
                {
                    o.RelatorioId = relatorio.Id;
                    db.Ocorrencias.Add(o);
                }

                var pastaFotos = PathIO.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RDOApp", "Fotos");
                Directory.CreateDirectory(pastaFotos);
                foreach (var f in _fotos)
                {
                    var destino = PathIO.Combine(pastaFotos, PathIO.GetFileName(f.CaminhoArquivo));
                    if (File.Exists(f.CaminhoArquivo) &&
                        !string.Equals(PathIO.GetFullPath(f.CaminhoArquivo),
                                       PathIO.GetFullPath(destino),
                                       StringComparison.OrdinalIgnoreCase))
                        File.Copy(f.CaminhoArquivo, destino, overwrite: true);

                    db.Fotos.Add(new Foto
                    {
                        RelatorioId = relatorio.Id,
                        CaminhoArquivo = destino,
                        Legenda = f.Legenda,
                        TiradaEm = f.TiradaEm
                    });
                }

                // Salvar documentos anexados
                var pastaDocumentos = PathIO.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RDOApp", "Documentos");
                Directory.CreateDirectory(pastaDocumentos);
                foreach (var d in _documentos)
                {
                    var destino = PathIO.Combine(pastaDocumentos, PathIO.GetFileName(d.CaminhoArquivo));
                    if (File.Exists(d.CaminhoArquivo) &&
                        !string.Equals(PathIO.GetFullPath(d.CaminhoArquivo),
                                       PathIO.GetFullPath(destino),
                                       StringComparison.OrdinalIgnoreCase))
                        File.Copy(d.CaminhoArquivo, destino, overwrite: true);

                    db.Fotos.Add(new Foto
                    {
                        RelatorioId = relatorio.Id,
                        CaminhoArquivo = destino,
                        Legenda = d.Legenda,
                        TiradaEm = d.TiradaEm,
                        Type = "document"
                    });
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
                            FuncionarioId = func.Id,
                            DataAssinatura = DateTime.Now,
                            Assinado = true,
                            HoraEntrada = horaE,
                            HoraSaida = horaS,
                            HoraIntervalo = horaI
                        });
                    }
                }

                // Salva todos os dependentes de uma vez — gera um único lote de eventos na SyncQueue
                await db.SaveChangesAsync(); // ← (2/2) único SaveChanges para todos os dependentes

                _savedRelatorioId = relatorio.Id;
                AppLogger.LogInfo("DB", $"RDO salvo: id={relatorio.Id}  nº={relatorio.Numero:D3}  obra={relatorio.ObraId}  rascunho=false");
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

                _isInitialized = false; // permite reinicialização ao abrir próximo RDO
                Frame.GoBack();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RDO FORM] Erro ao salvar: {ex}");
                await MostrarErro(AppErrorCodes.DB_002, ex);
            }
        }


        // ── VOLTAR + RASCUNHO ─────────────────────────────────────────────────
        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
        {
            // Modo edição: relatório original já existe, não gerar rascunho
            if (_editRelatorioId > 0)
            {
                Frame.GoBack();
                return;
            }

            bool temConteudo =
                _funcionarioIds.Count > 0 ||
                _equipamentoIds.Count > 0 ||
                _acompanhanteIds.Count > 0 ||
                _atividades.Count > 0 ||
                _ocorrencias.Count > 0 ||
                _fotos.Count > 0 ||
                _documentos.Count > 0 ||
                ManhaEnsolarado.IsChecked == true || ManhaNublado.IsChecked == true || ManhaChuvoso.IsChecked == true ||
                TardeEnsolarado.IsChecked == true || TardeNublado.IsChecked == true || TardeChuvoso.IsChecked == true ||
                NoiteEnsolarado.IsChecked == true || NoiteNublado.IsChecked == true || NoiteChuvoso.IsChecked == true;

            if (temConteudo)
                SalvarRascunho();

            _isInitialized = false; // permite reinicialização ao abrir próximo RDO
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
                    UsuarioId = ObterUsuarioLogadoId(),
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
                { a.RelatorioId = rascunho.Id; a.ParentId = null; db.Atividades.Add(a); }
                db.SaveChanges(); // IDs para sub-itens

                foreach (var a in _atividades)
                {
                    if (!_subAtividades.TryGetValue(a, out var subs)) continue;
                    foreach (var sub in subs)
                    { sub.RelatorioId = rascunho.Id; sub.ParentId = a.Id; db.Atividades.Add(sub); }
                }

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
                // Estrutura: Border > StackPanel outer > Grid row > Border numBadge > TextBlock
                if (AtividadesPanel.Children[i] is Border b &&
                    b.Child is StackPanel outer &&
                    outer.Children.Count > 0 &&
                    outer.Children[0] is Grid g &&
                    g.Children.Count > 0 &&
                    g.Children[0] is Border nb &&
                    nb.Child is TextBlock tb)
                    tb.Text = (i + 1).ToString();
            }
            // Atualiza também os sub-itens (seus prefixos mudam quando o pai muda de posição)
            foreach (var (ativ, sp) in _subPanels)
                RenumerarSubAtividades(ativ, sp);
            ContadorAtividades.Text = AtividadesPanel.Children.Count.ToString();
        }

        private async void RealtarCard(Border card)
        {
            var originalBorder = card.BorderBrush;
            var originalBg = card.Background;
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 250, 204, 21));
            card.Background = new SolidColorBrush(Color.FromArgb(255, 32, 28, 8));
            await Task.Delay(1100);
            card.BorderBrush = originalBorder;
            card.Background = originalBg;
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

        private async Task MostrarErro(string code, Exception? ex = null)
            => await ErrorDialogService.ShowAsync(this.XamlRoot, code, null, ex);

        private async Task ExportarPdf(int relatorioId)
        {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RDO FORM] Erro ao gerar PDF: {ex}");
                await MostrarErro(AppErrorCodes.PDF_001, ex);
            }
        }

        private static int ObterUsuarioLogadoId()
        {
            var id = LocalSettingsService.Get<int?>("UsuarioId");
            return id ?? 1;
        }
    }
}