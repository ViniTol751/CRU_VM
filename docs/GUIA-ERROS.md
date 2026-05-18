# Guia de Erros — RDO App

> **Versão:** 1.1  
> **Atualizado em:** 2026-04-28  
> **Público-alvo:** Usuários finais e equipe de suporte

---

## Como usar este guia

Quando o aplicativo exibe uma mensagem de erro, ela sempre inclui um **código no formato `MÓDULO-NNN`** (ex: `SYNC-002`). Localize esse código nas seções abaixo para entender o que aconteceu e como resolver.

Se o problema persistir após seguir os passos indicados, use o botão **"Copiar diagnóstico"** na janela de erro e envie o texto copiado para o suporte técnico.

### Dois tipos de código

O sistema usa dois tipos de código de erro:

| Tipo | Formato | Onde aparece | Finalidade |
|------|---------|--------------|------------|
| **Código padronizado** | `MÓDULO-NNN` (ex: `SYNC-002`) | Badge na barra de sync, janela de erro, este guia | Identificação rápida para o usuário e suporte |
| **Código interno** | Descritivo (ex: `SYNC-PULL-CONN`) | Arquivo de log, campo "Cód. Padronizado" no log | Diagnóstico técnico detalhado |

Ao abrir o arquivo de log, você verá os dois campos lado a lado:

```
  Código de Erro  : SYNC-PULL-CONN
  Cód. Padronizado: SYNC-002
```

Use o **código padronizado** para consultar este guia. Use o **código interno** ao reportar ao suporte técnico.

---

## Índice rápido

