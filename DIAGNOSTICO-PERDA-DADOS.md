# Diagnóstico: Perda de Dados Após Erro de PDF

## 🔍 Problema Relatado

1. Sincronização funciona e traz dados
2. Ao tentar exportar PDF → erro de layout
3. Ao reiniciar o app → **todos os dados desaparecem**

## 🛠️ Correções Implementadas

### 1. Logs Detalhados Adicionados

Adicionei logs em `App.xaml.cs` para rastrear exatamente o que acontece:

```
[DB-INIT] Verificando banco em: C:\Users\...\rdo_local.db
[DB-INIT] Banco existe: True
[DB-CHECK] Tem __EFMigrationsHistory: True
[DB-CHECK] Tem tabela 'Projects' (plural): False
[DB-CHECK] Tem tabela 'Project' (singular): True
[DB-CHECK] ✓ Coluna 'Ativo' não existe (OK)
[DB-CHECK] ✓ Schema está OK
[DB-INIT] Banco existe e está OK, usando Migrate()
[DB-INIT] Executando Migrate...
[DB-INIT] ✓ Migrate concluído
```

### 2. Proteção Contra Deleção Acidental

Mudei o `catch` em `BancoDeveSerRecriado` para retornar `false` em vez de `true`, evitando deletar o banco em caso de erros temporários.

### 3. Correção do Layout do PDF

Mudei de `Height()` fixo para `MaxHeight()` e renderização linha por linha em vez de tabela única.

## 📋 Como Diagnosticar

### Passo 1: Verificar Estado Inicial

Execute o script PowerShell:
```powershell
.\test-db-persistence.ps1
```

Isso mostra:
- Se o banco existe
- Tamanho do arquivo
- Contagem de registros em cada tabela

### Passo 2: Rodar o App com Logs

1. Abra o projeto no Visual Studio
2. Vá em **View → Output** (ou Ctrl+Alt+O)
3. No dropdown, selecione **Debug**
4. Rode o app (F5)
5. Observe os logs que começam com `[DB-INIT]` e `[DB-CHECK]`

### Passo 3: Reproduzir o Problema

1. Sincronize os dados
2. Execute o script novamente: `.\test-db-persistence.ps1`
   - **Anote a contagem de registros**
3. Tente exportar um PDF
4. **ANTES de fechar o app**, execute o script novamente
   - Verifique se os dados ainda estão lá
5. Feche o app
6. Execute o script novamente
   - Verifique se os dados desapareceram

### Passo 4: Analisar os Logs

Procure nos logs do Output por:

**Ao iniciar o app:**
```
[DB-INIT] Verificando banco em: ...
[DB-INIT] Banco existe: True/False
[DB-CHECK] ...
```

**Se aparecer:**
```
[DB-INIT] ⚠️ Banco deve ser recriado (schema incompatível)
[DB-INIT] ✓ Banco deletado com sucesso
```

Significa que o banco está sendo deletado. Copie TODOS os logs `[DB-CHECK]` que aparecem antes disso.

## 🔬 Possíveis Causas

### Causa 1: Crash do App Corrompe o Banco
Se o app crashar durante o erro de PDF, o SQLite pode ficar em estado inconsistente (arquivo WAL não commitado). Na próxima inicialização, o banco pode parecer corrompido.

**Solução:** Adicionar `PRAGMA journal_mode=WAL` e `PRAGMA synchronous=NORMAL`

### Causa 2: Verificação de Schema Falsa Positiva
A função `BancoDeveSerRecriado` pode estar detectando incorretamente que o banco precisa ser recriado.

**Solução:** Os logs vão mostrar exatamente qual verificação está falhando

### Causa 3: Múltiplas Instâncias do App
Se houver duas instâncias do app rodando, uma pode deletar o banco da outra.

**Solução:** Verificar no Task Manager se há múltiplos processos

## 🧪 Teste Específico

Execute este teste:

```powershell
# 1. Limpar tudo
Remove-Item "$env:LOCALAPPDATA\RDOApp\*" -Force -Recurse -ErrorAction SilentlyContinue

# 2. Rodar app, sincronizar, fechar

# 3. Verificar banco
.\test-db-persistence.ps1

# 4. Rodar app novamente (SEM fazer nada)

# 5. Verificar banco novamente
.\test-db-persistence.ps1
```

Se os dados desaparecerem apenas ao rodar o app (sem fazer nada), o problema está na inicialização.

## 📊 Informações Necessárias

Para diagnosticar, preciso que você me envie:

1. **Logs completos** do Output (Debug) desde o início até o erro
2. **Resultado** do script `test-db-persistence.ps1` antes e depois
3. **Mensagem de erro** completa do PDF
4. **Versão do Windows** e **.NET** que está usando

## 🚨 Workaround Temporário

Enquanto investigamos, você pode:

1. **Fazer backup do banco antes de exportar PDF:**
   ```powershell
   Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db" "$env:LOCALAPPDATA\RDOApp\rdo_local.db.backup"
   ```

2. **Restaurar se perder dados:**
   ```powershell
   Copy-Item "$env:LOCALAPPDATA\RDOApp\rdo_local.db.backup" "$env:LOCALAPPDATA\RDOApp\rdo_local.db" -Force
   ```

3. **Limitar fotos nos relatórios** para evitar erro de PDF

## 📝 Próximos Passos

1. Execute os testes acima
2. Copie os logs do Output
3. Me envie as informações
4. Vou analisar e identificar a causa raiz
