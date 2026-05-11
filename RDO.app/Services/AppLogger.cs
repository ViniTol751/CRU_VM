using System;
using System.IO;
using System.Text;

namespace RDO.App.Services;

/// <summary>
/// Grava logs por módulo em arquivos diários em %LocalAppData%\RDOApp\Logs\.
/// Um arquivo por módulo por dia: db_YYYY-MM-DD.log, auth_YYYY-MM-DD.log, etc.
/// O módulo é derivado automaticamente do prefixo do código de erro (DB, AUTH, IO, FORM, PDF, SYNC).
/// </summary>
public static class AppLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "Logs");

    // ─────────────────────────────────────────────────────────────────
    // Erro de módulo — log detalhado (mesmo formato do SyncLogger)
    // ─────────────────────────────────────────────────────────────────
    public static void LogError(string errorCode, string? detail = null, Exception? ex = null)
    {
        try
        {
            var module = ExtractModule(errorCode);
            EnsureDirectory();

            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  [ERRO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} (UTC{DateTime.Now:zzz})");
            sb.AppendLine("══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Código         : {errorCode}");
            sb.AppendLine($"  Módulo         : {module}");
            sb.AppendLine($"  Descrição      : {AppErrorCodes.GetDescription(errorCode)}");

            if (!string.IsNullOrWhiteSpace(detail))
                sb.AppendLine($"  Detalhe        : {detail}");

            sb.AppendLine();
            sb.AppendLine("  ── Solução Sugerida ───────────────────────────────────────");
            sb.AppendLine($"  {AppErrorCodes.GetSolution(errorCode)}");

            if (ex != null)
            {
                sb.AppendLine();
                sb.AppendLine("  ── Exceção ────────────────────────────────────────────────");
                sb.AppendLine($"  Tipo    : {ex.GetType().Name}");
                sb.AppendLine($"  Mensagem: {ex.Message}");
                if (ex.InnerException != null)
                    sb.AppendLine($"  Inner   : {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");

                if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    sb.AppendLine();
                    sb.AppendLine("  ── Stack Trace ────────────────────────────────────────────");
                    foreach (var line in ex.StackTrace.Split('\n'))
                        sb.AppendLine($"  {line.TrimEnd()}");
                }
            }

            sb.AppendLine();
            File.AppendAllText(ModuleFilePath(module), sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────────────────
    // Informação — registro compacto (operação bem-sucedida)
    // ─────────────────────────────────────────────────────────────────
    public static void LogInfo(string module, string message)
    {
        try
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [OK]   {message}{Environment.NewLine}";
            File.AppendAllText(ModuleFilePath(module.ToUpperInvariant()), line, Encoding.UTF8);
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────────────────
    // Aviso — registro compacto (situação não-crítica)
    // ─────────────────────────────────────────────────────────────────
    public static void LogWarning(string module, string message)
    {
        try
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}{Environment.NewLine}";
            File.AppendAllText(ModuleFilePath(module.ToUpperInvariant()), line, Encoding.UTF8);
        }
        catch { }
    }

    public static string GetLogDirectory() => LogDirectory;

    // ─────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Extrai o prefixo do módulo a partir do código de erro. Ex: "DB-001" → "DB".</summary>
    private static string ExtractModule(string errorCode)
    {
        var idx = errorCode.IndexOf('-');
        return idx > 0 ? errorCode[..idx].ToUpperInvariant() : "APP";
    }

    /// <summary>Caminho do arquivo de log diário para o módulo informado.</summary>
    private static string ModuleFilePath(string module) =>
        Path.Combine(LogDirectory, $"{module.ToLowerInvariant()}_{DateTime.Now:yyyy-MM-dd}.log");

    private static void EnsureDirectory() => Directory.CreateDirectory(LogDirectory);
}