| Código | Descrição resumida | Seção |
|--------|--------------------|-------|
| [DB-001](#db-001--falha-ao-carregar-dados) | Falha ao carregar dados | Banco de dados |
| [DB-002](#db-002--falha-ao-salvar-dados) | Falha ao salvar dados | Banco de dados |
| [DB-003](#db-003--falha-ao-excluir-registro) | Falha ao excluir registro | Banco de dados |
| [SYNC-001](#sync-001--sem-conexão-com-a-internet) | Sem conexão com a internet | Sincronização |
| [SYNC-002](#sync-002--falha-de-conexão-com-a-api) | Falha de conexão com a API | Sincronização |
| [SYNC-003](#sync-003--erro-no-servidor-de-sincronização) | Erro no servidor (HTTP 4xx/5xx) | Sincronização |
| [SYNC-004](#sync-004--falha-ao-salvar-dados-recebidos-localmente) | Falha ao salvar dados recebidos | Sincronização |
| [SYNC-005](#sync-005--resposta-inesperada-da-api) | Resposta inesperada da API | Sincronização |
| [SYNC-006](#sync-006--timeout--a-api-não-respondeu) | Timeout — API não respondeu | Sincronização |
| [SYNC-007](#sync-007--falha-inesperada-na-sincronização) | Falha inesperada na sincronização | Sincronização |
| [PDF-001](#pdf-001--falha-ao-gerar-o-relatório-pdf) | Falha ao gerar o relatório PDF | PDF |
| [PDF-002](#pdf-002--falha-ao-abrir-o-arquivo-pdf) | Falha ao abrir o arquivo PDF | PDF |
| [AUTH-001](#auth-001--credenciais-inválidas) | Credenciais inválidas | Autenticação |
| [AUTH-002](#auth-002--falha-ao-criar-a-conta) | Falha ao criar a conta | Autenticação |
| [AUTH-003](#auth-003--falha-ao-redefinir-a-senha) | Falha ao redefinir a senha | Autenticação |
| [IO-001](#io-001--falha-ao-copiar-ou-acessar-arquivo) | Falha ao copiar ou acessar arquivo | Arquivos |
| [FORM-001](#form-001--formulário-com-dados-inválidos) | Formulário com dados inválidos | Formulário |

---

## Banco de Dados

### DB-001 — Falha ao carregar dados

**Quando aparece:**  
Ao abrir a tela Início, a aba Relatórios, a aba Rascunhos ou qualquer lista de cadastros. O aplicativo não conseguiu ler os dados armazenados localmente.

**Causas mais comuns:**
- O banco de dados local (`rdo_local.db`) está corrompido ou inacessível.
- Espaço em disco insuficiente no computador.
- O arquivo do banco foi movido ou excluído manualmente.
- Permissão de leitura negada na pasta de dados do aplicativo.

**Como resolver:**
1. Feche o aplicativo completamente.
2. Verifique se há espaço livre em disco (pelo menos 500 MB recomendados).
3. Reabra o aplicativo e tente novamente.
4. Se o erro persistir, reinicie o computador e tente novamente.
5. Se ainda assim não funcionar, contate o suporte técnico com o diagnóstico copiado.

---

### DB-002 — Falha ao salvar dados

**Quando aparece:**  
Ao salvar uma obra, um relatório, um funcionário, um equipamento, uma empresa ou qualquer outro cadastro. Os dados preenchidos não foram gravados.

**Causas mais comuns:**
- Disco cheio ou sem permissão de escrita.
- Banco de dados local bloqueado por outro processo.
- Dados em formato inesperado (ex: campo de data inválido).

**Como resolver:**
1. Verifique se há espaço livre em disco.
2. Feche outras instâncias do aplicativo que possam estar abertas.
3. Tente salvar novamente.
4. Se o erro ocorrer em um campo específico, revise os dados preenchidos (datas, campos numéricos).
5. Reinicie o aplicativo e repita a operação.
6. Se persistir, copie o diagnóstico e contate o suporte.

---

### DB-003 — Falha ao excluir registro

**Quando aparece:**  
Ao tentar excluir uma empresa, obra, funcionário, equipamento ou acompanhante técnico.

**Causas mais comuns:**
- O registro está sendo referenciado por outro dado (ex: obra vinculada a relatórios).
- Banco de dados temporariamente bloqueado.
- Problema de permissão de escrita.

**Como resolver:**
1. Tente novamente após alguns segundos.
2. Feche e reabra o aplicativo.
3. Verifique se o registro que deseja excluir não está vinculado a outros dados ativos (ex: relatórios associados à obra).
4. Se persistir, copie o diagnóstico e contate o suporte.

---

## Sincronização

> **Como ler o badge de erro de sync:**  
> O indicador na barra superior exibe `[SYNC-NNN]  mensagem`. O código entre colchetes é o código padronizado — use-o para localizar a seção correta neste guia.

### Mapeamento: código interno → código padronizado

Ao consultar os logs, use a tabela abaixo para encontrar a seção correspondente:

| Código interno (log) | Código padronizado (UI/guia) | Significado resumido |
|----------------------|------------------------------|----------------------|
| `SYNC-PUSH-CONN` | `SYNC-002` | Conexão recusada ao enviar dados |
| `SYNC-PULL-CONN` | `SYNC-002` | Conexão recusada ao receber dados |
| `SYNC-PUSH-HTTP` | `SYNC-003` | Erro HTTP ao enviar dados |
| `SYNC-PULL-HTTP` | `SYNC-003` | Erro HTTP ao receber dados |
| `SYNC-PULL-UPSERT` | `SYNC-004` | Falha ao gravar dados recebidos no banco local |
| `SYNC-PULL-EMPTY` | `SYNC-005` | Resposta vazia da API |
| `SYNC-TIMEOUT` | `SYNC-006` | Timeout de requisição |
| `SYNC-UNEXPECTED` | `SYNC-007` | Erro inesperado não classificado |
| `SYNC-STATE-READ` | `SYNC-007` | Falha ao ler estado de sync local |
| `SYNC-STATE-WRITE` | `SYNC-007` | Falha ao gravar estado de sync local |

---

### SYNC-001 — Sem conexão com a internet

**Quando aparece:**  
Ao tentar sincronizar dados com o servidor (automático ao abrir o app ou manual pelo botão de sync). O indicador de status mostra "Sem rede".

**Causas mais comuns:**
- Computador sem acesso à internet ou à rede local.
- Cabo de rede desconectado ou Wi-Fi desligado.
- VPN ativa bloqueando o acesso ao servidor.
- Servidor da API inacessível a partir da rede atual.

**Como resolver:**
1. Verifique se o computador está conectado à internet (abra um navegador e acesse qualquer site).
2. Se estiver em rede cabeada, confira o cabo.
3. Se usar VPN, verifique se ela permite acesso ao servidor da API.
4. Tente sincronizar novamente após restabelecer a conexão.
5. O aplicativo continua funcionando normalmente sem sincronização — os dados ficam salvos localmente e serão enviados na próxima vez que houver conexão.

> **Nota:** Este erro não causa perda de dados. Tudo que foi criado ou editado fica salvo localmente.

---

### SYNC-002 — Falha de conexão com a API

**Códigos internos relacionados:** `SYNC-PUSH-CONN`, `SYNC-PULL-CONN`

**Quando aparece:**  
O computador tem conexão com a internet, mas a API não respondeu — a conexão foi recusada ou o host não foi encontrado. O badge exibe `[SYNC-002]  API offline — conexão recusada`.

**Exemplo no log:**
```
  Código de Erro  : SYNC-PULL-CONN
  Cód. Padronizado: SYNC-002
  Tipo            : Conexão Recusada (ECONNREFUSED)
  Mensagem        : API offline — conexão recusada
```

**Causas mais comuns:**
- A API não está rodando no servidor.
- A URL da API está incorreta ou a porta está errada.
- O container Docker da API está parado.
- O PostgreSQL (banco do servidor) está fora do ar, impedindo a API de iniciar.

**Como resolver:**
1. Verifique se a API está rodando — abra o terminal e execute `dotnet run` na pasta da API.
2. Confirme a URL configurada no aplicativo (padrão: `http://localhost:5043`).
3. Se usar Docker, verifique se o container está ativo: `docker ps`.
4. Confirme que o PostgreSQL está rodando: `docker ps | grep postgres`.
5. Teste no navegador: `http://localhost:5043/swagger`.
6. Se o problema persistir, reinicie os containers: `docker-compose restart`.

---

### SYNC-003 — Erro no servidor de sincronização

**Códigos internos relacionados:** `SYNC-PUSH-HTTP`, `SYNC-PULL-HTTP`

**Quando aparece:**  
A API respondeu, mas retornou um erro HTTP (geralmente 500, 502, 503 ou 504). O badge exibe `[SYNC-003]  Erro interno na API`.

**Exemplo no log:**
```
  Código de Erro  : SYNC-PULL-HTTP
  Cód. Padronizado: SYNC-003
  Tipo            : HTTP Error
  Status HTTP     : 500 InternalServerError
```

**Causas mais comuns:**
- Erro interno na API (HTTP 500) — geralmente problema no banco de dados PostgreSQL.
- Serviço indisponível (HTTP 503) — API sobrecarregada ou em manutenção.
- Erro de gateway (HTTP 502) — problema entre containers Docker.
- Timeout no gateway (HTTP 504) — banco de dados lento ou travado.

**Como resolver:**
1. Aguarde alguns minutos e tente sincronizar novamente.
2. Verifique se o PostgreSQL está saudável: `docker ps` ou serviço do Windows.
3. Consulte os logs da API para a exceção detalhada.
4. Execute as migrations se necessário: `dotnet ef database update`.
5. Se usar Docker e o erro for 502/504, reinicie os containers: `docker-compose restart`.
6. Os dados locais estão seguros — a sincronização será retomada quando o servidor estiver disponível.

---

### SYNC-004 — Falha ao salvar dados recebidos localmente

**Código interno relacionado:** `SYNC-PULL-UPSERT`

**Quando aparece:**  
A API respondeu com sucesso e enviou os dados, mas o aplicativo não conseguiu gravá-los no banco de dados local (SQLite).

**Exemplo no log:**
```
  Código de Erro  : SYNC-PULL-UPSERT
  Cód. Padronizado: SYNC-004
  Tipo            : DbUpdateException
  Mensagem        : Erro ao salvar Projects localmente: ...
```

**Causas mais comuns:**
- O banco de dados local está desatualizado (migrations pendentes).
- Conflito de dados entre o servidor e o banco local.
- Espaço em disco insuficiente para gravar no SQLite.
- Arquivo do banco local corrompido.

**Como resolver:**
1. Feche e reabra o aplicativo — o banco local pode ter ficado em estado inconsistente.
2. Verifique se há espaço livre em disco.
3. Tente sincronizar novamente.
4. Se o erro persistir, abra os logs (botão "Ver logs") e envie ao suporte com o campo `Mensagem` do log — ele indica qual entidade falhou (ex: `Projects`, `Reports`).
5. O suporte pode precisar executar as migrations do SQLite para resolver.

---

### SYNC-005 — Resposta inesperada da API

**Código interno relacionado:** `SYNC-PULL-EMPTY`

**Quando aparece:**  
A API respondeu com HTTP 200 (sucesso), mas o corpo da resposta estava vazio ou em formato incompatível.

**Exemplo no log:**
```
  Código de Erro  : SYNC-PULL-EMPTY
  Cód. Padronizado: SYNC-005
  Tipo            : Resposta Vazia
  Mensagem        : Resposta vazia do servidor
```

**Causas mais comuns:**
- Versão da API incompatível com a versão do aplicativo.
- A API retornou um erro silencioso sem corpo.
- Problema de serialização/deserialização de dados.

**Como resolver:**
1. Verifique se a versão da API é compatível com a versão do aplicativo.
2. Consulte os logs da API para verificar se houve erro interno.
3. Tente sincronizar novamente.
4. Se persistir, contate o suporte com o diagnóstico copiado.

---

### SYNC-006 — Timeout — a API não respondeu

**Código interno relacionado:** `SYNC-TIMEOUT`

**Quando aparece:**  
A requisição foi enviada, mas a API não respondeu dentro do limite de 30 segundos.

**Causas mais comuns:**
- O banco de dados PostgreSQL está lento ou travado.
- A API está sobrecarregada com muitas requisições simultâneas.
- Problema de rede com alta latência.

**Como resolver:**
1. Aguarde 1–2 minutos e tente sincronizar novamente.
2. Verifique se o PostgreSQL está saudável: `docker ps | grep postgres`.
3. Verifique os logs do PostgreSQL para queries lentas ou locks.
4. Se usar Docker, reinicie os containers: `docker-compose restart`.
5. Se persistir, contate o suporte.

---

### SYNC-007 — Falha inesperada na sincronização

**Códigos internos relacionados:** `SYNC-UNEXPECTED`, `SYNC-STATE-READ`, `SYNC-STATE-WRITE`

**Quando aparece:**  
Ocorreu um erro não classificado durante a sincronização — pode ser uma exceção inesperada, falha ao ler/gravar o estado de sincronização local, ou qualquer outro problema não coberto pelos códigos anteriores.

**Como resolver:**
1. Aguarde 1–2 minutos e tente sincronizar novamente.
2. Feche e reabra o aplicativo.
3. Abra os logs (botão "Ver logs") — o campo `Código de Erro` no log indica o código interno exato para diagnóstico.
4. Se persistir, copie o diagnóstico e contate o suporte.

---

## PDF

### PDF-001 — Falha ao gerar o relatório PDF

**Quando aparece:**  
Ao tentar exportar um RDO como PDF, seja pela tela de edição do relatório ou pelo botão de exportação na lista.

**Causas mais comuns:**
- O relatório está incompleto (campos obrigatórios em branco).
- Problema ao acessar a logo da empresa (arquivo de imagem corrompido ou inacessível).
- Espaço em disco insuficiente para criar o arquivo temporário.
- Biblioteca de geração de PDF com falha interna.

**Como resolver:**
1. Verifique se o relatório está completamente preenchido (data, obra, pelo menos uma atividade).
2. Verifique se a logo da empresa está acessível (tente abrir o arquivo de imagem manualmente).
3. Libere espaço em disco se necessário.
4. Feche o relatório, reabra e tente exportar novamente.
5. Se o erro persistir, tente exportar sem logo (remova temporariamente a imagem da empresa em Cadastros → Empresas).
6. Copie o diagnóstico e contate o suporte.

---

### PDF-002 — Falha ao abrir o arquivo PDF

**Quando aparece:**  
O PDF foi gerado com sucesso, mas o sistema não conseguiu abri-lo automaticamente.

**Causas mais comuns:**
- Nenhum leitor de PDF instalado no computador (ex: Adobe Acrobat, Foxit, Edge).
- O arquivo PDF foi gerado em uma pasta sem permissão de leitura.
- O arquivo foi movido ou excluído antes de ser aberto.

**Como resolver:**
1. Instale um leitor de PDF (o Microsoft Edge, já incluso no Windows, abre PDFs nativamente).
2. Navegue manualmente até a pasta onde o PDF foi salvo e abra-o.
3. Verifique se o arquivo existe na pasta de destino.
4. Tente exportar novamente escolhendo uma pasta diferente.

---

## Autenticação

### AUTH-001 — Credenciais inválidas

**Quando aparece:**  
Na tela de login, ao tentar autenticar com usuário e senha.

**Causas mais comuns:**
- Usuário ou senha digitados incorretamente.
- Conta desativada pelo administrador.
- Caps Lock ativado ao digitar a senha.

**Como resolver:**
1. Verifique se o Caps Lock está desligado.
2. Confirme o nome de usuário (formato: `nome.sobrenome`, ex: `joao.silva`).
3. Se esqueceu a senha, clique em **"ESQUECEU A SENHA?"** na tela de login e redefina-a.
4. Se a conta foi desativada, contate o administrador do sistema.

---

### AUTH-002 — Falha ao criar a conta

**Quando aparece:**  
Ao tentar registrar uma nova conta pelo assistente de cadastro na tela de login.

**Causas mais comuns:**
- O nome de usuário escolhido já existe no sistema.
- Problema ao gravar os dados no banco local.
- Campos obrigatórios não preenchidos corretamente.

**Como resolver:**
1. Verifique se o nome de usuário sugerido já não está em uso (o sistema avisa durante o cadastro).
2. Tente um nome de usuário diferente.
3. Certifique-se de preencher todos os campos obrigatórios (nome, função, senha).
4. Feche e reabra o aplicativo e tente novamente.
5. Se persistir, contate o administrador.

---

### AUTH-003 — Falha ao redefinir a senha

**Quando aparece:**  
Ao usar a opção "Esqueceu a senha?" na tela de login.

**Causas mais comuns:**
- O nome de usuário informado não existe ou está digitado incorretamente.
- A conta está desativada.
- Problema ao gravar a nova senha no banco local.

**Como resolver:**
1. Confirme o nome de usuário exato (ex: `joao.silva`).
2. Verifique se a conta está ativa com o administrador.
3. Tente novamente após fechar e reabrir o aplicativo.
4. Se persistir, o administrador pode redefinir a senha diretamente no banco de dados.

---

## Arquivos

### IO-001 — Falha ao copiar ou acessar arquivo

**Quando aparece:**  
- Ao selecionar e salvar a logo de uma empresa em Cadastros → Empresas.
- Ao exportar uma lista como CSV (Cadastros ou Relatórios).
- Ao abrir a pasta de logs pelo botão "Ver logs".

**Causas mais comuns:**
- O arquivo de origem (ex: logo PNG) foi movido ou excluído.
- A pasta de destino não existe ou não tem permissão de escrita.
- O arquivo está sendo usado por outro programa (bloqueado).
- Caminho de rede (NAS) inacessível no momento.

**Como resolver:**
1. Verifique se o arquivo de origem ainda existe no local indicado.
2. Verifique se você tem permissão de escrita na pasta de destino.
3. Se for um arquivo de rede (NAS), confirme que o compartilhamento está acessível (tente abrir pelo Explorador de Arquivos).
4. Feche outros programas que possam estar usando o arquivo.
5. Tente novamente com um arquivo ou pasta diferente.
6. Para exportação CSV, escolha uma pasta local (ex: Documentos) em vez de uma pasta de rede.

---

## Formulário

### FORM-001 — Formulário com dados inválidos

**Quando aparece:**  
Ao tentar salvar qualquer formulário com campos obrigatórios em branco ou preenchidos incorretamente. Exemplos: salvar uma obra sem nome, publicar um RDO sem data, ou salvar configurações com URL em branco.

**Causas mais comuns:**
- Campo obrigatório deixado em branco.
- Formato de dado incorreto (ex: data inválida).
- Tentativa de publicar um relatório com status diferente de "Publicado".

**Como resolver:**
1. Revise todos os campos marcados com `*` (obrigatórios).
2. Preencha os campos em branco destacados.
3. Verifique se datas estão no formato correto.
4. Para relatórios: certifique-se de que o status está definido como "Publicado" antes de exportar.
5. Salve novamente após corrigir os campos.

---

## Diagnóstico e suporte

### Como ler o arquivo de log

Os logs de sincronização ficam em:

```
%LOCALAPPDATA%\RDOApp\Logs\
```

Para abrir essa pasta diretamente, clique no botão **"Ver logs"** que aparece no indicador de status quando há um erro de sincronização.

Cada entrada de erro no log tem o seguinte formato:

```
══════════════════════════════════════════════════════════════
  [ERRO] 2026-04-28 13:55:23 (UTC-03:00)
══════════════════════════════════════════════════════════════
  Operação        : Pull
  Código de Erro  : SYNC-PULL-CONN          ← código interno (para diagnóstico técnico)
  Cód. Padronizado: SYNC-002                ← código padronizado (consulte este guia)
  Tipo            : Conexão Recusada (ECONNREFUSED)
  Status HTTP     : N/A
  URL da API      : http://localhost:5043/api/sync/pull?since=...
  Duração         : 4146 ms

  Mensagem        : API offline — conexão recusada
  Detalhe Técn.   : No connection could be made because the target machine actively refused it.

  ── Diagnóstico / Troubleshooting ──────────────────────────
  • Inicie a API: cd TesteAPI && dotnet run
  • Verifique se o Docker está rodando: docker ps
  • Confirme que o PostgreSQL está ativo: docker ps | grep postgres
  • Confirme a porta na URL: http://localhost:5043
  • Teste no navegador: http://localhost:5043/swagger

  ── Stack Trace ────────────────────────────────────────────
  at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(...)
  ...
```

**Campos importantes:**
- **Código de Erro** — código interno detalhado, útil para o suporte técnico
- **Cód. Padronizado** — use este código para localizar a seção neste guia
- **Mensagem** — descrição legível do problema
- **Diagnóstico** — passos de resolução específicos para o erro ocorrido

### Como copiar o diagnóstico da janela de erro

Toda janela de erro do aplicativo possui um botão **"Copiar diagnóstico"**. Ao clicar, o seguinte é copiado para a área de transferência:

```
=== Diagnóstico RDO App ===
Versão   : 1.0.0
Data/Hora: 2026-04-28 14:32:10
Código   : SYNC-002
Descrição: Falha de conexão com a API.
Solução  : Verifique se a API está rodando e se a URL está correta.
Exceção  : HttpRequestException: No connection could be made...
===========================
```

Cole esse texto em um e-mail ou mensagem para o suporte técnico.

### Contato com o suporte

Ao contatar o suporte, sempre informe:
- O **código padronizado** exibido na UI (ex: `SYNC-002`)
- O **código interno** do log se disponível (ex: `SYNC-PULL-CONN`)
- O **texto do diagnóstico** copiado pelo botão
- O que você estava fazendo quando o erro apareceu
- Se o erro é recorrente ou ocorreu uma única vez

---

*Documento mantido em sincronia com `RDO.app/Services/AppErrorCodes.cs` e `RDO.app/Services/SyncLogger.cs`.*
