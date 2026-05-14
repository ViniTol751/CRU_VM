using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using RDO.App.Services;
using RDO.Data.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.System;
using Windows.UI;

namespace RDO.App.Views
{
    public sealed partial class LoginPage : Page
    {
        private const string VaultResource = "RDOApp";
        private List<string> _savedLogins = new();
        private DispatcherTimer? _hideSuggestionsTimer;

        public LoginPage()
        {
            this.InitializeComponent();
            AtualizarImagemTema();
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSavedLogins();

            var lastLogin = LocalSettingsService.Get<string>("LembrarLogin");
            if (!string.IsNullOrEmpty(lastLogin))
            {
                EmailBox.Text = lastLogin;
                LembrarCheck.IsChecked = true;
                try
                {
                    var vault = new PasswordVault();
                    var cred = vault.Retrieve(VaultResource, lastLogin);
                    cred.RetrievePassword();
                    SenhaBox.Password = cred.Password;
                }
                catch { }
            }
        }

        private void LoadSavedLogins()
        {
            try
            {
                var vault = new PasswordVault();
                _savedLogins = vault.FindAllByResource(VaultResource)
                    .Select(c => c.UserName)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }
            catch { _savedLogins = new List<string>(); }
        }

        private void EmailBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _hideSuggestionsTimer?.Stop();
            UpdateSuggestions();
        }

        private void EmailBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _hideSuggestionsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _hideSuggestionsTimer.Tick += (s, _) =>
            {
                _hideSuggestionsTimer!.Stop();
                SuggestionsContainer.Visibility = Visibility.Collapsed;
            };
            _hideSuggestionsTimer.Start();
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
            => UpdateSuggestions();

