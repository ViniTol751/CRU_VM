using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace RDO.App.Views
{
    public class NovaObraParams
    {
        public int? ObraId { get; set; }
        public string AbaOrigem { get; set; } = "Obras";
    }

    public sealed partial class CadastrosPage : Page
    {
        private const string EmpresaPadrao = "Focus Engenharia Elétrica";

        public CadastrosPage()
        {
            this.InitializeComponent();
            MostrarAba("Obras");
            CarregarTodos();
        }

        protected override void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is string aba)
                MostrarAba(aba);
            else
                MostrarAba("Obras");

            CarregarTodos();
        }

        private void MostrarAba(string aba)
        {
            PainelFuncionarios.Visibility = aba == "Funcionarios" ? Visibility.Visible : Visibility.Collapsed;
            PainelEquipamentos.Visibility = aba == "Equipamentos" ? Visibility.Visible : Visibility.Collapsed;
            PainelAcompanhantes.Visibility = aba == "Acompanhantes" ? Visibility.Visible : Visibility.Collapsed;
            PainelObras.Visibility = aba == "Obras" ? Visibility.Visible : Visibility.Collapsed;

            var cor = new SolidColorBrush(Color.FromArgb(255, 0, 200, 160));
            var transp = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var muted = new SolidColorBrush(Color.FromArgb(255, 138, 180, 212));

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
        }

        private void CarregarTodos()
        {
            FiltrarObras(BuscaObrasBox?.Text ?? "");
            FiltrarFuncionarios(BuscaFuncionariosBox?.Text ?? "");
            FiltrarEquipamentos(BuscaEquipamentosBox?.Text ?? "");
            FiltrarAcompanhantes(BuscaAcompanhantesBox?.Text ?? "");
        }

        // ── OBRAS ─────────────────────────────────────────────────────────────────
        private void FiltrarObras(string termo)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.Obras.Where(o => o.Ativo).ToList();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                var t = termo.ToLower();
                lista = lista.Where(o =>
                    o.Nome.ToLower().Contains(t) ||
                    o.Grupo.ToLower().Contains(t) ||
                    o.Contratante.ToLower().Contains(t) ||
                    o.Responsavel.ToLower().Contains(t)
                ).ToList();
            }
            ObrasListViewCadastro.ItemsSource = lista;
        }

        private void BuscaObras_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarObras(BuscaObrasBox.Text);
            LimparBuscaObrasBtn.Visibility =
                string.IsNullOrEmpty(BuscaObrasBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaObras_Click(object sender, RoutedEventArgs e)
        {
            BuscaObrasBox.Text = "";
            LimparBuscaObrasBtn.Visibility = Visibility.Collapsed;
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
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var item = await db.Obras.FindAsync(o.Id);
                if (item != null) { item.Ativo = false; await db.SaveChangesAsync(); }
                FiltrarObras(BuscaObrasBox?.Text ?? "");
            }
        }

        // ── FUNCIONÁRIOS ──────────────────────────────────────────────────────
        private void FiltrarFuncionarios(string termo)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.Funcionarios.Where(f => f.Ativo).ToList();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                var t = termo.ToLower();
                lista = lista.Where(f =>
                    f.Nome.ToLower().Contains(t) ||
                    f.Funcao.ToLower().Contains(t) ||
                    f.Empresa.ToLower().Contains(t) ||
                    f.Contato.ToLower().Contains(t)
                ).ToList();
            }
            FuncionariosListView.ItemsSource = lista;
        }

        private void BuscaFuncionarios_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarFuncionarios(BuscaFuncionariosBox.Text);
            LimparBuscaFuncionariosBtn.Visibility =
                string.IsNullOrEmpty(BuscaFuncionariosBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaFuncionarios_Click(object sender, RoutedEventArgs e)
        {
            BuscaFuncionariosBox.Text = "";
            LimparBuscaFuncionariosBtn.Visibility = Visibility.Collapsed;
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
                CarregarTodos();
            }
        }

        private async void ExcluirFuncionario_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Funcionario f) return;
            if (await ConfirmarExclusao(f.Nome))
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var item = await db.Funcionarios.FindAsync(f.Id);
                if (item != null) { item.Ativo = false; await db.SaveChangesAsync(); }
                CarregarTodos();
            }
        }

        // ── EQUIPAMENTOS ─────────────────────────────────────────────────────
        private void FiltrarEquipamentos(string termo)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.EquipamentosCadastrados.Where(e => e.Ativo).ToList();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                var t = termo.ToLower();
                lista = lista.Where(e =>
                    e.Nome.ToLower().Contains(t) ||
                    e.Fabricante.ToLower().Contains(t) ||
                    e.NumeroSerie.ToLower().Contains(t) ||
                    e.Modelo.ToLower().Contains(t)
                ).ToList();
            }
            EquipamentosListView.ItemsSource = lista;
        }

        private void BuscaEquipamentos_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarEquipamentos(BuscaEquipamentosBox.Text);
            LimparBuscaEquipamentosBtn.Visibility =
                string.IsNullOrEmpty(BuscaEquipamentosBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaEquipamentos_Click(object sender, RoutedEventArgs e)
        {
            BuscaEquipamentosBox.Text = "";
            LimparBuscaEquipamentosBtn.Visibility = Visibility.Collapsed;
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
                CarregarTodos();
            }
        }

        private async void ExcluirEquipamento_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not EquipamentoCadastrado eq) return;
            if (await ConfirmarExclusao(eq.Nome))
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var item = await db.EquipamentosCadastrados.FindAsync(eq.Id);
                if (item != null) { item.Ativo = false; await db.SaveChangesAsync(); }
                CarregarTodos();
            }
        }

        // ── ACOMPANHANTES ────────────────────────────────────────────────────
        private void FiltrarAcompanhantes(string termo)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var lista = db.Acompanhantes.Where(a => a.Ativo).ToList();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                var t = termo.ToLower();
                lista = lista.Where(a =>
                    a.Nome.ToLower().Contains(t) ||
                    a.Cargo.ToLower().Contains(t) ||
                    a.Grupo.ToLower().Contains(t)
                ).ToList();
            }
            AcompanhantesListView.ItemsSource = lista;
        }

        private void BuscaAcompanhantes_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarAcompanhantes(BuscaAcompanhantesBox.Text);
            LimparBuscaAcompanhantesBtn.Visibility =
                string.IsNullOrEmpty(BuscaAcompanhantesBox.Text)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimparBuscaAcompanhantes_Click(object sender, RoutedEventArgs e)
        {
            BuscaAcompanhantesBox.Text = "";
            LimparBuscaAcompanhantesBtn.Visibility = Visibility.Collapsed;
        }

        private async void AdicionarAcompanhante_Click(object sender, RoutedEventArgs e)
            => await AbrirModalAcompanhante(null);

        private async void EditarAcompanhante_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Acompanhante a)
                await AbrirModalAcompanhante(a);
        }

        private async Task AbrirModalAcompanhante(Acompanhante? existente)
        {
            var nomeBox = new TextBox { PlaceholderText = "Nome completo" };
            var cargoBox = new TextBox { PlaceholderText = "Ex: Fiscal, Engenheiro..." };
            var grupoBox = new TextBox { PlaceholderText = "Ex: Ambev, Cargill..." };
            var contatoBox = new TextBox { PlaceholderText = "Telefone ou e-mail" };

            if (existente != null)
            {
                nomeBox.Text = existente.Nome;
                cargoBox.Text = existente.Cargo;
                grupoBox.Text = existente.Grupo;
                contatoBox.Text = existente.Contato;
            }

            var form = new StackPanel { Spacing = 16, Width = 480 };
            var linha1 = new Grid { ColumnSpacing = 16 };
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var campoNome = CriarCampo("NOME", nomeBox);
            var campoCargo = CriarCampo("CARGO", cargoBox);
            Grid.SetColumn(campoNome, 0);
            Grid.SetColumn(campoCargo, 1);
            linha1.Children.Add(campoNome);
            linha1.Children.Add(campoCargo);
            form.Children.Add(linha1);

            var linha2 = new Grid { ColumnSpacing = 16 };
            linha2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            linha2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var campoGrupo = CriarCampo("GRUPO / CLIENTE", grupoBox);
            var campoContato = CriarCampo("CONTATO", contatoBox);
            Grid.SetColumn(campoGrupo, 0);
            Grid.SetColumn(campoContato, 1);
            linha2.Children.Add(campoGrupo);
            linha2.Children.Add(campoContato);
            form.Children.Add(linha2);

            var dialog = new ContentDialog
            {
                Title = existente == null ? "Novo acompanhante técnico" : "Editar acompanhante técnico",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(nomeBox.Text))
                {
                    await MostrarErro("O nome é obrigatório.");
                    return;
                }
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                if (existente != null)
                {
                    var item = await db.Acompanhantes.FindAsync(existente.Id);
                    if (item != null)
                    {
                        item.Nome = nomeBox.Text.Trim();
                        item.Cargo = cargoBox.Text.Trim();
                        item.Grupo = grupoBox.Text.Trim();
                        item.Contato = contatoBox.Text.Trim();
                    }
                }
                else
                {
                    db.Acompanhantes.Add(new Acompanhante
                    {
                        Nome = nomeBox.Text.Trim(),
                        Cargo = cargoBox.Text.Trim(),
                        Grupo = grupoBox.Text.Trim(),
                        Contato = contatoBox.Text.Trim(),
                        Ativo = true
                    });
                }
                await db.SaveChangesAsync();
                CarregarTodos();
            }
        }

        private async void ExcluirAcompanhante_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Acompanhante a) return;
            if (await ConfirmarExclusao(a.Nome))
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var item = await db.Acompanhantes.FindAsync(a.Id);
                if (item != null) { item.Ativo = false; await db.SaveChangesAsync(); }
                CarregarTodos();
            }
        }

        // ── NAVEGAÇÃO ABAS ────────────────────────────────────────────────────
        private void BtnAbaObras_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Obras");
        private void BtnAbaFuncionarios_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Funcionarios");
        private void BtnAbaEquipamentos_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Equipamentos");
        private void BtnAbaAcompanhantes_Click(object sender, RoutedEventArgs e)
            => MostrarAba("Acompanhantes");

        // ── HELPERS ───────────────────────────────────────────────────────────
        private static StackPanel CriarCampo(string label, Control input)
        {
            var sp = new StackPanel { Spacing = 6 };
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = new Windows.UI.Text.FontWeight { Weight = 600 },
                Foreground = new SolidColorBrush(Color.FromArgb(255, 138, 180, 212)),
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

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}