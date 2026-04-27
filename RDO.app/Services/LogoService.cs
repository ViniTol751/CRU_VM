using RDO.Data.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace RDO.App.Services;

public static class LogoService
{
    // Cache da acessibilidade do NAS (evita checar por empresa, só checa 1x a cada 30s)
    private static bool? _nasReachable;
    private static DateTime _nasLastCheck = DateTime.MinValue;

    private static bool IsNasReachable(LogosConfig cfg)
    {
        if (!cfg.IsConfigured) return false;
        if (_nasReachable.HasValue && (DateTime.Now - _nasLastCheck).TotalSeconds < 30)
            return _nasReachable.Value;
        try
        {
            _nasReachable = Directory.Exists(cfg.NasPath);
        }
        catch
        {
            _nasReachable = false;
        }
        _nasLastCheck = DateTime.Now;
        return _nasReachable.Value;
    }
    // Extrai "ADM" de "ADM | Campo Grande (MS)"
    public static string GetBaseNome(string nomeCompleto)
    {
        var idx = nomeCompleto.IndexOf(" | ", StringComparison.Ordinal);
        return idx >= 0 ? nomeCompleto[..idx].Trim() : nomeCompleto.Trim();
    }

    // "Açucar Guarani" → "Acucar_Guarani"
    public static string SanitizeNome(string nome)
    {
        var baseNome = GetBaseNome(nome);
        var normalized = baseNome.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        var clean = sb.ToString().Normalize(NormalizationForm.FormC);
        clean = Regex.Replace(clean, @"[^a-zA-Z0-9]", "_");
        clean = Regex.Replace(clean, @"_+", "_").Trim('_');
        return clean;
    }

    // Caminho completo na pasta de rede: \\192.168.0.89\Levantamentos\Logos\ABB.png
    public static string? GetNasLogoPath(LogosConfig cfg, string nomeCompleto)
    {
        if (!cfg.IsConfigured) return null;
        return Path.Combine(cfg.NasPath, SanitizeNome(nomeCompleto) + ".png");
    }

    // Converte caminho UNC para URI file:// para o BitmapImage
    // \\servidor\pasta\file.png → file://servidor/pasta/file.png
    public static string UncToFileUri(string uncPath)
        => "file:" + uncPath.Replace('\\', '/');

    // Resolve o caminho final: logo local tem prioridade, depois NAS (só se arquivo existir)
    public static string? ResolveLogoUrl(LogosConfig cfg, string? localPath, string nomeCompleto)
    {
        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
            return localPath;

        if (!IsNasReachable(cfg)) return null;

        var nasPath = GetNasLogoPath(cfg, nomeCompleto);
        if (nasPath == null) return null;

        try { return File.Exists(nasPath) ? nasPath : null; }
        catch { return null; }
    }

