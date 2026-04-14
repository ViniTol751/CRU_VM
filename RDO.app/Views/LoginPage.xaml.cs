using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace RDO.App.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void EntrarBtn_Click(object sender, RoutedEventArgs e)

        {
            var email = EmailBox.Text.Trim();
            var senha = SenhaBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                ErroTexto.Text = "Preencha e-mail e senha.";
                ErroTexto.Visibility = Visibility.Visible;
                return;
            }

            // Por enquanto aceita qualquer login — autenticação real vem depois
            ErroTexto.Visibility = Visibility.Collapsed;
            Frame.Navigate(typeof(MainPage));
        }

        private async void EsqueceuSenhaBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Recuperação de senha",
                Content = "Entre em contato com o administrador do sistema para redefinir sua senha.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}