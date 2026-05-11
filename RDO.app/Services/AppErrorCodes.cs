namespace RDO.App.Services;

public static class AppErrorCodes
{
    // ── Banco de dados ────────────────────────────────────────────────
    public const string DB_001 = "DB-001";
    public const string DB_002 = "DB-002";
    public const string DB_003 = "DB-003";

    // ── Sincronização (códigos públicos exibidos na UI) ───────────────
    public const string SYNC_001 = "SYNC-001";   // Sem rede
    public const string SYNC_002 = "SYNC-002";   // Erro de conexão com a API (recusada / host não encontrado)
    public const string SYNC_003 = "SYNC-003";   // Erro HTTP do servidor (4xx / 5xx)
    public const string SYNC_004 = "SYNC-004";   // Falha ao persistir dados recebidos (upsert local)
    public const string SYNC_005 = "SYNC-005";   // Resposta vazia ou inesperada da API
    public const string SYNC_006 = "SYNC-006";   // Timeout de requisição
    public const string SYNC_007 = "SYNC-007";   // Erro inesperado / não classificado

    // ── PDF ───────────────────────────────────────────────────────────
    public const string PDF_001 = "PDF-001";
    public const string PDF_002 = "PDF-002";

    // ── Autenticação ──────────────────────────────────────────────────
    public const string AUTH_001 = "AUTH-001";
    public const string AUTH_002 = "AUTH-002";
    public const string AUTH_003 = "AUTH-003";

    // ── Arquivos / IO ─────────────────────────────────────────────────
    public const string IO_001 = "IO-001";

    // ── Formulário ────────────────────────────────────────────────────
    public const string FORM_001 = "FORM-001";

    // ─────────────────────────────────────────────────────────────────
    // Mapeamento: código interno do SyncService → código padronizado
    // Os códigos internos (ex: SYNC-PULL-CONN) são usados nos logs;
    // os códigos padronizados (ex: SYNC-002) são exibidos na UI.
    // ─────────────────────────────────────────────────────────────────
    public static string MapToStandardCode(string internalCode) => internalCode switch
    {
        // Conexão recusada / host não encontrado
        "SYNC-PUSH-CONN"   => SYNC_002,
        "SYNC-PULL-CONN"   => SYNC_002,

        // Erros HTTP do servidor
        "SYNC-PUSH-HTTP"   => SYNC_003,
        "SYNC-PULL-HTTP"   => SYNC_003,

        // Falha ao salvar dados recebidos no banco local
        "SYNC-PULL-UPSERT" => SYNC_004,

        // Resposta vazia ou inesperada
        "SYNC-PULL-EMPTY"  => SYNC_005,

        // Timeout
        "SYNC-TIMEOUT"     => SYNC_006,

        // Erros inesperados
        "SYNC-UNEXPECTED"  => SYNC_007,

        // Estado de sincronização (leitura/escrita do arquivo local)
        "SYNC-STATE-READ"  => SYNC_007,
        "SYNC-STATE-WRITE" => SYNC_007,

        // Já é um código padronizado — retorna como está
        _ when internalCode.Length == 8 && internalCode[4] == '-'
               && char.IsDigit(internalCode[5]) => internalCode,

        // Fallback
        _ => SYNC_007
    };

    public static string GetDescription(string code) => code switch
    {
        DB_001   => "Falha ao carregar dados.",
        DB_002   => "Falha ao salvar dados.",
        DB_003   => "Falha ao excluir registro.",
        SYNC_001 => "Sem conexão com a internet.",
        SYNC_002 => "Falha de conexão com a API.",
        SYNC_003 => "Erro no servidor de sincronização.",
        SYNC_004 => "Falha ao salvar dados recebidos localmente.",
        SYNC_005 => "Resposta inesperada da API.",
        SYNC_006 => "Timeout — a API não respondeu.",
        SYNC_007 => "Falha inesperada na sincronização.",
        PDF_001  => "Falha ao gerar o relatório PDF.",
        PDF_002  => "Falha ao abrir o arquivo PDF.",
        AUTH_001 => "Credenciais inválidas.",
        AUTH_002 => "Falha ao criar a conta.",
        AUTH_003 => "Falha ao redefinir a senha.",
        IO_001   => "Falha ao copiar ou acessar arquivo.",
        FORM_001 => "Formulário com dados inválidos.",
        _        => "Erro desconhecido.",
    };

    public static string GetSolution(string code) => code switch
    {
        DB_001   => "Reinicie o aplicativo. Se persistir, verifique o espaço em disco.",
        DB_002   => "Verifique se há espaço em disco disponível e tente novamente.",
        DB_003   => "Tente novamente. Se persistir, reinicie o aplicativo.",
        SYNC_001 => "Verifique sua conexão com a internet e tente novamente.",
        SYNC_002 => "Verifique se a API está rodando e se a URL está correta. Consulte o Guia de Erros.",
        SYNC_003 => "O servidor pode estar indisponível. Tente em alguns minutos.",
        SYNC_004 => "O banco local pode estar desatualizado. Feche e reabra o aplicativo.",
        SYNC_005 => "Verifique se a versão da API é compatível com o aplicativo.",
        SYNC_006 => "A API demorou muito para responder. Verifique se está rodando.",
        SYNC_007 => "Aguarde e tente novamente. Se persistir, contate o suporte.",
        PDF_001  => "Verifique se o relatório está completo e tente novamente.",
        PDF_002  => "Verifique se há um leitor de PDF instalado no sistema.",
        AUTH_001 => "Verifique o usuário e a senha digitados.",
        AUTH_002 => "Tente novamente. Se persistir, contate o administrador.",
        AUTH_003 => "Verifique se o usuário informado está cadastrado.",
        IO_001   => "Verifique se o arquivo existe e se você tem permissão de acesso.",
        FORM_001 => "Preencha todos os campos obrigatórios corretamente.",
        _        => "Contate o suporte técnico.",
    };
}
