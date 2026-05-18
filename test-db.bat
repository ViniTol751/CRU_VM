@echo off
echo === Teste de Persistencia do Banco ===
echo.

set "dbPath=%LOCALAPPDATA%\RDOApp\rdo_local.db"

if exist "%dbPath%" (
    echo [OK] Banco existe em: %dbPath%
    echo.
    
    for %%A in ("%dbPath%") do (
        echo   Tamanho: %%~zA bytes
        echo   Modificado: %%~tA
    )
    
    echo.
    echo Arquivos relacionados:
    dir /b "%LOCALAPPDATA%\RDOApp\rdo_local.db*" 2>nul
    
) else (
    echo [ERRO] Banco NAO existe em: %dbPath%
)

echo.
echo Pressione qualquer tecla para continuar...
pause >nul
