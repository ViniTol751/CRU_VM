// Resolve caminhos de assets para modo unpackaged.
// Em modo MSIX: ms-appx:///Assets/foo.png
// Em modo unpackaged: caminho absoluto relativo ao executável
using System;
using System.IO;
using System.Reflection;

namespace RDO.App.Services;

public static class AssetHelper
{
    /// <summary>Diretório base onde o executável está.</summary>
    public static string AppDir { get; } =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? AppContext.BaseDirectory;

    /// <summary>
    /// Retorna um Uri para um asset.
    /// Exemplo: AssetHelper.GetUri("Assets/SE_Dark.png")
    /// </summary>
    public static Uri GetUri(string relativePath)
    {
        var full = Path.Combine(AppDir, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return new Uri(full);
    }

    /// <summary>Caminho absoluto para um asset.</summary>
    public static string GetPath(string relativePath)
        => Path.Combine(AppDir, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
}
