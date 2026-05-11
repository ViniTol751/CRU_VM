// Substitui ApplicationData.Current.LocalSettings para modo unpackaged.
// Persiste chave/valor em %LocalAppData%\RDOApp\settings.json
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RDO.App.Services;

public static class LocalSettingsService
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "settings.json");

    private static Dictionary<string, object?> _cache = Load();

    private static Dictionary<string, object?> Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                       ?? new();
            }
        }
        catch { }
        return new();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_cache,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static object? Get(string key)
        => _cache.TryGetValue(key, out var v) ? v : null;

    public static T? Get<T>(string key)
    {
        if (!_cache.TryGetValue(key, out var v) || v is null) return default;
        try
        {
            if (v is JsonElement je)
            {
                if (typeof(T) == typeof(string))  return (T)(object)je.GetString()!;
                if (typeof(T) == typeof(int))     return (T)(object)je.GetInt32();
                if (typeof(T) == typeof(int?))    return (T)(object)(int?)je.GetInt32();
            }
            return (T)Convert.ChangeType(v, typeof(T));
        }
        catch { return default; }
    }

    public static bool ContainsKey(string key) => _cache.ContainsKey(key);

    public static void Set(string key, object? value)
    {
        _cache[key] = value;
        Save();
    }

    public static void Remove(string key)
    {
        if (_cache.Remove(key)) Save();
    }
}
