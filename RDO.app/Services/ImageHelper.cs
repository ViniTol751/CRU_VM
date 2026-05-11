using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace RDO.App.Services
{
    /// <summary>
    /// Carrega imagens de disco via MemoryStream para evitar lock no arquivo,
    /// permitindo File.Copy enquanto a imagem está sendo exibida na UI.
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Carrega um arquivo PNG/JPG do disco para um BitmapImage sem manter
        /// o arquivo bloqueado. Retorna null se o arquivo não existir ou falhar.
        /// </summary>
        public static async Task<BitmapImage?> CarregarAsync(string? caminhoArquivo)
        {
            if (string.IsNullOrEmpty(caminhoArquivo) || !File.Exists(caminhoArquivo))
                return null;

            try
            {
                // Lê todo o conteúdo para memória e fecha o handle do arquivo imediatamente
                var bytes = await File.ReadAllBytesAsync(caminhoArquivo);

                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                using var ras = ms.AsRandomAccessStream();
                await bitmap.SetSourceAsync(ras);
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
