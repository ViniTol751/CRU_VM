using System;
using System.IO;
using System.Text.Json;

namespace RDO.app.Services;

public class SupabaseConfig
{
    public string ProjectUrl { get; set; } = "";   // ex: https://abcdefgh.supabase.co
    public string ServiceKey { get; set; } = "";   // service_role key ou anon key com bucket público
    public string Bucket     { get; set; } = "rdo-anexos";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ProjectUrl) &&
        !string.IsNullOrWhiteSpace(ServiceKey);

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "supabase.json");

    public static SupabaseConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<SupabaseConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new SupabaseConfig();
            }
        }
        catch { }
        return new SupabaseConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
