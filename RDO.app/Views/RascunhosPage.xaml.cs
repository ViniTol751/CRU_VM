using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RDO.Data.Data;
using System;
using System.Linq;

namespace RDO.App.Views
{
    public class RascunhoViewModel
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string ObraNome { get; set; } = "";
        public string DataFormatada { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public sealed partial class RascunhosPage : Page
    {
        public RascunhosPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => CarregarRascunhos();
        }

        private void CarregarRascunhos()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            var lista = db.Relatorios
                .Where(r => r.Rascunho)
                .ToList()
                .Select(r => new RascunhoViewModel
                {
                    Id = r.Id,
                    ObraId = r.ObraId,
                    ObraNome = db.Obras.Find(r.ObraId)?.Nome ?? "—",
                    DataFormatada = $"Salvo em {r.CriadoEm:dd/MM/yyyy HH:mm}",
                    Status = $"Status: {r.Status}"
                })
                .ToList();

            RascunhosListView.ItemsSource = lista;
        }

        private void ContinuarRascunho_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int obraId)
                Frame.Navigate(typeof(RdoFormPage), obraId);
        }

        private async void ExcluirRascunho_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not int id) return;

            var dialog = new ContentDialog
            {
                Title = "Excluir rascunho",
                Content = "Deseja excluir este rascunho permanentemente?",
                PrimaryButtonText = "Excluir",
                CloseButtonText = "Cancelar",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                using var db = new RdoDbContext(DbContextHelper.GetOptions());
                var r = await db.Relatorios.FindAsync(id);
                if (r != null) { db.Relatorios.Remove(r); await db.SaveChangesAsync(); }
                CarregarRascunhos();
            }
        }

        private void VoltarBtn_Click(object sender, RoutedEventArgs e)
            => Frame.GoBack();
    }
}