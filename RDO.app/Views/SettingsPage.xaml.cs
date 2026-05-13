using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RDO.App.Services;
using RDO.app.Services;
using RDO.Data.Data;

namespace RDO.App.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            CarregarInfos();
            AtualizarTemaTexto();
        }

        private void CarregarInfos()
        {
            var appCfg = AppConfig.Load();
            var apiUrl = appCfg.ApiUrl ?? "—";
            ApiUrlTexto.Text = apiUrl;

            // BD central: deriva o host da URL da API e aponta para a porta padrão do PostgreSQL
            try
            {
                var uri = new Uri(apiUrl);
                BdCentralTexto.Text = $"{uri.Host}:5432 — PostgreSQL (QNAP / Container Station)";
            }
            catch
            {
                BdCentralTexto.Text = "—";
            }

            var logosCfg = LogosConfig.Load();
            NasPathTexto.Text = string.IsNullOrWhiteSpace(logosCfg.NasPath) ? "Não configurado" : logosCfg.NasPath;

            DbPathTexto.Text = DbContextHelper.GetDbPath();

            DadosPathTexto.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RDOApp");
        }

        private void AtualizarTemaTexto()
        {
            TemaAtualTexto.Text = ThemeManager.Current == ElementTheme.Dark ? "Escuro" : "Claro";
        }

        private void BtnAlternarTema_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Toggle();
            AtualizarTemaTexto();
        }

        private async void BtnResetBanco_Click(object sender, RoutedEventArgs e)
        {
            var confirm = new ContentDialog
            {
                Title = "Limpar banco local?",
                Content = "Todos os dados locais serão removidos.\n\nOs dados serão baixados novamente do servidor na próxima sincronização. Rascunhos não sincronizados serão perdidos.",
                PrimaryButtonText = "Limpar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
                return;

            BtnResetBanco.IsEnabled = false;
            BtnResetBanco.Content = "Limpando…";

            try
            {
                await SyncService.ResetLocalDataAsync();

                var ok = new ContentDialog
                {
                    Title = "Banco limpo",
                    Content = "Dados locais removidos com sucesso. Ao voltar para a tela principal, a sincronização automática restaurará os dados do servidor.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await ok.ShowAsync();
                Frame.GoBack();
            }
            catch (Exception ex)
            {
                BtnResetBanco.IsEnabled = true;
                BtnResetBanco.Content = "Limpar";
                var erro = new ContentDialog
                {
                    Title = "Erro ao limpar",
                    Content = $"Não foi possível limpar o banco local:\n{ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await erro.ShowAsync();
            }
        }

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}
