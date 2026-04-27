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
using Windows.Security.Credentials;
using Windows.Storage;
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

            var lastLogin = ApplicationData.Current.LocalSettings.Values["LembrarLogin"] as string;
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
                new Uri(isDark ? "ms-appx:///Assets/SE_Dark.png" : "ms-appx:///Assets/SE_Light.png"));
        }

        private void EntrarBtn_Click(object sender, RoutedEventArgs e)
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

            if (LembrarCheck.IsChecked == true)
            {
                try
                {
                    var vault = new PasswordVault();
                    try { vault.Remove(vault.Retrieve(VaultResource, login)); } catch { }
                    vault.Add(new PasswordCredential(VaultResource, login, senha));
                }
                catch { }
                ApplicationData.Current.LocalSettings.Values["LembrarLogin"] = login;
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values.Remove("LembrarLogin");
            }

            ApplicationData.Current.LocalSettings.Values["NomeUsuario"] = usuario.Nome;
            ApplicationData.Current.LocalSettings.Values["UsuarioId"]   = usuario.Id;
            ApplicationData.Current.LocalSettings.Values.Remove("FuncionarioVinculadoId");

            Frame.Navigate(typeof(MainPage));
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
            var nomeBox = new TextBox
            {
                PlaceholderText = "Nome completo",
                Header = "NOME COMPLETO"
            };
            var loginBox = new TextBox
            {
                PlaceholderText = "Ex: vinicius.toledo",
                Header = "USUÁRIO / LOGIN"
            };
            var senhaBox = new PasswordBox
            {
                PlaceholderText = "Mínimo 6 caracteres",
                Header = "SENHA"
            };
            var confirmarBox = new PasswordBox
            {
                PlaceholderText = "Confirme a senha",
                Header = "CONFIRMAR SENHA"
            };
            var erroMsg = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 80, 80)),
                FontSize = 12,
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Visibility = Visibility.Collapsed
            };

            nomeBox.TextChanged += (s, ev) =>
                loginBox.Text = GerarLogin(nomeBox.Text);

            var form = new StackPanel { Spacing = 14, Width = 360 };
            form.Children.Add(nomeBox);
            form.Children.Add(loginBox);
            form.Children.Add(senhaBox);
            form.Children.Add(confirmarBox);
            form.Children.Add(erroMsg);

            var dialog = new ContentDialog
            {
                Title = "Registrar Conta",
                Content = form,
                PrimaryButtonText = "Criar conta",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            while (true)
            {
                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary) break;

                var nome     = nomeBox.Text.Trim();
                var loginVal = loginBox.Text.Trim();
                var senha    = senhaBox.Password;
                var confirma = confirmarBox.Password;

                if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(loginVal) || string.IsNullOrEmpty(senha))
                {
                    erroMsg.Text = "Preencha todos os campos.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }
                if (senha != confirma)
                {
                    erroMsg.Text = "As senhas não coincidem.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }
                if (senha.Length < 6)
                {
                    erroMsg.Text = "A senha deve ter ao menos 6 caracteres.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }

                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                if (db.Usuarios.Any(u => u.Email == loginVal))
                {
                    erroMsg.Text = $"O usuário \"{loginVal}\" já existe.";
                    erroMsg.Visibility = Visibility.Visible;
                    continue;
                }

                db.Usuarios.Add(new RDO.Data.Models.User
                {
                    Nome      = nome,
                    Email     = loginVal,
                    SenhaHash = PasswordHasher.Hash(senha),
                    Perfil    = "Technician",
                    Ativo     = true
                });
                db.SaveChanges();

                var ok = new ContentDialog
                {
                    Title = "Conta criada",
                    Content = $"Conta \"{loginVal}\" criada com sucesso. Faça login para entrar.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await ok.ShowAsync();

                EmailBox.Text = loginVal;
                break;
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
