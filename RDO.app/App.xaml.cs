using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.IO;
using System.Linq;

namespace RDO.App
{
    public partial class App : Application
    {
        public Window? MainWindow { get; private set; }

        // Todas as migrations que este código de app já conhece.
        // Atualizar sempre que uma nova migration for adicionada.
        private static readonly string[] _todasMigrations =
        {
            "20260409180159_InitialCreate",
            "20260410132443_TornarCPFOpcional",
            "20260413161819_AddSyncQueue",
            "20260414170010_AddResponsavelCliente",
            "20260414193159_AddPhotoTypeForSQLite",
            "20260415120000_AddCreaToProject",
            "20260416100000_AddRevisaoToReport",
        };

        public App()
        {
            InitializeComponent();
            InicializarBanco();
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
            MainWindow = new MainWindow();
            MainWindow.Activate();
            ThemeManager.Apply(ThemeManager.Current); // aplica RequestedTheme ao conteúdo
        }
    }
}
