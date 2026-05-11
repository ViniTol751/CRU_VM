using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace RDO.App.Services;

public static class ErrorDialogService
{
    private static string AppVersion
    {
        get
        {
            try
            {
                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
            }
            catch { return "1.0.0"; }
        }
    }

    /// <summary>
    /// Exibe um diálogo de erro padronizado com código, solução e botão de diagnóstico.
    /// </summary>
    /// <param name="xamlRoot">XamlRoot da página atual.</param>
    /// <param name="errorCode">Código no formato MODULE-NNN (use AppErrorCodes constants).</param>
    /// <param name="detail">Mensagem de detalhe adicional (pode ser null).</param>
    /// <param name="ex">Exceção original (opcional, usada no diagnóstico).</param>
    public static async Task ShowAsync(XamlRoot xamlRoot, string errorCode, string? detail = null, Exception? ex = null)
    {
        AppLogger.LogError(errorCode, detail, ex);

        var description = AppErrorCodes.GetDescription(errorCode);
        var solution    = AppErrorCodes.GetSolution(errorCode);
        var timestamp   = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var version     = AppVersion;

        // --- Conteúdo visual ---
        var stack = new StackPanel { Spacing = 10 };

        // Mensagem principal
        var msgBlock = new TextBlock
        {
            Text = detail ?? description,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
        };
        stack.Children.Add(msgBlock);

        // Bloco de solução
        var solutionPanel = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8, 12, 8),
            Background = (Brush)Application.Current.Resources["PanelBgBrush"],
        };
        var solutionStack = new StackPanel { Spacing = 4 };
        solutionStack.Children.Add(new TextBlock
        {
            Text = "Como resolver",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 12,
        });
        solutionStack.Children.Add(new TextBlock
        {
            Text = solution,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
        });
        solutionPanel.Child = solutionStack;
        stack.Children.Add(solutionPanel);

        // Código de erro + botão copiar
        var diagPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        diagPanel.Children.Add(new TextBlock
        {
            Text = $"Código: {errorCode}",
            FontSize = 11,
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
        });

        var copyBtn = new Button
        {
            Content = "Copiar diagnóstico",
            FontSize = 11,
            Padding = new Thickness(8, 4, 8, 4),
        };

        string diagText = BuildDiagText(errorCode, description, detail, solution, version, timestamp, ex);
        copyBtn.Click += (_, _) =>
        {
            var dp = new DataPackage();
            dp.SetText(diagText);
            Clipboard.SetContent(dp);
            copyBtn.Content = "Copiado ✓";
        };
        diagPanel.Children.Add(copyBtn);
        stack.Children.Add(diagPanel);

        var dialog = new ContentDialog
        {
            Title = "Algo deu errado",
            Content = stack,
            CloseButtonText = "Fechar",
            XamlRoot = xamlRoot,
            DefaultButton = ContentDialogButton.Close,
        };

        await dialog.ShowAsync();
    }

    private static string BuildDiagText(string code, string description, string? detail,
        string solution, string version, string timestamp, Exception? ex)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Diagnóstico RDO App ===");
        sb.AppendLine($"Versão   : {version}");
        sb.AppendLine($"Data/Hora: {timestamp}");
        sb.AppendLine($"Código   : {code}");
        sb.AppendLine($"Descrição: {description}");
        if (!string.IsNullOrEmpty(detail) && detail != description)
            sb.AppendLine($"Detalhe  : {detail}");
        sb.AppendLine($"Solução  : {solution}");
        if (ex != null)
        {
            sb.AppendLine($"Exceção  : {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                sb.AppendLine($"Inner    : {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }
        sb.AppendLine($"===========================");
        return sb.ToString();
    }
}
