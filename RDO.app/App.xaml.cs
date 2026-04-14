using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using RDO.Data.Data;
using RDO.Data.Models;
using System.Linq;

namespace RDO.App
{
    public partial class App : Application
    {
        public Window? MainWindow { get; private set; }

        public App()
        {
            InitializeComponent();
            InicializarBanco();
        }

        private void InicializarBanco()
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());

            // 1. PRIMEIRO: remove colunas alias órfãs que podem ter sido criadas antes das migrations
            // (banco criado com EnsureCreated quando esses aliases ainda eram colunas mapeadas)
            LimparColunasOrfas(db);

            // 2. Cria o banco e aplica todas as migrations pendentes
            db.Database.Migrate();

            // 3. Registra migrations antigas como já aplicadas
            var applied = db.Database.GetAppliedMigrations().ToList();

            if (!applied.Contains("20260409180159_InitialCreate"))
                db.Database.ExecuteSqlRaw(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260409180159_InitialCreate', '8.0.0')");

            if (!applied.Contains("20260410132443_TornarCPFOpcional"))
                db.Database.ExecuteSqlRaw(
                    "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('20260410132443_TornarCPFOpcional', '8.0.0')");

            // 3. Seed de dados
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
            }
        }

        private static void LimparColunasOrfas(DbContext db)
        {
            // Colunas alias PT que existiam no banco antes das migrations (criado via EnsureCreated)
            // e que agora são [NotMapped]. O EF não as inclui no INSERT, quebrando a constraint NOT NULL.
            var colunasProject = new[] { "Ativo", "Nome", "Endereco", "Grupo", "Responsavel",
                                         "TipoContrato", "Contratante", "DataInicio",
                                         "PrevisaoTermino", "ImagemPath" };
            foreach (var col in colunasProject)
            {
                try { db.Database.ExecuteSqlRaw("ALTER TABLE \"Project\" DROP COLUMN \"" + col + "\""); }
                catch { /* coluna não existe ou SQLite não suporta — ignora */ }
            }
        }

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            ThemeManager.LoadSaved();
        }
    }
}