        private void UpdateSuggestions()
        {
            if (_savedLogins.Count == 0) return;

            var query = EmailBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(query)
                ? _savedLogins
                : _savedLogins.Where(l => l.ToLower().Contains(query)).ToList();

            if (filtered.Count > 0)
            {
                SuggestionsList.ItemsSource = filtered;
                SuggestionsContainer.Visibility = Visibility.Visible;
            }
            else
            {
                SuggestionsContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void SuggestionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            _hideSuggestionsTimer?.Stop();
            if (e.ClickedItem is string login)
                FillCredential(login);
        }

        private void FillCredential(string login)
        {
            EmailBox.Text = login;
            try
            {
                var vault = new PasswordVault();
                var cred = vault.Retrieve(VaultResource, login);
                cred.RetrievePassword();
                SenhaBox.Password = cred.Password;
            }
            catch { }
            SuggestionsContainer.Visibility = Visibility.Collapsed;
        }

        private void LoginField_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SuggestionsContainer.Visibility = Visibility.Collapsed;
                if (!string.IsNullOrEmpty(EmailBox.Text) && !string.IsNullOrEmpty(SenhaBox.Password))
                    EntrarBtn_Click(sender, e);
            }
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            AtualizarImagemTema();
        }

        private void AtualizarImagemTema()
        {
            var isDark = ThemeManager.Current == ElementTheme.Dark;
            BtnTema.Content = isDark ? "" : "";
            LoginSideImage.Source = new BitmapImage(
                AssetHelper.GetUri(isDark ? "Assets/SE_Dark.png" : "Assets/SE_Light.png"));
        }

        private async void EntrarBtn_Click(object sender, RoutedEventArgs e)
        {
            var login = EmailBox.Text.Trim();
            var senha = SenhaBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(senha))
            {
                MostrarErro("Preencha o usuário e a senha.");
                return;
            }

            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == login && u.Ativo);

            if (usuario == null || !PasswordHasher.Verify(senha, usuario.SenhaHash))
            {
                MostrarErro("Usuário ou senha incorretos.");
                return;
            }

            ErroTexto.Visibility = Visibility.Collapsed;

            // Guarda credenciais em memória para o SyncService usar JWT na API
            UserSession.Set(login, senha, usuario.Nome);

            // Garante que o usuário existe no servidor (recupera contas criadas offline)
            await EnsureServerUserAsync(login, senha, usuario.Nome);

            if (LembrarCheck.IsChecked == true)
            {
                try
                {
                    var vault = new PasswordVault();
                    try { vault.Remove(vault.Retrieve(VaultResource, login)); } catch { }
                    vault.Add(new PasswordCredential(VaultResource, login, senha));
                }
                catch { }
                LocalSettingsService.Set("LembrarLogin", login);
            }
            else
            {
                LocalSettingsService.Remove("LembrarLogin");
            }

            LocalSettingsService.Set("NomeUsuario", usuario.Nome);
            LocalSettingsService.Set("UsuarioId", usuario.Id);
            LocalSettingsService.Remove("FuncionarioVinculadoId");

            AppLogger.LogInfo("AUTH", $"Login: {usuario.Email}  (perfil={usuario.Perfil})");
            Frame.Navigate(typeof(MainPage));
        }

        private static async Task EnsureServerUserAsync(string email, string password, string nome)
        {
            try
            {
                var apiUrl = AppConfig.Load().ApiUrl.TrimEnd('/');
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                var resp = await http.PostAsJsonAsync($"{apiUrl}/api/auth/login",
                    new { Email = email, Password = password });
                if (resp.IsSuccessStatusCode) return;
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    await http.PostAsJsonAsync($"{apiUrl}/api/auth/register",
                        new { Name = nome, Email = email, Password = password });
            }
            catch { }
        }

        private async void EsqueceuSenhaBtn_Click(object sender, RoutedEventArgs e)
        {
            var loginBox = new TextBox
            {
                PlaceholderText = "Seu usuário (ex: vinicius.toledo)",
                Header = "USUÁRIO"
            };
            var novaSenhaBox = new PasswordBox
            {
                PlaceholderText = "Nova senha",
                Header = "NOVA SENHA"
            };
            var confirmarBox = new PasswordBox
            {
                PlaceholderText = "Confirme a nova senha",
                Header = "CONFIRMAR SENHA"
            };
            var erroMsg = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)),
                FontSize = 12,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            var form = new StackPanel { Spacing = 14, Width = 360 };
            form.Children.Add(loginBox);
            form.Children.Add(novaSenhaBox);
            form.Children.Add(confirmarBox);
            form.Children.Add(erroMsg);

            var dialog = new ContentDialog
            {
                Title = "Redefinir Senha",
                Content = form,
                PrimaryButtonText = "Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            while (true)
            {
                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary) break;

                var loginVal = loginBox.Text.Trim();
                var nova     = novaSenhaBox.Password;
                var confirma = confirmarBox.Password;

                if (string.IsNullOrEmpty(loginVal) || string.IsNullOrEmpty(nova))
                {
                    erroMsg.Text = "Preencha todos os campos.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }
                if (nova != confirma)
                {
                    erroMsg.Text = "As senhas não coincidem.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }
                if (nova.Length < 6)
                {
                    erroMsg.Text = "A senha deve ter ao menos 6 caracteres.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }

                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == loginVal && u.Ativo);
                if (usuario == null)
                {
                    erroMsg.Text = "Usuário não encontrado.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }

                usuario.SenhaHash = PasswordHasher.Hash(nova);
                db.SaveChanges();

                var ok = new ContentDialog
                {
                    Title = "Senha atualizada",
                    Content = "Sua senha foi redefinida com sucesso.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await ok.ShowAsync();
                break;
            }
        }

        private async void RegistrarBtn_Click(object sender, RoutedEventArgs e)
        {
            var passo = 1;
            var criouConta = false;
            string? regNome = null, regEmail = null, regSenha = null;

            // ── Passo 1: Identificação ─────────────────────────────────────────
            var nomeBox = new TextBox { PlaceholderText = "Nome completo", Header = "NOME COMPLETO" };
            var loginBox = new TextBox { PlaceholderText = "Ex: vinicius.toledo", Header = "USUÁRIO / LOGIN" };
            nomeBox.TextChanged += (s, ev) => loginBox.Text = GerarLogin(nomeBox.Text);

            var panel1 = new StackPanel { Spacing = 14 };
            panel1.Children.Add(nomeBox);
            panel1.Children.Add(loginBox);

            // ── Passo 2: Dados profissionais ───────────────────────────────────
            var funcaoBox = new TextBox { PlaceholderText = "Ex: Eletricista, Engenheiro...", Header = "FUNÇÃO" };
            var emailContatoBox = new TextBox { PlaceholderText = "Ex: nome@focuseng.com.br", Header = "E-MAIL DE CONTATO (opcional)" };

            var panel2 = new StackPanel { Spacing = 14, Visibility = Visibility.Collapsed };
            panel2.Children.Add(funcaoBox);
            panel2.Children.Add(emailContatoBox);

            // ── Passo 3: Segurança ─────────────────────────────────────────────
            var senhaBox = new PasswordBox { PlaceholderText = "Mínimo 6 caracteres", Header = "SENHA" };
            var confirmarBox = new PasswordBox { PlaceholderText = "Confirme a senha", Header = "CONFIRMAR SENHA" };

            var panel3 = new StackPanel { Spacing = 14, Visibility = Visibility.Collapsed };
            panel3.Children.Add(senhaBox);
            panel3.Children.Add(confirmarBox);

            // ── Indicador de passos ────────────────────────────────────────────
            Border MakeDot() => new Border
            {
                Width = 9, Height = 9,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(255, 160, 165, 190))
            };
            var dot1 = MakeDot(); var dot2 = MakeDot(); var dot3 = MakeDot();
            var passoTexto = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 125, 150)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7, VerticalAlignment = VerticalAlignment.Center };
            dotsPanel.Children.Add(dot1); dotsPanel.Children.Add(dot2); dotsPanel.Children.Add(dot3);
            var indicatorGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            indicatorGrid.Children.Add(dotsPanel);
            indicatorGrid.Children.Add(passoTexto);

            var accent = Color.FromArgb(255, 37, 99, 235);
            var muted  = Color.FromArgb(255, 160, 165, 190);
            void AtualizarIndicador(int p)
            {
                dot1.Background = new SolidColorBrush(p >= 1 ? accent : muted);
                dot2.Background = new SolidColorBrush(p >= 2 ? accent : muted);
                dot3.Background = new SolidColorBrush(p >= 3 ? accent : muted);
                passoTexto.Text = $"Passo {p} de 3";
            }
            AtualizarIndicador(1);

            // ── Mensagem de erro ───────────────────────────────────────────────
            var erroMsg = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)),
                FontSize = 12,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            var form = new StackPanel { Spacing = 14, Width = 360 };
            form.Children.Add(indicatorGrid);
            form.Children.Add(panel1);
            form.Children.Add(panel2);
            form.Children.Add(panel3);
            form.Children.Add(erroMsg);

            var dialog = new ContentDialog
            {
                Title = "Registrar Conta",
                Content = form,
                PrimaryButtonText = "Próximo",
                SecondaryButtonText = "Voltar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            dialog.IsSecondaryButtonEnabled = false;

            dialog.PrimaryButtonClick += (s, args) =>
            {
                args.Cancel = true;
                erroMsg.Visibility = Visibility.Collapsed;

                if (passo == 1)
                {
                    var nome  = nomeBox.Text.Trim();
                    var login = loginBox.Text.Trim();
                    if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(login))
                    {
                        erroMsg.Text = "Preencha o nome e o usuário.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    using var db = new RdoDbContext(DbContextHelper.GetOptions());
                    if (db.Usuarios.Any(u => u.Email == login))
                    {
                        erroMsg.Text = $"O usuário \"{login}\" já existe.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    passo = 2;
                    panel1.Visibility = Visibility.Collapsed;
                    panel2.Visibility = Visibility.Visible;
                    dialog.IsSecondaryButtonEnabled = true;
                    AtualizarIndicador(2);
                }
                else if (passo == 2)
                {
                    if (string.IsNullOrEmpty(funcaoBox.Text.Trim()))
                    {
                        erroMsg.Text = "Informe a função.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    passo = 3;
                    panel2.Visibility = Visibility.Collapsed;
                    panel3.Visibility = Visibility.Visible;
                    dialog.PrimaryButtonText = "Criar conta";
                    AtualizarIndicador(3);
                }
                else
                {
                    var senha    = senhaBox.Password;
                    var confirma = confirmarBox.Password;
                    if (string.IsNullOrEmpty(senha))
                    {
                        erroMsg.Text = "Informe a senha.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    if (senha != confirma)
                    {
                        erroMsg.Text = "As senhas não coincidem.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    if (senha.Length < 6)
                    {
                        erroMsg.Text = "A senha deve ter ao menos 6 caracteres.";
                        erroMsg.Visibility = Visibility.Visible;
                        return;
                    }
                    try
                    {
                        using var db = new RdoDbContext(DbContextHelper.GetOptions());
                        var loginVal = loginBox.Text.Trim();
                        var nome     = nomeBox.Text.Trim();
                        var funcao   = funcaoBox.Text.Trim();
                        var contato  = emailContatoBox.Text.Trim();

                        db.Usuarios.Add(new RDO.Data.Models.User
                        {
                            Nome      = nome,
                            Email     = loginVal,
                            SenhaHash = PasswordHasher.Hash(senha),
                            Perfil    = "Technician",
                            Ativo     = true
                        });
                        db.Funcionarios.Add(new Funcionario
                        {
                            Nome    = nome,
                            Funcao  = funcao,
                            Tipo    = "Próprio",
                            Empresa = "Focus Engenharia Elétrica",
                            Contato = contato,
                            Ativo   = true
                        });
                        db.SaveChanges();
                        criouConta = true;
                        regNome = nome; regEmail = loginVal; regSenha = senha;
                        args.Cancel = false;
                    }
                    catch (Exception)
                    {
                        erroMsg.Text = "Não foi possível criar a conta. Tente novamente.";
                        erroMsg.Visibility = Visibility.Visible;
                    }
                }
            };

            dialog.SecondaryButtonClick += (s, args) =>
            {
                args.Cancel = true;
                erroMsg.Visibility = Visibility.Collapsed;
                if (passo == 2)
                {
                    passo = 1;
                    panel2.Visibility = Visibility.Collapsed;
                    panel1.Visibility = Visibility.Visible;
                    dialog.PrimaryButtonText = "Próximo";
                    dialog.IsSecondaryButtonEnabled = false;
                    AtualizarIndicador(1);
                }
                else if (passo == 3)
                {
                    passo = 2;
                    panel3.Visibility = Visibility.Collapsed;
                    panel2.Visibility = Visibility.Visible;
                    dialog.PrimaryButtonText = "Próximo";
                    AtualizarIndicador(2);
                }
            };

            await dialog.ShowAsync();

            if (criouConta)
            {
                // Registra no servidor (best-effort — se offline, o sync posterior enviará)
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var apiUrl = AppConfig.Load().ApiUrl.TrimEnd('/');
                    await http.PostAsJsonAsync($"{apiUrl}/api/auth/register",
                        new { Name = regNome, Email = regEmail, Password = regSenha });
                }
                catch { }

                var loginVal = loginBox.Text.Trim();
                var ok = new ContentDialog
                {
                    Title = "Conta criada",
                    Content = new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock { Text = $"Conta \"{loginVal}\" criada com sucesso.", TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap },
                            new TextBlock
                            {
                                Text = "Funcionário registrado automaticamente nos cadastros como colaborador da Focus Engenharia Elétrica.",
                                FontSize = 12,
                                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                                Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 180, 100))
                            }
                        }
                    },
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await ok.ShowAsync();
                EmailBox.Text = loginVal;
            }
        }

        private void MostrarErro(string msg)
        {
            ErroTexto.Text = msg;
            ErroTexto.Visibility = Visibility.Visible;
        }

        private static string GerarLogin(string nomeCompleto)
        {
            var prep = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "de", "da", "do", "dos", "das", "e", "di", "del", "van", "von" };
            var partes = nomeCompleto.Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !prep.Contains(p))
                .ToArray();
            if (partes.Length >= 2)
                return $"{partes[0].ToLower()}.{partes[1].ToLower()}";
            return partes.FirstOrDefault()?.ToLower() ?? "";
        }
    }
}
