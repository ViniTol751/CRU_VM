using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using RDO.Data.Data;
using RDO.Data.Models;

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
            db.Database.EnsureCreated();

            // Garante que existe um usuário padrão (Id=1) — necessário pois UsuarioId é FK obrigatória
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
