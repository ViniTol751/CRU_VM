# =============================================================================
# iniciar-servidor.ps1 — Inicia a API em modo Produção
# Execute este script no SERVIDOR onde a API vai rodar
# =============================================================================

$appDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Verifica se o arquivo de produção existe
$prodConfig = Join-Path $appDir "appsettings.Production.json"
if (-not (Test-Path $prodConfig)) {
    Write-Host "ERRO: appsettings.Production.json não encontrado em $appDir" -ForegroundColor Red
    Write-Host "Crie o arquivo com a connection string e o Jwt:Secret antes de continuar." -ForegroundColor Yellow
    exit 1
}

# Verifica se o Secret foi preenchido
$config = Get-Content $prodConfig | ConvertFrom-Json
if ($config.Jwt.Secret -like "*TROQUE*" -or $config.Jwt.Secret.Length -lt 32) {
    Write-Host "ERRO: Jwt:Secret não foi configurado em appsettings.Production.json" -ForegroundColor Red
    Write-Host "Substitua 'TROQUE-ISSO-POR-...' por uma senha longa e aleatória." -ForegroundColor Yellow
    exit 1
}

Write-Host "Iniciando API RDO em modo Produção..." -ForegroundColor Green
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet "$appDir\Teste.dll"