    // Escaneia a pasta NAS uma única vez e retorna os nomes de arquivo sem extensão.
    // Use junto com ResolveLogoUrlFast para evitar um File.Exists por empresa.
    public static async Task<HashSet<string>> GetNasFilesAsync(LogosConfig cfg)
    {
        if (!IsNasReachable(cfg)) return new HashSet<string>();
        return await Task.Run(() =>
        {
            try
            {
                return new HashSet<string>(
                    Directory.GetFiles(cfg.NasPath, "*.png")
                             .Select(Path.GetFileNameWithoutExtension)
                             .Where(n => n != null)
                             .Select(n => n!),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch { return new HashSet<string>(); }
        });
    }

    // Versão rápida: usa o índice pré-carregado pelo GetNasFilesAsync — sem I/O por item.
    public static string? ResolveLogoUrlFast(LogosConfig cfg, string? localPath, string nomeCompleto,
                                             HashSet<string> nasFiles)
    {
        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
            return localPath;

        var key = SanitizeNome(nomeCompleto);
        return nasFiles.Contains(key) ? GetNasLogoPath(cfg, nomeCompleto) : null;
    }

    // Compõe a imagem sobre fundo branco e salva em cache JPEG (sem canal alpha).
    // Usa WinRT BitmapDecoder/Encoder — garantido em WinUI 3 MSIX.
    public static async Task<string?> FlattenToWhiteAsync(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RDOApp", "LogoCache");
            Directory.CreateDirectory(cacheDir);

            var cachedName = "v5_" + Path.GetFileNameWithoutExtension(path) + ".jpg";
            var cachedPath = Path.Combine(cacheDir, cachedName);

            if (File.Exists(cachedPath) &&
                File.GetLastWriteTime(cachedPath) >= File.GetLastWriteTime(path))
                return cachedPath;

            // Decodifica usando WinRT
            var sourceFile = await StorageFile.GetFileFromPathAsync(path);
            using var sourceStream = await sourceFile.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(sourceStream);

            uint w = decoder.OrientedPixelWidth;
            uint h = decoder.OrientedPixelHeight;

            // Lê pixels como BGRA8 com alpha direto (straight)
            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);
            var pixels = pixelData.DetachPixelData();

            // Se imagem não tem canal alpha real (ex: PNG com fundo xadrez baked-in),
            // tenta detectar e remover o fundo via flood fill antes de compor sobre branco
            bool hasRealAlpha = false;
            for (int i = 3; i < pixels.Length; i += 4)
                if (pixels[i] < 255) { hasRealAlpha = true; break; }
            if (!hasRealAlpha)
                RemoveBackgroundByFloodFill(pixels, (int)w, (int)h);

            // Compõe sobre branco: resultado = src * alpha + 255 * (1 - alpha)
            for (int i = 0; i < pixels.Length; i += 4)
            {
                float a = pixels[i + 3] / 255f;
                pixels[i]     = (byte)(pixels[i]     * a + 255 * (1 - a)); // B
                pixels[i + 1] = (byte)(pixels[i + 1] * a + 255 * (1 - a)); // G
                pixels[i + 2] = (byte)(pixels[i + 2] * a + 255 * (1 - a)); // R
                pixels[i + 3] = 255; // alpha totalmente opaco
            }

            // Salva como JPEG (sem canal alpha — impossível ter transparência)
            var cacheFolder = await StorageFolder.GetFolderFromPathAsync(cacheDir);
            var cacheFile = await cacheFolder.CreateFileAsync(cachedName, CreationCollisionOption.ReplaceExisting);
            using var outStream = await cacheFile.OpenAsync(FileAccessMode.ReadWrite);

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                w, h, decoder.DpiX, decoder.DpiY, pixels);
            await encoder.FlushAsync();

            System.Diagnostics.Debug.WriteLine($"[LOGO] FlattenToWhiteAsync OK: {cachedName}");
            return cachedPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LOGO] FlattenToWhiteAsync FALHOU ({path}): {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    // Flood fill a partir dos 4 cantos para remover fundo claro/neutro baked-in (ex: xadrez).
    // Só atua quando o fundo detectado nos cantos é claro e acinzentado (brilho > 150, canais próximos).
    private static void RemoveBackgroundByFloodFill(byte[] pixels, int width, int height)
    {
        // Amostra 5x5 em cada canto (apenas pixels opacos) para determinar cor de fundo
        float totR = 0, totG = 0, totB = 0; int totCount = 0;
        void SampleArea(int sx, int sy)
        {
            for (int dy = 0; dy < 5; dy++)
            for (int dx = 0; dx < 5; dx++)
            {
                int x = sx + dx, y = sy + dy;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                int i = (y * width + x) * 4;
                if (pixels[i + 3] < 200) continue;
                totB += pixels[i]; totG += pixels[i + 1]; totR += pixels[i + 2];
                totCount++;
            }
        }
        SampleArea(0, 0); SampleArea(width - 5, 0);
        SampleArea(0, height - 5); SampleArea(width - 5, height - 5);

        if (totCount == 0) return;
        float avgR = totR / totCount, avgG = totG / totCount, avgB = totB / totCount;
        float brightness = (avgR + avgG + avgB) / 3f;
        float maxCh = MathF.Max(MathF.Max(avgR, avgG), avgB);
        float minCh = MathF.Min(MathF.Min(avgR, avgG), avgB);

        // Só remove se o fundo for claro e neutro (acinzentado/branco)
        if (brightness < 150 || maxCh - minCh > 60) return;

        const float tolerance = 50f;
        var visited = new bool[width * height];
        var queue = new Queue<int>();

        void Seed(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height && !visited[y * width + x])
                queue.Enqueue(y * width + x);
        }
        Seed(0, 0); Seed(width - 1, 0); Seed(0, height - 1); Seed(width - 1, height - 1);

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (visited[idx]) continue;
            visited[idx] = true;

            int i = idx * 4;
            float r = pixels[i + 2], g = pixels[i + 1], b = pixels[i];
            if (MathF.Abs(r - avgR) > tolerance || MathF.Abs(g - avgG) > tolerance || MathF.Abs(b - avgB) > tolerance)
                continue;

            pixels[i + 3] = 0; // torna transparente → composição resultará em branco
            int x = idx % width, y = idx / width;
            Seed(x - 1, y); Seed(x + 1, y); Seed(x, y - 1); Seed(x, y + 1);
        }
    }

    // Copia o logo local para a pasta do NAS (usado na sincronização)
    public static async Task UploadPendingLogosAsync(LogosConfig cfg)
    {
        if (!cfg.IsConfigured) return;

        using var db = new RdoDbContext(DbContextHelper.GetOptions());
        var empresas = db.Empresas
            .Where(e => e.ImagemPath != null && e.ImagemPath != "" && !e.IsDeleted)
            .ToList();

        foreach (var empresa in empresas)
        {
            if (!string.IsNullOrEmpty(empresa.ImagemPath) && File.Exists(empresa.ImagemPath))
                await CopiarLogoParaNasAsync(cfg, empresa.ImagemPath, empresa.Nome);
        }
    }

    public static Task CopiarLogoParaNasAsync(LogosConfig cfg, string localPath, string nomeCompleto)
    {
        return Task.Run(() =>
        {
            if (!cfg.IsConfigured || !File.Exists(localPath)) return;
            var destino = GetNasLogoPath(cfg, nomeCompleto);
            if (destino == null) return;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                File.Copy(localPath, destino, overwrite: true);
                System.Diagnostics.Debug.WriteLine($"[LOGO] ✓ Copiado para NAS: {Path.GetFileName(destino)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGO] ✗ Falha ao copiar {Path.GetFileName(destino)}: {ex.Message}");
            }
        });
    }
}
