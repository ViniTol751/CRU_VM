using Microsoft.EntityFrameworkCore;
using TesteAPI.Models;

namespace TesteAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Relatorio> Relatorios { get; set; }
    }
}