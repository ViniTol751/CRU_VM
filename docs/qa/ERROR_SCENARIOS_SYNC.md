# Guia de Interpretacao de Erros de Sincronizacao

Este guia foi criado para ajudar usuarios e suporte a interpretar falhas comuns de sincronizacao entre `RDO.App` e `Teste (API)`.

## Como ler os codigos de erro

- `SYNC-PULL-400`: erro de requisicao no endpoint de download de dados (`pull`).
- `SYNC-PUSH-400`: erro de requisicao no endpoint de envio de dados (`push`).
- `SYNC-PUSH-500`: erro interno ao salvar dados no servidor.

## Cenarios de erro validados em testes

1. `SYNC-PULL-400` - parametro `since` invalido
   - O que significa: a data enviada no `pull` nao esta no formato esperado.
   - Acao para usuario: verificar data/hora do dispositivo e tentar novamente.
   - Exemplo valido: `2026-04-13T10:30:00Z`.

2. `SYNC-PUSH-400` - JSON invalido no envio
   - O que significa: a API nao conseguiu interpretar o corpo da requisicao.
   - Acao para usuario: atualizar o aplicativo e repetir a sincronizacao.
   - Acao para suporte: validar versao do app e payload enviado.

3. `SYNC-PUSH-500` - violacao de relacionamento no banco
   - O que significa: foi enviado um registro com referencia inexistente (ex.: `ProjectId`).
   - Acao para usuario: registrar o horario da falha e encaminhar ao suporte.
   - Acao para suporte: revisar consistencia das entidades relacionadas.

## Logs simulados para identificacao rapida

Os testes de QA geram logs simulados no formato abaixo para padronizar diagnostico:

```text
[SIMULATED-ERROR] code=SYNC-PULL-400 summary="Formato de data invalido em 'since'." action="Revise o relogio/dispositivo e tente novamente." hint="Parametro 'since' deve estar em ISO-8601, ex: 2026-04-13T10:30:00Z."
[SIMULATED-ERROR] code=SYNC-PUSH-400 summary="Payload JSON invalido." action="Atualize o aplicativo e repita a sincronizacao." hint="A API nao conseguiu desserializar o corpo da requisicao."
[SIMULATED-ERROR] code=SYNC-PUSH-500 summary="Falha interna ao persistir payload (violacao de relacionamento)." action="Enviar relatorio para suporte com horario da ocorrencia." hint="Possivel FK invalida em Report.ProjectId/UserId."
```

## Execucao dos testes de erro

Para rodar apenas os cenarios de erro:

```powershell
dotnet test "Tests/TesteAPI.Tests/TesteAPI.Tests.csproj" --filter "Pull_ShouldReturnBadRequest_WhenSinceHasInvalidFormat|Push_ShouldReturnBadRequest_WhenBodyHasInvalidJson|Push_ShouldReturnServerError_WhenPayloadViolatesForeignKey"
```
