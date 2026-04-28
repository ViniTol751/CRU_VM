namespace RDO.App.Services;

public static class AppErrorCodes
{
    // DB
    public const string DB_001 = "DB-001";
    public const string DB_002 = "DB-002";
    public const string DB_003 = "DB-003";

    // SYNC
    public const string SYNC_001 = "SYNC-001";
    public const string SYNC_002 = "SYNC-002";
    public const string SYNC_003 = "SYNC-003";

    // PDF
    public const string PDF_001 = "PDF-001";
    public const string PDF_002 = "PDF-002";

    // AUTH
    public const string AUTH_001 = "AUTH-001";
    public const string AUTH_002 = "AUTH-002";
    public const string AUTH_003 = "AUTH-003";

    // IO
    public const string IO_001 = "IO-001";

    // FORM
    public const string FORM_001 = "FORM-001";

    public static string GetDescription(string code) => code switch
    {
        DB_001   => "Falha ao carregar dados.",
        DB_002   => "Falha ao salvar dados.",
        DB_003   => "Falha ao excluir registro.",
        SYNC_001 => "Sem conexão com a internet.",
        SYNC_002 => "Erro no servidor de sincronização.",
        SYNC_003 => "Falha inesperada na sincronização.",
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
        SYNC_002 => "O servidor pode estar indisponível. Tente em alguns minutos.",
        SYNC_003 => "Aguarde e tente novamente. Se persistir, contate o suporte.",
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
