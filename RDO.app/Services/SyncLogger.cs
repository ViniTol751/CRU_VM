using System;
using System.IO;
using System.Net;
using System.Text;

namespace RDO.app.Services;

/// <summary>
/// Grava logs de sincronização em arquivos diários em %LocalAppData%\RDOApp\Logs\.
/// </summary>
public static class SyncLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "Logs");

    // ─────────────────────────────────────────────────────────────────
    // Erro de sincronização — log detalhado
    // ─────────────────────────────────────────────────────────────────
    public static void LogError(SyncLogEntry entry)
    {
        try
        {
            EnsureDirectory();
            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  [ERRO] {entry.Timestamp:yyyy-MM-dd HH:mm:ss} (UTC{entry.Timestamp:zzz})");
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Operação       : {entry.Operation}");
            sb.AppendLine($"  Código de Erro : {entry.ErrorCode}");
            sb.AppendLine($"  Cód. Padronizado: {RDO.App.Services.AppErrorCodes.MapToStandardCode(entry.ErrorCode)}");
            sb.AppendLine($"  Tipo           : {entry.ErrorType}");
            sb.AppendLine($"  Status HTTP    : {(entry.HttpStatusCode.HasValue ? $"{(int)entry.HttpStatusCode} {entry.HttpStatusCode}" : "N/A")}");
            sb.AppendLine($"  URL da API     : {entry.ApiUrl}");
            sb.AppendLine($"  Duração        : {entry.DurationMs:F0} ms");
            sb.AppendLine();
            sb.AppendLine($"  Mensagem       : {entry.UserMessage}");
            sb.AppendLine($"  Detalhe Técn.  : {entry.TechnicalDetail}");
            sb.AppendLine();
            sb.AppendLine("  ── Diagnóstico / Troubleshooting ──────────────────────────");
            foreach (var line in entry.Diagnosis.Split('\n'))
                sb.AppendLine($"  {line.TrimEnd()}");
            if (!string.IsNullOrWhiteSpace(entry.StackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("  ── Stack Trace ────────────────────────────────────────────");
                foreach (var line in entry.StackTrace.Split('\n'))
                    sb.AppendLine($"  {line.TrimEnd()}");
            }
            sb.AppendLine();
            File.AppendAllText(DailyFilePath(), sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────────────────
    // Sync bem-sucedido — registro compacto
    // ─────────────────────────────────────────────────────────────────
    public static void LogSuccess(string apiUrl, int pushed, int pulled, double durationMs)
    {
        try
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [OK] Sync concluído — " +
                       $"↑{pushed} enviados / ↓{pulled} recebidos — {durationMs:F0} ms — {apiUrl}{Environment.NewLine}";
            File.AppendAllText(DailyFilePath(), line, Encoding.UTF8);
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────────────────
    // Offline / sem rede — registro informativo
    // ─────────────────────────────────────────────────────────────────
    public static void LogOffline(string apiUrl)
    {
        try
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [OFFLINE] Sem rede — sync ignorado — {apiUrl}{Environment.NewLine}";
            File.AppendAllText(DailyFilePath(), line, Encoding.UTF8);
        }
        catch { }
    }

    public static string GetLogDirectory() => LogDirectory;

    private static string DailyFilePath() =>
        Path.Combine(LogDirectory, $"sync_{DateTime.Now:yyyy-MM-dd}.log");

    private static void EnsureDirectory() => Directory.CreateDirectory(LogDirectory);
}

// ─────────────────────────────────────────────────────────────────
// Estrutura de entrada de log
// ─────────────────────────────────────────────────────────────────
public class SyncLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Operation { get; set; } = "";
    public string ErrorCode { get; set; } = "";
    public string ErrorType { get; set; } = "";
    public HttpStatusCode? HttpStatusCode { get; set; }
    public string ApiUrl { get; set; } = "";
    public double DurationMs { get; set; }
    public string UserMessage { get; set; } = "";
    public string TechnicalDetail { get; set; } = "";
    public string StackTrace { get; set; } = "";
    /// <summary>Passos de diagnóstico separados por \n.</summary>
    public string Diagnosis { get; set; } = "";
}
