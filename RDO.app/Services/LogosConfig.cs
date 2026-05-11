using System;
using System.IO;
using System.Text.Json;

namespace RDO.App.Services;

public class LogosConfig
{
    public string NasPath { get; set; } = @"\\192.168.0.89\Levantamentos\Logos";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(NasPath);

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "logos_config.json");

    public static LogosConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<LogosConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new LogosConfig();
            }
        }
        catch { }
        return new LogosConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
