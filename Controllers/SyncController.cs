using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SyncController(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // PULL — cliente baixa tudo que mudou desde X
        // ─────────────────────────────────────────────
        [HttpGet("pull")]
        public async Task<IActionResult> Pull([FromQuery] DateTime since)
        {
            var payload = new SyncPullPayload
            {
                ServerTime          = DateTime.UtcNow,
                Projects            = await _context.Projects.Where(x => x.UpdatedAt > since).ToListAsync(),
                Users               = await _context.Users.Where(x => x.UpdatedAt > since).ToListAsync(),
                Employees           = await _context.Employees.Where(x => x.UpdatedAt > since).ToListAsync(),
                Equipments          = await _context.Equipments.Where(x => x.UpdatedAt > since).ToListAsync(),
                Companions          = await _context.Companions.Where(x => x.UpdatedAt > since).ToListAsync(),
                Reports             = await _context.Reports.Where(x => x.UpdatedAt > since).ToListAsync(),
                WeatherDetails      = await _context.WeatherDetails.Where(x => x.UpdatedAt > since).ToListAsync(),
                Activities          = await _context.Activities.Where(x => x.UpdatedAt > since).ToListAsync(),
                Occurrences         = await _context.Occurrences.Where(x => x.UpdatedAt > since).ToListAsync(),
                Materials           = await _context.Materials.Where(x => x.UpdatedAt > since).ToListAsync(),
                Photos              = await _context.Photos.Where(x => x.UpdatedAt > since).ToListAsync(),
                Signatures          = await _context.Signatures.Where(x => x.UpdatedAt > since).ToListAsync(),
                ProjectMembers      = await _context.ProjectMembers.Where(x => x.UpdatedAt > since).ToListAsync(),
                ReportEquipments    = await _context.ReportEquipments.Where(x => x.UpdatedAt > since).ToListAsync(),
                ReportCompanions    = await _context.ReportCompanions.Where(x => x.UpdatedAt > since).ToListAsync(),
            };

            return Ok(payload);
        }

        // ─────────────────────────────────────────────
        // PUSH — cliente envia dados locais para o servidor
        // ─────────────────────────────────────────────
        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] SyncPushPayload payload)
        {
            int inserted = 0, updated = 0, skipped = 0;

            var r1 = await UpsertRange(_context.Employees, payload.Employees,
                (e, i) => { e.Name = i.Name; e.JobTitle = i.JobTitle; e.Company = i.Company;
                    e.Type = i.Type; e.Contact = i.Contact; e.IsActive = i.IsActive;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r1.ins; updated += r1.upd; skipped += r1.skp;

            var r2 = await UpsertRange(_context.Companions, payload.Companions,
                (e, i) => { e.Name = i.Name; e.Role = i.Role; e.Group = i.Group;
                    e.Contact = i.Contact; e.IsActive = i.IsActive;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r2.ins; updated += r2.upd; skipped += r2.skp;

            var r3 = await UpsertRange(_context.Equipments, payload.Equipments,
                (e, i) => { e.Name = i.Name; e.Manufacturer = i.Manufacturer; e.Model = i.Model;
                    e.SerialNumber = i.SerialNumber; e.IsActive = i.IsActive;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r3.ins; updated += r3.upd; skipped += r3.skp;

            var r4 = await UpsertRange(_context.Users, payload.Users,
                (e, i) => { e.Name = i.Name; e.Email = i.Email; e.Profile = i.Profile;
                    e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; });
            inserted += r4.ins; updated += r4.upd; skipped += r4.skp;

            var r5 = await UpsertRange(_context.Projects, payload.Projects,
                (e, i) => { e.Name = i.Name; e.Address = i.Address; e.ART = i.ART;
                    e.Group = i.Group; e.Status = i.Status; e.Manager = i.Manager;
                    e.ContractType = i.ContractType; e.Client = i.Client;
                    e.StartDate = i.StartDate; e.ExpectedEndDate = i.ExpectedEndDate;
                    e.ImagePath = i.ImagePath; e.IsActive = i.IsActive;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r5.ins; updated += r5.upd; skipped += r5.skp;

            var r6 = await UpsertRange(_context.Reports, payload.Reports,
                (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                    e.CompanionId = i.CompanionId; e.Number = i.Number;
                    e.Date = i.Date; e.CheckInTime = i.CheckInTime;
                    e.CheckOutTime = i.CheckOutTime; e.BreakTime = i.BreakTime;
                    e.GeneralNotes = i.GeneralNotes; e.Status = i.Status;
                    e.IsDraft = i.IsDraft; e.IsDeleted = i.IsDeleted; });
            inserted += r6.ins; updated += r6.upd; skipped += r6.skp;

            var r7 = await UpsertRange(_context.Activities, payload.Activities,
                (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                    e.Location = i.Location; e.Status = i.Status;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r7.ins; updated += r7.upd; skipped += r7.skp;

            var r8 = await UpsertRange(_context.Occurrences, payload.Occurrences,
                (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                    e.Tags = i.Tags; e.StartTime = i.StartTime; e.EndTime = i.EndTime;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r8.ins; updated += r8.upd; skipped += r8.skp;

            var r9 = await UpsertRange(_context.Materials, payload.Materials,
                (e, i) => { e.ReportId = i.ReportId; e.Name = i.Name;
                    e.Quantity = i.Quantity; e.Unit = i.Unit;
                    e.Type = i.Type; e.IsDeleted = i.IsDeleted; });
            inserted += r9.ins; updated += r9.upd; skipped += r9.skp;

            var r10 = await UpsertRange(_context.Photos, payload.Photos,
                (e, i) => { e.ReportId = i.ReportId; e.FilePath = i.FilePath;
                    e.Caption = i.Caption; e.RelatedActivity = i.RelatedActivity;
                    e.TakenAt = i.TakenAt; e.IsDeleted = i.IsDeleted; });
            inserted += r10.ins; updated += r10.upd; skipped += r10.skp;

            var r11 = await UpsertRange(_context.Signatures, payload.Signatures,
                (e, i) => { e.ReportId = i.ReportId; e.SignerName = i.SignerName;
                    e.Role = i.Role; e.SignedAt = i.SignedAt; e.IsSigned = i.IsSigned;
                    e.EmployeeId = i.EmployeeId; e.CheckInTime = i.CheckInTime;
                    e.CheckOutTime = i.CheckOutTime; e.BreakTime = i.BreakTime;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r11.ins; updated += r11.upd; skipped += r11.skp;

            var r12 = await UpsertRange(_context.ProjectMembers, payload.ProjectMembers,
                (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                    e.Role = i.Role; e.IsDeleted = i.IsDeleted; });
            inserted += r12.ins; updated += r12.upd; skipped += r12.skp;

            var r13 = await UpsertRange(_context.ReportEquipments, payload.ReportEquipments,
                (e, i) => { e.ReportId = i.ReportId; e.EquipmentId = i.EquipmentId;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r13.ins; updated += r13.upd; skipped += r13.skp;

            var r14 = await UpsertRange(_context.ReportCompanions, payload.ReportCompanions,
                (e, i) => { e.ReportId = i.ReportId; e.CompanionId = i.CompanionId;
                    e.IsDeleted = i.IsDeleted; });
            inserted += r14.ins; updated += r14.upd; skipped += r14.skp;

            await _context.SaveChangesAsync();

            return Ok(new { Inserted = inserted, Updated = updated, Skipped = skipped });
        }

        // ─────────────────────────────────────────────
        // Helper: Upsert com Last Write Wins
        // ─────────────────────────────────────────────
        private async Task<(int ins, int upd, int skp)> UpsertRange<T>(
            DbSet<T> dbSet,
            List<T> incoming,
            Action<T, T> applyChanges) where T : class, ILocalSyncEntity
        {
            int ins = 0, upd = 0, skp = 0;
            foreach (var item in incoming)
            {
                var existing = await dbSet.FindAsync(item.Id);
                if (existing is null)
                {
                    item.UpdatedAt = DateTime.UtcNow;
                    dbSet.Add(item);
                    ins++;
                }
                else if (item.UpdatedAt >= existing.UpdatedAt)
                {
                    applyChanges(existing, item);
                    existing.UpdatedAt = DateTime.UtcNow;
                    upd++;
                }
                else
                {
                    skp++;
                }
            }
            return (ins, upd, skp);
        }
    }

    // ─────────────────────────────────────────────
    // DTOs de Sync
    // ─────────────────────────────────────────────
    public class SyncPullPayload
    {
        public DateTime ServerTime { get; set; }
        public List<Project> Projects { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<Equipment> Equipments { get; set; } = new();
        public List<Companion> Companions { get; set; } = new();
        public List<Report> Reports { get; set; } = new();
        public List<WeatherDetail> WeatherDetails { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
        public List<Occurrence> Occurrences { get; set; } = new();
        public List<Material> Materials { get; set; } = new();
        public List<Photo> Photos { get; set; } = new();
        public List<Signature> Signatures { get; set; } = new();
        public List<ProjectMember> ProjectMembers { get; set; } = new();
        public List<ReportEquipment> ReportEquipments { get; set; } = new();
        public List<ReportCompanion> ReportCompanions { get; set; } = new();
    }

    public class SyncPushPayload
    {
        public List<Project> Projects { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<Equipment> Equipments { get; set; } = new();
        public List<Companion> Companions { get; set; } = new();
        public List<Report> Reports { get; set; } = new();
        public List<WeatherDetail> WeatherDetails { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
        public List<Occurrence> Occurrences { get; set; } = new();
        public List<Material> Materials { get; set; } = new();
        public List<Photo> Photos { get; set; } = new();
        public List<Signature> Signatures { get; set; } = new();
        public List<ProjectMember> ProjectMembers { get; set; } = new();
        public List<ReportEquipment> ReportEquipments { get; set; } = new();
        public List<ReportCompanion> ReportCompanions { get; set; } = new();
    }
}