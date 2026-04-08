using Microsoft.EntityFrameworkCore;
using TesteAPI.Models;

namespace TesteAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Companion> Companions { get; set; }
        public DbSet<Signature> Signatures { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<WeatherDetail> WeatherDetails { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Occurrence> Occurrences { get; set; }
        public DbSet<EmployeePresence> EmployeePresences { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportCompanion> ReportCompanions { get; set; }
        public DbSet<ReportEquipment> ReportEquipments { get; set; }
        public DbSet<EquipmentUsage> EquipmentUsages { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}