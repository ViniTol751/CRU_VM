# =============================================================================
# publicar.ps1 — Publica a API na pasta de saída
# Execute este script na máquina de desenvolvimento
# =============================================================================

$outputDir = ".\deploy\publish"

Write-Host "Publicando a API..." -ForegroundColor Cyan
dotnet publish ..\Teste.csproj -c Release -o $outputDir --no-self-contained

Write-Host ""
Write-Host "Publicado em: $outputDir" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos passos:" -ForegroundColor Yellow
Write-Host "  1. Copie a pasta '$outputDir' para o servidor"
Write-Host "  2. No servidor, edite o arquivo 'appsettings.Production.json'"
Write-Host "     com a connection string e o Jwt:Secret corretos"
Write-Host "  3. Execute 'iniciar-servidor.ps1' no servidor"
