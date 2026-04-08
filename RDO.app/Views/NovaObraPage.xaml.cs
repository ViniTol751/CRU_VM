using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.IO;
using Windows.Storage.Pickers;

namespace RDO.App.Views
{
    public sealed partial class NovaObraPage : Page
    {
        private string? _imagemPath;
        private Obra? _obraExistente;
        private string _abaOrigem = "Obras";

        public NovaObraPage()
        {
            this.InitializeComponent();
            DataInicioPicker.Date = DateTimeOffset.Now;
            DataTerminoPicker.Date = DateTimeOffset.Now.AddMonths(6);
            StatusBox.SelectedIndex = 0;
            TipoContratoBox.SelectedIndex = 0;
        }

        protected override void OnNavigatedTo(
            Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.Parameter is NovaObraParams p)
            {
                _abaOrigem = p.AbaOrigem;
                if (p.ObraId.HasValue)
                    CarregarObra(p.ObraId.Value);
            }
            else if (e.Parameter is int obraId)
            {
                CarregarObra(obraId);
            }
        }

        private void CarregarObra(int obraId)
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            var obra = db.Obras.Find(obraId);
            if (obra == null) return;

            _obraExistente = obra;

            TituloTexto.Text = "Editar Obra";
            SalvarBtn.Content = "Salvar Alterações";

            NomeBox.Text = obra.Nome;
            GrupoBox.Text = obra.Grupo;
            ContratanteBox.Text = obra.Contratante;
            EnderecoBox.Text = obra.Endereco;
            ArtBox.Text = obra.ART;
            DataInicioPicker.Date = obra.DataInicio;
            DataTerminoPicker.Date = obra.PrevisaoTermino ?? DateTimeOffset.Now.AddMonths(6);

            for (int i = 0; i < StatusBox.Items.Count; i++)
            {
                if ((StatusBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.Status)
                { StatusBox.SelectedIndex = i; break; }
            }

            for (int i = 0; i < TipoContratoBox.Items.Count; i++)
            {
                if ((TipoContratoBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.TipoContrato)
                { TipoContratoBox.SelectedIndex = i; break; }
            }

            for (int i = 0; i < ResponsavelBox.Items.Count; i++)
            {
                if ((ResponsavelBox.Items[i] as ComboBoxItem)?.Content.ToString() == obra.Responsavel)
                { ResponsavelBox.SelectedIndex = i; break; }
            }

            if (!string.IsNullOrEmpty(obra.ImagemPath) && File.Exists(obra.ImagemPath))
            {
                _imagemPath = obra.ImagemPath;
                ImagemNomeTexto.Text = Path.GetFileName(obra.ImagemPath);
                ImagemPlaceholder.Visibility = Visibility.Collapsed;
                PreviewImagem.Visibility = Visibility.Visible;
                PreviewImagem.Source = new BitmapImage(new Uri(obra.ImagemPath));
                BtnRemoverImagem.Visibility = Visibility.Visible; // ← mostra botão remover
            }
        }

        private async void ImagemBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Evita abrir o picker ao clicar no botão remover
            if (BtnRemoverImagem.Visibility == Visibility.Visible &&
                PreviewImagem.Visibility == Visibility.Visible)
                return;

            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".png");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _imagemPath = file.Path;
                ImagemNomeTexto.Text = file.Name;
                ImagemPlaceholder.Visibility = Visibility.Collapsed;
                PreviewImagem.Visibility = Visibility.Visible;
                BtnRemoverImagem.Visibility = Visibility.Visible; // ← mostra botão remover
                var bitmap = new BitmapImage();
                using var stream = await file.OpenReadAsync();
                await bitmap.SetSourceAsync(stream);
                PreviewImagem.Source = bitmap;
            }
        }

        private void RemoverImagem_Click(object sender, RoutedEventArgs e)
        {
            _imagemPath = null;
            PreviewImagem.Visibility = Visibility.Collapsed;
            BtnRemoverImagem.Visibility = Visibility.Collapsed;
            ImagemPlaceholder.Visibility = Visibility.Visible;
            ImagemNomeTexto.Text = "";
        }

        private async void CriarBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NomeBox.Text))
            {
                await MostrarErro("O nome da obra é obrigatório.");
                return;
            }

            try
            {
                // Se removeu a imagem, destino é null
                string? imagemDestino = _imagemPath == null ? null : _obraExistente?.ImagemPath;

                if (_imagemPath != null && _imagemPath != _obraExistente?.ImagemPath)
                {
                    var pastaImagens = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RDOApp", "Imagens");
                    Directory.CreateDirectory(pastaImagens);
                    var destino = Path.Combine(pastaImagens, Path.GetFileName(_imagemPath));
                    File.Copy(_imagemPath, destino, overwrite: true);
                    imagemDestino = destino;
                }

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
                        item.TipoContrato = (TipoContratoBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
                        item.Contratante = ContratanteBox.Text.Trim();
                        item.Endereco = EnderecoBox.Text.Trim();
                        item.ART = ArtBox.Text.Trim();
                        item.DataInicio = DataInicioPicker.Date.DateTime;
                        item.PrevisaoTermino = DataTerminoPicker.Date.DateTime;
                        item.ImagemPath = imagemDestino;
                    }
                    await db.SaveChangesAsync();

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
                        TipoContrato = (TipoContratoBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "",
                        Contratante = ContratanteBox.Text.Trim(),
                        Endereco = EnderecoBox.Text.Trim(),
                        ART = ArtBox.Text.Trim(),
                        DataInicio = DataInicioPicker.Date.DateTime,
                        PrevisaoTermino = DataTerminoPicker.Date.DateTime,
                        ImagemPath = imagemDestino,
                        Ativo = true
                    };
                    db.Obras.Add(obra);
                    await db.SaveChangesAsync();

                    var dialog = new ContentDialog
                    {
                        Title = "Obra criada!",
                        Content = $"A obra \"{obra.Nome}\" foi cadastrada com sucesso.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

                Frame.Navigate(typeof(CadastrosPage), _abaOrigem);
            }
            catch (Exception ex)
            {
                await MostrarErro($"Erro ao salvar: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task MostrarErro(string mensagem)
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