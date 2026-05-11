using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using RDO.App.Services;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace RDO.App.Views
{
    public class NovaObraEstado
    {
        public string Nome { get; set; } = "";
        public string Grupo { get; set; } = "";
        public bool GrupoEditavel { get; set; }
        public int StatusIndex { get; set; }
        public int ResponsavelIndex { get; set; }
        public string Endereco { get; set; } = "";
        public string ART { get; set; } = "";
        public DateTimeOffset DataInicio { get; set; }
        public DateTimeOffset DataTermino { get; set; }
        public string? ImagemPath { get; set; }
        public int? ObraExistenteId { get; set; }
    }

    public class CadastrosParams
    {
        public string AbaInicial { get; set; } = "";
        public string VoltarPara { get; set; } = "";
        public NovaObraEstado? EstadoNovaObra { get; set; }
    }

    public sealed partial class NovaObraPage : Page
    {
        private Obra? _obraExistente;
        private string _abaOrigem = "Obras";
        private List<Acompanhante> _terceirosCache = new();
        private Acompanhante? _responsavelClienteSelecionado;

        public NovaObraPage()
        {
            this.InitializeComponent();
            DataInicioPicker.Date = DateTimeOffset.Now;
            DataTerminoPicker.Date = DateTimeOffset.Now.AddMonths(6);
            StatusBox.SelectedIndex = 0;
            CarregarTerceiros();
        }

        private void CarregarTerceiros()
        {
            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                _terceirosCache = db.Acompanhantes
                    .Where(a => a.Ativo)
                    .OrderBy(a => a.Nome)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TERCEIROS] Erro ao carregar: {ex}");
                _terceirosCache = new List<Acompanhante>();
            }

            ResponsavelClienteBox.TextMemberPath = "Nome";
            ResponsavelClienteBox.DisplayMemberPath = "Nome";

            ResponsavelClienteBox.GotFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(ResponsavelClienteBox.Text))
                {
                    ResponsavelClienteBox.ItemsSource = _terceirosCache;
                }
            };

            // Clica no 'X': só limpa quando não veio de uma sugestão escolhida
            ResponsavelClienteBox.QuerySubmitted += (s, e) =>
            {
                if (e.ChosenSuggestion == null)
                {
                    ResponsavelClienteBox.Text = "";
                    _responsavelClienteSelecionado = null;
                    ResponsavelClienteBox.IsSuggestionListOpen = false;
                }
            };
        }

        private void ResponsavelCliente_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var termo = sender.Text.ToLower();
                if (string.IsNullOrWhiteSpace(termo))
                {
                    sender.ItemsSource = _terceirosCache;
                }
                else
                {
                    sender.ItemsSource = _terceirosCache.Where(t => t.Nome.ToLower().Contains(termo)).ToList();
                }
                _responsavelClienteSelecionado = null;
            }
        }

        private void ResponsavelCliente_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is Acompanhante terceiro)
            {
                _responsavelClienteSelecionado = terceiro;
                sender.Text = terceiro.Nome;

                if (terceiro.EmpresaId.HasValue)
                {
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    var empresa = db.Empresas.Find(terceiro.EmpresaId.Value);
                    if (empresa != null)
                    {
                        GrupoBox.IsReadOnly = true;
                        GrupoBox.Text = LogoService.GetBaseNome(empresa.Nome);
                        GrupoBox.PlaceholderText = "";

                        // Extrai "YYY | ZZ" do formato "XXX | YYY (ZZ)"
                        var pipeIdx = empresa.Nome.IndexOf(" | ", StringComparison.Ordinal);
                        if (pipeIdx >= 0)
                        {
                            var localPart = empresa.Nome[(pipeIdx + 3)..];
                            if (localPart.EndsWith(")") && localPart.Contains(" ("))
                            {
                                var ufIdx = localPart.LastIndexOf(" (");
                                var cidade = localPart[..ufIdx].Trim();
                                var uf = localPart[(ufIdx + 2)..^1].Trim();
                                EnderecoBox.Text = $"{cidade} | {uf}";
                            }
                            else
                            {
                                EnderecoBox.Text = localPart;
                            }
                        }
                        EnderecoBox.IsReadOnly = true;
                        return;
                    }
                }

                if (terceiro.Nome == "-")
                {
                    GrupoBox.IsReadOnly = false;
                    GrupoBox.Text = "";
                    GrupoBox.PlaceholderText = "Ex: Cargill, Siemens...";
                    EnderecoBox.IsReadOnly = false;
                }
                else
                {
                    GrupoBox.IsReadOnly = true;
                    GrupoBox.Text = terceiro.Grupo;
                    EnderecoBox.IsReadOnly = true;
                }
            }
        }

        private void BtnEditarGrupoLocal_Click(object sender, RoutedEventArgs e)
        {
            GrupoBox.IsReadOnly = false;
            EnderecoBox.IsReadOnly = false;
            _responsavelClienteSelecionado = null;
            ResponsavelClienteBox.Text = "";
        }

        private bool _mostrандоAvisoGrupo = false;

        private async void GrupoBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Se o campo está bloqueado e não está já mostrando o aviso
            if (GrupoBox.IsReadOnly && !_mostrандоAvisoGrupo)
            {
                _mostrандоAvisoGrupo = true;
                var dialog = new ContentDialog
                {
                    Title = "Campo bloqueado",
                    Content = "O grupo é preenchido automaticamente ao selecionar o Responsável Cliente.\n\nPara editar manualmente, selecione o terceiro \"-\" na lista.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                _mostrандоAvisoGrupo = false;
                // Devolve foco para o ComboBox sem disparar GotFocus do GrupoBox
                ResponsavelClienteBox.Focus(FocusState.Programmatic);
            }
        }

        private void CadastrarTerceiro_Click(object sender, RoutedEventArgs e)
        {
            // Salva o estado atual do formulário antes de navegar
            var estadoAtual = new NovaObraEstado
            {
                Nome = NomeBox.Text,
                Grupo = GrupoBox.Text,
                GrupoEditavel = !GrupoBox.IsReadOnly,
                StatusIndex = StatusBox.SelectedIndex,
                ResponsavelIndex = ResponsavelBox.SelectedIndex,
                Endereco = EnderecoBox.Text,
                ART = ArtBox.Text,
                DataInicio = DataInicioPicker.Date,
                DataTermino = DataTerminoPicker.Date,
                ObraExistenteId = _obraExistente?.Id
            };

            // Navega para cadastros com parâmetro para voltar
            Frame.Navigate(typeof(CadastrosPage), new CadastrosParams
            {
                AbaInicial = "Terceiros",
                VoltarPara = "NovaObra",
                EstadoNovaObra = estadoAtual
            });
        }

        protected override async void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is NovaObraParams p)
            {
                _abaOrigem = p.AbaOrigem;
                if (p.ObraId.HasValue)
                    await CarregarObra(p.ObraId.Value);
            }
            else if (e.Parameter is NovaObraEstado estado)
            {
                // Restaura estado após voltar de Cadastros
                await RestaurarEstado(estado);
            }
            else if (e.Parameter is int obraId)
            {
                await CarregarObra(obraId);
            }
        }

        private async Task RestaurarEstado(NovaObraEstado estado)
        {
            NomeBox.Text = estado.Nome;
            GrupoBox.Text = estado.Grupo;
            GrupoBox.IsReadOnly = !estado.GrupoEditavel;
            StatusBox.SelectedIndex = estado.StatusIndex;
            ResponsavelBox.SelectedIndex = estado.ResponsavelIndex;
            EnderecoBox.Text = estado.Endereco;
            ArtBox.Text = estado.ART;
            DataInicioPicker.Date = estado.DataInicio;
            DataTerminoPicker.Date = estado.DataTermino;

            if (estado.ObraExistenteId.HasValue)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                _obraExistente = db.Obras.Find(estado.ObraExistenteId.Value);
                if (_obraExistente != null)
                {
                    TituloTexto.Text = "Editar Obra";
                    SalvarBtn.Content = "Salvar Alterações";
                }
            }

            // Recarrega terceiros
            CarregarTerceiros();
        }

        private async Task CarregarObra(int obraId)
        {
            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var obra = db.Obras.Find(obraId);
                if (obra == null) return;

                _obraExistente = obra;

                TituloTexto.Text = "Editar Obra";
                SalvarBtn.Content = "Salvar Alterações";

                NomeBox.Text = obra.Nome;
                GrupoBox.Text = obra.Grupo;
                EnderecoBox.Text = obra.Endereco;
                ArtBox.Text = obra.ART;
                DataInicioPicker.Date = obra.DataInicio;
                DataTerminoPicker.Date = obra.PrevisaoTermino ?? DateTimeOffset.Now.AddMonths(6);

                for (int i = 0; i < StatusBox.Items.Count; i++)
                {
                    if ((StatusBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.Status)
                    { StatusBox.SelectedIndex = i; break; }
                }

                for (int i = 0; i < ResponsavelBox.Items.Count; i++)
                {
                    if ((ResponsavelBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.Responsavel)
                    { ResponsavelBox.SelectedIndex = i; break; }
                }

                // Auto-seleciona responsável cliente se houver match
                if (!string.IsNullOrEmpty(obra.ResponsavelCliente))
                {
                    var acom = _terceirosCache.FirstOrDefault(t => t.Nome == obra.ResponsavelCliente);
                    if (acom != null)
                    {
                        _responsavelClienteSelecionado = acom;
                        ResponsavelClienteBox.Text = acom.Nome;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CARREGAR OBRA] Erro: {ex}");
                await MostrarErro(AppErrorCodes.DB_001, ex);
            }
        }



        private async void CriarBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NomeBox.Text))
            {
                await MostrarErro(AppErrorCodes.FORM_001);
                return;
            }

            try
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());

                if (_obraExistente != null)
                {
                    var item = await db.Obras.FindAsync(_obraExistente.Id);
                    if (item != null)
                    {
                        item.Nome = NomeBox.Text.Trim();
                        item.Grupo = GrupoBox.Text.Trim();
                        item.Status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Em execução";
                        item.Responsavel = (ResponsavelBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                        item.ResponsavelCliente = _responsavelClienteSelecionado != null ? _responsavelClienteSelecionado.Nome : ResponsavelClienteBox.Text.Trim();
                        item.Endereco = EnderecoBox.Text.Trim();
                        item.ART = ArtBox.Text.Trim();
                        item.DataInicio = DataInicioPicker.Date.DateTime;
                        item.PrevisaoTermino = DataTerminoPicker.Date.DateTime;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync();
                    AppLogger.LogInfo("DB", $"Obra atualizada: \"{NomeBox.Text.Trim()}\"  id={_obraExistente.Id}");

                    var dialog = new ContentDialog
                    {
                        Title = "Obra atualizada!",
                        Content = $"A obra \"{NomeBox.Text.Trim()}\" foi atualizada com sucesso.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
                else
                {
                    var obra = new Obra
                    {
                        Nome = NomeBox.Text.Trim(),
                        Grupo = GrupoBox.Text.Trim(),
                        Status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Em execução",
                        Responsavel = (ResponsavelBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                        ResponsavelCliente = _responsavelClienteSelecionado != null ? _responsavelClienteSelecionado.Nome : ResponsavelClienteBox.Text.Trim(),
                        Endereco = EnderecoBox.Text.Trim(),
                        ART = ArtBox.Text.Trim(),
                        DataInicio = DataInicioPicker.Date.DateTime,
                        PrevisaoTermino = DataTerminoPicker.Date.DateTime,
                        Ativo = true
                    };
                    db.Obras.Add(obra);
                    await db.SaveChangesAsync();
                    AppLogger.LogInfo("DB", $"Obra criada: \"{obra.Nome}\"  id={obra.Id}");

                    var dialog = new ContentDialog
                    {
                        Title = "Obra criada!",
                        Content = $"A obra \"{obra.Nome}\" foi cadastrada com sucesso.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                Frame.BackStack.Clear();
                Frame.Navigate(typeof(MainPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOVA OBRA] Erro ao salvar: {ex}");
                await MostrarErro(AppErrorCodes.DB_002, ex);
            }
        }

        private async System.Threading.Tasks.Task MostrarErro(string code, Exception? ex = null)
            => await ErrorDialogService.ShowAsync(this.XamlRoot, code, null, ex);

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}