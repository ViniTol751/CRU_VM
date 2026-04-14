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

            // 1. PRIMEIRO: cria o banco e aplica todas as migrations pendentes
            db.Database.Migrate();

            // 2. DEPOIS: registra migrations antigas como já aplicadas
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

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
            ThemeManager.LoadSaved();
        }
    }
}
