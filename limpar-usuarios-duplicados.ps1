# ============================================================
# Script: limpar-usuarios-duplicados.ps1
# Remove usuários no padrão antigo (sem ponto no login),
# mantendo admin e os no novo padrão (nome.sobrenome)
# ============================================================

$db = "$env:LOCALAPPDATA\RDOApp\rdo_local.db"

if (-not (Test-Path $db)) {
    Write-Host "ERRO: Banco não encontrado em $db" -ForegroundColor Red
    exit 1
}

# Carrega o assembly do SQLite que já vem com o app
$dllPaths = @(
    "$env:LOCALAPPDATA\Packages\*RDO*\LocalCache\Local\*\Microsoft.Data.Sqlite.dll",
    "$env:LOCALAPPDATA\Packages\*RDO*\LocalCache\Local\*\e_sqlite3.dll"
)

# Tenta localizar o Microsoft.Data.Sqlite.dll no cache do NuGet ou no app
$sqliteDll = Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.data.sqlite.core" -Recurse -Filter "Microsoft.Data.Sqlite.dll" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName

if (-not $sqliteDll) {
    # Tenta no diretório do projeto
    $sqliteDll = Get-ChildItem "$PSScriptRoot" -Recurse -Filter "Microsoft.Data.Sqlite.dll" -ErrorAction SilentlyContinue |
                 Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}

if (-not $sqliteDll) {
    Write-Host "DLL do SQLite não encontrada automaticamente." -ForegroundColor Yellow
    Write-Host "Tentando via System.Data.SQLite do NuGet..." -ForegroundColor Yellow

    # Fallback: usa o binário sqlite3.exe se existir no PATH ou pasta atual
    $sqlite3exe = Get-Command sqlite3.exe -ErrorAction SilentlyContinue
    if (-not $sqlite3exe) {
        Write-Host ""
        Write-Host "Nenhuma forma de acessar o SQLite foi encontrada." -ForegroundColor Red
        Write-Host ""
        Write-Host "SOLUÇÃO MAIS SIMPLES: Baixe o DB Browser for SQLite:" -ForegroundColor Cyan
        Write-Host "  https://sqlitebrowser.org/dl/" -ForegroundColor White
        Write-Host ""
        Write-Host "Depois abra o banco em:" -ForegroundColor Cyan
        Write-Host "  $db" -ForegroundColor White
        Write-Host ""
        Write-Host "Vá em 'Execute SQL' e cole:" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  DELETE FROM Usuarios WHERE Email != 'admin' AND Email NOT LIKE '%.%';" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Clique em 'Run' e depois 'Write Changes'." -ForegroundColor Cyan
        exit 1
    }

    # Usa sqlite3.exe encontrado
    $sql = "SELECT '--- ANTES ---'; SELECT Id, Nome, Email FROM Usuarios ORDER BY Email; DELETE FROM Usuarios WHERE Email != 'admin' AND Email NOT LIKE '%.%'; SELECT '--- DEPOIS ---'; SELECT Id, Nome, Email FROM Usuarios ORDER BY Email;"
    & $sqlite3exe.Source $db $sql
    Write-Host "Concluído!" -ForegroundColor Green
    exit 0
}

# Carrega a DLL e executa
Add-Type -Path $sqliteDll

$connStr = "Data Source=$db"
$conn = New-Object Microsoft.Data.Sqlite.SqliteConnection($connStr)
$conn.Open()

Write-Host "=== USUÁRIOS ANTES ===" -ForegroundColor Cyan
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT Id, Nome, Email FROM Usuarios ORDER BY Email"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host ("  [{0}] {1,-30} -> {2}" -f $reader["Id"], $reader["Nome"], $reader["Email"])
}
$reader.Close()

Write-Host ""
$cmd.CommandText = "DELETE FROM Usuarios WHERE Email != 'admin' AND Email NOT LIKE '%.%'"
$deleted = $cmd.ExecuteNonQuery()
Write-Host "Registros removidos (padrão antigo): $deleted" -ForegroundColor Yellow

Write-Host ""
Write-Host "=== USUÁRIOS DEPOIS ===" -ForegroundColor Green
$cmd.CommandText = "SELECT Id, Nome, Email FROM Usuarios ORDER BY Email"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host ("  [{0}] {1,-30} -> {2}" -f $reader["Id"], $reader["Nome"], $reader["Email"])
}
$reader.Close()
$conn.Close()

Write-Host ""
Write-Host "Concluído!" -ForegroundColor Green
