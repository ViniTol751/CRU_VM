using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RDO.App
{
    public partial class App : Application
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        private const uint MB_ICONERROR = 0x10;

        public Window? MainWindow { get; private set; }

        // Todas as migrations que este código de app já conhece.
        // Atualizar sempre que uma nova migration for adicionada.
        private static readonly string[] _todasMigrations =
        {
            "20260409180159_InitialCreate",
            "20260410132443_TornarCPFOpcional",
            "20260413161819_AddSyncQueue",
            "20260413200000_RemoveSyncQueue",
            "20260414170010_AddResponsavelCliente",
            "20260414193159_AddPhotoTypeForSQLite",
            "20260414200000_AddReportCompanionAliasColumns",
            "20260414210000_AddPhotoType",
            "20260415120000_AddCreaToProject",
            "20260416100000_AddRevisaoToReport",
        };

        public App()
        {
            this.UnhandledException += (_, e) =>
            {
                e.Handled = true;
                var msg = e.Exception?.ToString() ?? "Erro desconhecido";
                MessageBox(IntPtr.Zero, msg, "RDO — Erro Fatal", MB_ICONERROR);
                Environment.Exit(1);
            };

            InitializeComponent();

            try
            {
                InicializarBanco();
            }
            catch (Exception ex)
            {
                // Mostra erro detalhado antes da janela existir (MessageBox nativo)
                MessageBox(IntPtr.Zero,
                    $"Falha ao inicializar banco de dados.\n\n{ex.GetType().Name}: {ex.Message}" +
                    (ex.InnerException != null ? $"\n\nCausa: {ex.InnerException.Message}" : ""),
                    "RDO — Erro de Inicialização", MB_ICONERROR);
                // Continua: a janela abre mesmo assim (usuário verá o erro e poderá reportar)
            }
        }

        private void InicializarBanco()
        {
            var dbPath = DbContextHelper.GetDbPath();
            bool usarEnsureCreated = false;

            System.Diagnostics.Debug.WriteLine($"[DB-INIT] Verificando banco em: {dbPath}");
            System.Diagnostics.Debug.WriteLine($"[DB-INIT] Banco existe: {File.Exists(dbPath)}");

            if (!File.Exists(dbPath))
            {
                // Banco não existe → criar do zero com EnsureCreated
                System.Diagnostics.Debug.WriteLine("[DB-INIT] Banco não existe, será criado com EnsureCreated");
                usarEnsureCreated = true;
            }
            else if (BancoDeveSerRecriado(dbPath))
            {
                // Banco existe com schema antigo (coluna Ativo órfã) → apagar e recriar
                System.Diagnostics.Debug.WriteLine("[DB-INIT] ⚠️ Banco deve ser recriado (schema incompatível)");
                try
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                    foreach (var ext in new[] { "-wal", "-shm" })
                        if (File.Exists(dbPath + ext)) File.Delete(dbPath + ext);
                    System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Banco deletado com sucesso");
                    usarEnsureCreated = true;
                }
                catch (Exception ex)
                {
                    // Não conseguiu deletar — continua com Migrate() como fallback
                    System.Diagnostics.Debug.WriteLine($"[DB-INIT] ✗ Erro ao deletar banco: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DB-INIT] Banco existe e está OK, usando Migrate()");
            }

            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            // Configura SQLite para usar WAL mode (mais robusto contra corrupção)
            try
            {
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
                db.Database.ExecuteSqlRaw("PRAGMA temp_store=MEMORY;");
                db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
                System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ WAL mode configurado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB-INIT] ⚠️ Erro ao configurar WAL: {ex.Message}");
            }

            // Garante colunas adicionadas em migrations recentes (segurança para bancos existentes)
            GarantirColunasExtras(db);

            if (usarEnsureCreated)
            {
                System.Diagnostics.Debug.WriteLine("[DB-INIT] Executando EnsureCreated...");
                // Cria todas as tabelas baseado no modelo atual.
                // EnsureCreated usa os nomes de tabela corretos (singular) definidos no modelo,
                // diferente do InitialCreate que usava nomes plurais para PostgreSQL.
                db.Database.EnsureCreated();

                // Cria a tabela de histórico de migrations e marca todas como já aplicadas.
                // O schema já está completo via EnsureCreated — migrations não precisam rodar.
                db.Database.ExecuteSqlRaw(
                    "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" " +
                    "(\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, " +
                    "\"ProductVersion\" TEXT NOT NULL)");

                foreach (var migId in _todasMigrations)
                    db.Database.ExecuteSqlRaw(
                        $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" " +
                        $"(\"MigrationId\", \"ProductVersion\") VALUES ('{migId}', '8.0.0')");
                
                System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ EnsureCreated concluído");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DB-INIT] Executando Migrate...");
                // Banco existente com migrations — aplica apenas as pendentes
                db.Database.Migrate();
                System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Migrate concluído");
            }

            // Seed de dados
            if (db.Usuarios.Find(1) == null)
            {
                db.Usuarios.Add(new Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@focusengenharia.com.br",
                    SenhaHash = "admin",
                    Perfil = "Admin",
                    Ativo = true
                });
                db.SaveChanges();
                System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Usuário admin criado");
            }

            // Seed dos colaboradores (idempotente — só cria os que não existem)
            RDO.App.Services.UserSeeder.Seed(db);
            System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Colaboradores verificados");

            // Seed das empresas (idempotente — só cria as que não existem)
            RDO.App.Services.EmpresaSeeder.Seed(db);
            System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Empresas verificadas");

            // Auto-arquiva obras com mais de 2 anos de início (reduz carga do front)
            ArquivarObrasAntigas(db);

            // Limpeza de produção: remove (soft-delete) obras inativas — roda uma única vez
            LimparObrasInativasProducao(db);

            System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Inicialização concluída");
        }

        /// <summary>
        /// Garante que colunas adicionadas em migrations recentes existam no banco.
        /// Usa ALTER TABLE ... ADD COLUMN IF NOT EXISTS (seguro — não falha se já existir).
        /// Necessário para bancos que foram criados via EnsureCreated e não passaram pelo Migrate().
        /// </summary>
        private static void GarantirColunasExtras(RdoDbContext db)
        {
            // Garante tabela Empresas (pode não existir em bancos antigos)
            try
            {
                // Renomeia tabela "Empresa" (singular, criada por versão anterior) para "Empresas" se necessário
                db.Database.ExecuteSqlRaw(
                    "CREATE TABLE IF NOT EXISTS \"Empresas\" (" +
                    "\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Empresas\" PRIMARY KEY AUTOINCREMENT, " +
                    "\"Nome\" TEXT NOT NULL DEFAULT '', " +
                    "\"ImagemPath\" TEXT NULL, " +
                    "\"IsActive\" INTEGER NOT NULL DEFAULT 1, " +
                    "\"UpdatedAt\" TEXT NOT NULL DEFAULT '2000-01-01', " +
                    "\"IsDeleted\" INTEGER NOT NULL DEFAULT 0)");

                // Se existia tabela "Empresa" (singular) com dados, migra os dados e remove a antiga
                db.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO \"Empresas\" (\"Id\", \"Nome\", \"ImagemPath\", \"IsActive\", \"UpdatedAt\", \"IsDeleted\") " +
                    "SELECT \"Id\", \"Nome\", \"ImagemPath\", \"IsActive\", \"UpdatedAt\", \"IsDeleted\" " +
                    "FROM \"Empresa\" WHERE 1=1");
                db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"Empresa\"");

                System.Diagnostics.Debug.WriteLine("[DB-INIT] ✓ Tabela Empresas garantida");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB-INIT] ⚠️ Erro ao garantir tabela Empresas: {ex.Message}");
            }

            var colunas = new[]
            {
                ("Report",    "Revisao",             "INTEGER NOT NULL DEFAULT 0"),
                ("Project",   "Crea",                "TEXT NOT NULL DEFAULT ''"),
                ("Project",   "ResponsavelCliente",  "TEXT NOT NULL DEFAULT ''"),
                ("Companion", "EmpresaId",            "INTEGER NULL"),
                ("Activity",  "ParentId",             "INTEGER NULL"),
                ("Photo",     "Type",                "TEXT NOT NULL DEFAULT 'photo'"),
            };

            foreach (var (tabela, coluna, definicao) in colunas)
            {
                try
                {
                    // Verifica se a coluna já existe
                    var existe = false;
                    using var conn = new Microsoft.Data.Sqlite.SqliteConnection(
                        $"Data Source={DbContextHelper.GetDbPath()};Mode=ReadWriteCreate;Cache=Shared;");
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"PRAGMA table_info(\"{tabela}\")";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (reader.GetString(1).Equals(coluna, StringComparison.OrdinalIgnoreCase))
                        {
                            existe = true;
                            break;
                        }
                    }

                    if (!existe)
                    {
                        db.Database.ExecuteSqlRaw(
                            $"ALTER TABLE \"{tabela}\" ADD COLUMN \"{coluna}\" {definicao}");
                        System.Diagnostics.Debug.WriteLine($"[DB-INIT] ✓ Coluna {tabela}.{coluna} adicionada");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DB-INIT] ⚠️ Erro ao garantir {tabela}.{coluna}: {ex.Message}");
                }
            }
        }

        private const string FlagLimpezaProducao = "ProductionCleanup_InativasV1";

        /// <summary>
        /// Remove (soft-delete) todas as obras inativas. Roda apenas uma vez por instalação.
        /// </summary>
        private static void LimparObrasInativasProducao(RDO.Data.Data.RdoDbContext db)
        {
            try
            {
                // Flag gravada no LocalSettings — garante execução única
                if (RDO.App.Services.LocalSettingsService.Get<bool?>(FlagLimpezaProducao) == true)
                    return;

                var inativas = db.Obras
                    .Where(o => !o.IsActive && !o.IsDeleted)
                    .ToList();

                if (inativas.Count > 0)
                {
                    foreach (var obra in inativas)
                    {
                        obra.IsDeleted = true;
                        obra.UpdatedAt = DateTime.UtcNow;
                    }
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"[DB-INIT] ✓ {inativas.Count} obra(s) inativa(s) removidas da produção");
                }

                RDO.App.Services.LocalSettingsService.Set(FlagLimpezaProducao, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB-INIT] ⚠️ Erro ao limpar inativas: {ex.Message}");
            }
        }

        /// <summary>
        /// Arquiva automaticamente obras cuja data de início é anterior a 2 anos.
        /// Opere de forma silenciosa — o usuário pode reativar manualmente na aba Inativas.
        /// </summary>
        private static void ArquivarObrasAntigas(RDO.Data.Data.RdoDbContext db)
        {
            try
            {
                var limite = DateTime.UtcNow.AddYears(-2);
                var antigas = db.Obras
                    .Where(o => o.IsActive && !o.IsDeleted && o.StartDate <= limite)
                    .ToList();

                if (antigas.Count == 0) return;

                foreach (var obra in antigas)
                {
                    obra.IsActive = false;
                    obra.UpdatedAt = DateTime.UtcNow;
                }
                db.SaveChanges();
                System.Diagnostics.Debug.WriteLine($"[DB-INIT] ✓ {antigas.Count} obra(s) arquivada(s) automaticamente (>2 anos)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB-INIT] ⚠️ Erro ao arquivar obras antigas: {ex.Message}");
            }
        }

        /// <summary>
        /// Retorna true se o banco deve ser deletado e recriado:
        ///  - Sem __EFMigrationsHistory → criado por EnsureCreated antigo (schema inconsistente)
        ///  - Tabela "Projects" (plural) existe → schema do PostgreSQL, incompatível com modelo SQLite
        /// </summary>
        private static bool BancoDeveSerRecriado(string dbPath)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();

                // Sem tabela de histórico = banco criado por EnsureCreated (pré-migrations)
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
                var temHistorico = (long)cmd.ExecuteScalar()! > 0;
                System.Diagnostics.Debug.WriteLine($"[DB-CHECK] Tem __EFMigrationsHistory: {temHistorico}");
                if (!temHistorico) return true;

                // Verifica se existe tabela com nome plural (Projects) = schema do PostgreSQL
                // InitialCreate cria "Projects", mas o modelo SQLite espera "Project"
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Projects'";
                var temProjects = (long)cmd.ExecuteScalar()! > 0;
                System.Diagnostics.Debug.WriteLine($"[DB-CHECK] Tem tabela 'Projects' (plural): {temProjects}");
                if (temProjects) return true;

                // Verifica se a tabela Project existe (deve existir)
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Project'";
                var temProject = (long)cmd.ExecuteScalar()! > 0;
                System.Diagnostics.Debug.WriteLine($"[DB-CHECK] Tem tabela 'Project' (singular): {temProject}");
                
                if (!temProject)
                {
                    System.Diagnostics.Debug.WriteLine("[DB-CHECK] ⚠️ Tabela 'Project' não existe!");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine("[DB-CHECK] ✓ Schema está OK");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB-CHECK] ✗ Erro ao verificar: {ex.Message}");
                // IMPORTANTE: Em caso de erro ao verificar, NÃO deletar o banco
                // Pode ser apenas um lock temporário ou erro de leitura
                // Melhor tentar Migrate() do que perder dados
                return false;
            }
        }

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            ThemeManager.LoadSaved();          // carrega tema ANTES de criar a janela
            try
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
                ThemeManager.Apply(ThemeManager.Current);
            }
            catch (Exception ex)
            {
                // Monta mensagem com todas as InnerExceptions para diagnóstico
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Falha ao inicializar a janela principal.");
                sb.AppendLine();
                var e = ex;
                int depth = 0;
                while (e != null && depth < 5)
                {
                    sb.AppendLine($"[{depth}] {e.GetType().FullName}: {e.Message}");
                    if (e.HResult != 0)
                        sb.AppendLine($"    HResult: 0x{e.HResult:X8}");
                    e = e.InnerException;
                    depth++;
                }
                sb.AppendLine();
                sb.AppendLine("StackTrace:");
                sb.AppendLine(ex.StackTrace);

                MessageBox(IntPtr.Zero, sb.ToString(), "RDO — Erro Fatal (Diagnóstico)", MB_ICONERROR);
                Environment.Exit(1);
            }
        }
    }
}
