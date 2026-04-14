using Microsoft.EntityFrameworkCore;
using RDO.Data.Models;

namespace RDO.Data.Data;

public class RdoDbContext : DbContext
{
    public RdoDbContext(DbContextOptions<RdoDbContext> options)
        : base(options) { }

    // ── Entidades de negócio ─────────────────────────────────────────────────
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<Companion> Companions { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<WeatherDetail> WeatherDetails { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Occurrence> Occurrences { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<Signature> Signatures { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ReportEquipment> ReportEquipments { get; set; }
    public DbSet<ReportCompanion> ReportCompanions { get; set; }
    public DbSet<EmployeePresence> EmployeePresences { get; set; }

    // ── Aliases em português (retrocompatibilidade) ──────────────────────────
    public DbSet<Project> Obras => Projects;
    public DbSet<Report> Relatorios => Reports;
    public DbSet<Employee> Funcionarios => Employees;
    public DbSet<User> Usuarios => Users;
    public DbSet<Companion> Acompanhantes => Companions;
    public DbSet<Equipment> EquipamentosCadastrados => Equipments;
    public DbSet<ProjectMember> MembrosObra => ProjectMembers;
    public DbSet<Activity> Atividades => Activities;
    public DbSet<WeatherDetail> Climas => WeatherDetails;
    public DbSet<Occurrence> Ocorrencias => Occurrences;
    public DbSet<Material> Materiais => Materials;
    public DbSet<Photo> Fotos => Photos;
    public DbSet<Signature> Assinaturas => Signatures;
    public DbSet<ReportEquipment> RelatorioEquipamentos => ReportEquipments;
    public DbSet<ReportCompanion> RelatorioAcompanhantes => ReportCompanions;
    public DbSet<EmployeePresence> PresencasFuncionarios => EmployeePresences;

    // ── Configuração do modelo ───────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Report>(e => {
            e.HasOne(r => r.Project).WithMany(p => p.Reports).HasForeignKey(r => r.ProjectId);
            e.HasOne(r => r.User).WithMany(u => u.Reports).HasForeignKey(r => r.UserId);
            e.HasOne(r => r.Companion).WithMany().HasForeignKey(r => r.CompanionId);
            e.Ignore(r => r.Obra);
            e.Ignore(r => r.Usuario);
            e.Ignore(r => r.Acompanhante);
            e.Ignore(r => r.Climas);
            e.Ignore(r => r.Atividades);
            e.Ignore(r => r.Ocorrencias);
            e.Ignore(r => r.Materiais);
            e.Ignore(r => r.Fotos);
            e.Ignore(r => r.Assinaturas);
            e.Ignore(r => r.Equipamentos);
            e.Ignore(r => r.RelatorioAcompanhantes);
        });

        modelBuilder.Entity<Activity>(e =>
            e.HasOne(a => a.Report).WithMany(r => r.Activities).HasForeignKey(a => a.ReportId));

        modelBuilder.Entity<WeatherDetail>(e =>
            e.HasOne(w => w.Report).WithMany(r => r.WeatherDetails).HasForeignKey(w => w.ReportId));

        modelBuilder.Entity<Occurrence>(e =>
            e.HasOne(o => o.Report).WithMany(r => r.Occurrences).HasForeignKey(o => o.ReportId));

        modelBuilder.Entity<Material>(e =>
            e.HasOne(m => m.Report).WithMany(r => r.Materials).HasForeignKey(m => m.ReportId));

        modelBuilder.Entity<Photo>(e =>
            e.HasOne(p => p.Report).WithMany(r => r.Photos).HasForeignKey(p => p.ReportId));

        modelBuilder.Entity<Signature>(e =>
            e.HasOne(s => s.Report).WithMany(r => r.Signatures).HasForeignKey(s => s.ReportId));

        modelBuilder.Entity<ReportEquipment>(e => {
            e.HasOne(re => re.Report).WithMany(r => r.Equipments).HasForeignKey(re => re.ReportId);
            e.HasOne(re => re.Equipment).WithMany().HasForeignKey(re => re.EquipmentId);
            e.Ignore(re => re.EquipamentoCadastrado);
        });

        modelBuilder.Entity<ReportCompanion>(e => {
            e.HasOne(rc => rc.Report).WithMany(r => r.ReportCompanions).HasForeignKey(rc => rc.ReportId);
            e.HasOne(rc => rc.Companion).WithMany().HasForeignKey(rc => rc.CompanionId);
            e.Ignore(rc => rc.Acompanhante);
        });

        modelBuilder.Entity<EmployeePresence>(e =>
            e.HasOne(ep => ep.Report).WithMany().HasForeignKey(ep => ep.ReportId));

        modelBuilder.Entity<ProjectMember>(e => {
            e.HasOne(pm => pm.Project).WithMany(p => p.Members).HasForeignKey(pm => pm.ProjectId);
            e.HasOne(pm => pm.User).WithMany(u => u.Projects).HasForeignKey(pm => pm.UserId);
        });
    }
}
