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

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}
