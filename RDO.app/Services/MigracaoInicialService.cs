using RDO.Data.Data;
using RDO.app.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RDO.app.Services;

/// <summary>
/// Serviço de migração one-time: exporta todos os dados do SQLite local para o servidor central.
/// Execute apenas uma vez, na primeira conexão com o servidor.
/// </summary>
public class MigracaoInicialService
{
    private readonly SyncService _syncService;

    public MigracaoInicialService(string apiBaseUrl)
    {
        _syncService = new SyncService(apiBaseUrl);
    }

    /// <summary>
    /// Envia todos os dados locais para o servidor (push completo).
    /// Após a migração, a sincronização regular via SyncService.SyncAsync() assume.
    /// </summary>
    public async Task<MigracaoResult> MigrarAsync()
    {
        if (!SyncService.IsNetworkAvailable())
            return new MigracaoResult { Sucesso = false, Mensagem = "Sem conexão com a rede." };

        try
        {
            // Usa o SyncAsync com since=MinValue para forçar push+pull completo
            var result = await _syncService.SyncAsync();

            if (result.IsOffline)
                return new MigracaoResult { Sucesso = false, Mensagem = "Sem conexão com o servidor." };

            if (!result.Success)
                return new MigracaoResult { Sucesso = false, Mensagem = result.Error ?? "Erro desconhecido." };

            return new MigracaoResult
            {
                Sucesso = true,
                Mensagem = $"Migração concluída. " +
                           $"Enviados: {result.PushedInserted} novos, {result.PushedUpdated} atualizados. " +
                           $"Recebidos do servidor: {result.PulledRecords} registros."
            };
        }
        catch (HttpRequestException ex)
        {
            return new MigracaoResult
            {
                Sucesso = false,
                Mensagem = $"Não foi possível conectar à API: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new MigracaoResult { Sucesso = false, Mensagem = ex.Message };
        }
    }
}

public class MigracaoResult
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
