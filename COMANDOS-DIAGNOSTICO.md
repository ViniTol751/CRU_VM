# Comandos de Diagnóstico

## 🔧 Opção 1: Usar arquivo .bat (mais simples)

Execute no terminal (CMD ou PowerShell):
```cmd
test-db.bat
```

---

## 🔧 Opção 2: Comando direto no PowerShell

Copie e cole este comando no PowerShell:

```powershell
$db = "$env:LOCALAPPDATA\RDOApp\rdo_local.db"; if (Test-Path $db) { Write-Host "✓ Banco existe" -ForegroundColor Green; Get-Item $db | Select-Object Length, LastWriteTime; Get-ChildItem "$env:LOCALAPPDATA\RDOApp\rdo_local.db*" | Select-Object Name, Length } else { Write-Host "✗ Banco NÃO existe" -ForegroundColor Red }
```

---

## 🔧 Opção 3: Habilitar execução de scripts PowerShell

Se quiser usar o arquivo `.ps1`, execute primeiro:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned -Force
```

Depois pode executar:
```powershell
.\test-db-persistence.ps1
```

---

## 📊 Comandos Úteis

### Ver tamanho do banco
```powershell
(Get-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db").Length / 1KB
```

### Ver quando foi modificado
```powershell
(Get-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db").LastWriteTime
```

### Listar todos os arquivos do banco
```powershell
Get-ChildItem "$env:LOCALAPPDATA\RDOApp\rdo_local.db*"
```

### Fazer backup do banco
```powershell
Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db" "$env:LOCALAPPDATA\RDOApp\rdo_local.db.backup"
```

### Restaurar backup
```powershell
Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db.backup" "$env:LOCALAPPDATA\RDOApp\rdo_local.db" -Force
```

### Deletar banco (para testar do zero)
```powershell
Remove-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db*" -Force
```

### Ver logs do app em tempo real
No Visual Studio:
1. Pressione `Ctrl+Alt+O` (ou View → Output)
2. No dropdown, selecione **Debug**
3. Rode o app (F5)
4. Procure por linhas com `[DB-INIT]` e `[DB-CHECK]`

---

## 🧪 Teste Completo

Execute estes comandos em sequência:

```powershell
# 1. Limpar tudo
Remove-Item "$env:LOCALAPPDATA\RDOApp\*" -Force -Recurse -ErrorAction SilentlyContinue

# 2. Verificar que está vazio
Get-ChildItem "$env:LOCALAPPDATA\RDOApp\" -ErrorAction SilentlyContinue

# 3. Rodar o app, sincronizar, fechar

# 4. Verificar banco foi criado
Get-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db"

# 5. Ver tamanho
(Get-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db").Length / 1KB

# 6. Fazer backup
Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db" "$env:LOCALAPPDATA\RDOApp\backup-antes-pdf.db"

# 7. Rodar app, tentar exportar PDF, fechar

# 8. Comparar tamanhos
Get-ChildItem "$env:LOCALAPPDATA\RDOApp\*.db" | Select-Object Name, Length, LastWriteTime
```

---

## 🔍 Interpretando os Resultados

### Banco Saudável
```
Name              Length    LastWriteTime
----              ------    -------------
rdo_local.db      1234567   15/04/2026 10:30:00
rdo_local.db-wal  32768     15/04/2026 10:30:00
rdo_local.db-shm  32768     15/04/2026 10:30:00
```

Se você vê os 3 arquivos, o WAL mode está ativo ✓

### Banco Vazio/Corrompido
```
Name              Length    LastWriteTime
----              ------    -------------
rdo_local.db      0         15/04/2026 10:30:00
```

Se o tamanho é 0 ou muito pequeno (< 50KB), o banco está vazio ✗

### Banco Deletado
```
Get-Item: Cannot find path '...\rdo_local.db' because it does not exist.
```

Se o arquivo não existe, foi deletado ✗
