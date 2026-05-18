# Script para testar se o banco está sendo preservado
# Execute este script para verificar o estado do banco antes e depois de rodar o app

$dbPath = "$env:LOCALAPPDATA\RDOApp\rdo_local.db"

Write-Host "=== Teste de Persistência do Banco ===" -ForegroundColor Cyan
Write-Host ""

if (Test-Path $dbPath) {
    Write-Host "✓ Banco existe em: $dbPath" -ForegroundColor Green
    
    # Mostra informações do arquivo
    $fileInfo = Get-Item $dbPath
    Write-Host "  Tamanho: $($fileInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "  Modificado: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
    
    # Tenta contar registros
    try {
        Add-Type -Path "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Data.SQLite\v4.0_1.0.118.0__db937bc2d44ff139\System.Data.SQLite.dll" -ErrorAction SilentlyContinue
        
        $conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$dbPath")
        $conn.Open()
        
        $tables = @("Project", "User", "Employee", "Equipment", "Companion", "Report")
        
        Write-Host ""
        Write-Host "Contagem de registros:" -ForegroundColor Yellow
        
        foreach ($table in $tables) {
            try {
                $cmd = $conn.CreateCommand()
                $cmd.CommandText = "SELECT COUNT(*) FROM `"$table`""
                $count = $cmd.ExecuteScalar()
                Write-Host "  $table : $count" -ForegroundColor White
            } catch {
                Write-Host "  $table : (tabela não existe)" -ForegroundColor DarkGray
            }
        }
        
        $conn.Close()
    } catch {
        Write-Host ""
        Write-Host "⚠ Não foi possível ler o banco (pode estar em uso)" -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ Banco NÃO existe em: $dbPath" -ForegroundColor Red
}

Write-Host ""
Write-Host "Pressione qualquer tecla para continuar..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
