using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<SyncController> _logger;

    public SyncController(AppDbContext context, ILogger<SyncController> logger)
    {
        _context = context;
        _logger  = logger;
    }

    // ── PULL ─────────────────────────────────────────────────────────────────

    [HttpGet("pull")]
    public async Task<IActionResult> Pull([FromQuery] DateTime since, [FromQuery] string? table = null)
    {
        var sinceUtc = since.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(since, DateTimeKind.Utc)
            : since.ToUniversalTime();

        _logger.LogInformation("[Sync/Pull] since={Since:O} table={Table}", sinceUtc, table ?? "all");

        var payload = new SyncPullPayload { ServerTime = DateTime.UtcNow };

        var t = table?.ToLowerInvariant();
        bool all = t is null;

        if (all || t == "projects")
            payload.Projects         = await _context.Projects.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "users")
            payload.Users            = await _context.Users.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "employees")
            payload.Employees        = await _context.Employees.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "equipments")
            payload.Equipments       = await _context.Equipments.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "companions")
            payload.Companions       = await _context.Companions.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "reports")
            payload.Reports          = await _context.Reports.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "weatherdetails")
            payload.WeatherDetails   = await _context.WeatherDetails.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "activities")
            payload.Activities       = await _context.Activities.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "occurrences")
            payload.Occurrences      = await _context.Occurrences.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "materials")
            payload.Materials        = await _context.Materials.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "photos")
            payload.Photos           = await _context.Photos.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "signatures")
            payload.Signatures       = await _context.Signatures.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "projectmembers")
            payload.ProjectMembers   = await _context.ProjectMembers.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "reportequipments")
            payload.ReportEquipments = await _context.ReportEquipments.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "reportcompanions")
            payload.ReportCompanions = await _context.ReportCompanions.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();
        if (all || t == "empresas")
            payload.Empresas         = await _context.Empresas.Where(x => x.UpdatedAt >= sinceUtc).ToListAsync();

        return Ok(payload);
    }

    // ── PUSH ─────────────────────────────────────────────────────────────────

    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] SyncPushPayload payload)
    {
        _logger.LogInformation("[Sync/Push] {Count} report(s)", payload.Reports.Count);

        int inserted = 0, updated = 0, skipped = 0;

        void Acc((int I, int U, int S) r) { inserted += r.I; updated += r.U; skipped += r.S; }

        Acc(await UpsertAll(_context.Projects,         payload.Projects));
        Acc(await UpsertUsers(_context.Users,          payload.Users));
        Acc(await UpsertAll(_context.Employees,        payload.Employees));
        Acc(await UpsertAll(_context.Equipments,       payload.Equipments));
        Acc(await UpsertAll(_context.Companions,       payload.Companions));
        Acc(await UpsertAll(_context.Reports,          payload.Reports));
        Acc(await UpsertAll(_context.WeatherDetails,   payload.WeatherDetails));
        Acc(await UpsertAll(_context.Activities,       payload.Activities));
        Acc(await UpsertAll(_context.Occurrences,      payload.Occurrences));
        Acc(await UpsertAll(_context.Materials,        payload.Materials));
        Acc(await UpsertAll(_context.Photos,           payload.Photos));
        Acc(await UpsertAll(_context.Signatures,       payload.Signatures));
        Acc(await UpsertAll(_context.ProjectMembers,   payload.ProjectMembers));
        Acc(await UpsertAll(_context.ReportEquipments, payload.ReportEquipments));
        Acc(await UpsertAll(_context.ReportCompanions, payload.ReportCompanions));
        Acc(await UpsertAll(_context.Empresas,         payload.Empresas));

        await _context.SaveChangesAsync();

        _logger.LogInformation("[Sync/Push] inserted={I} updated={U} skipped={S}",
            inserted, updated, skipped);

        return Ok(new { Inserted = inserted, Updated = updated, Skipped = skipped });
    }

    // Users never arrive with a valid PasswordHash ([JsonIgnore] strips it from the push payload).
    // Inserting a passwordless phantom breaks FirstOrDefaultAsync email lookups in AuthController.
    // Rule: skip inserts entirely (auth endpoints own user creation); for updates, copy the
    // stored hash into the incoming entity so EF Core does not overwrite it with empty string.
    private async Task<(int I, int U, int S)> UpsertUsers(
        DbSet<User> dbSet, List<User> incoming)
    {
        if (incoming.Count == 0) return (0, 0, 0);

        var ids      = incoming.Select(x => x.Id).ToList();
        var existing = await dbSet.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        int ins = 0, upd = 0, skp = 0;
        var serverNow = DateTime.UtcNow;

        foreach (var item in incoming)
        {
            if (!existing.TryGetValue(item.Id, out var found))
            {
                // Skip: new users must be created via /api/auth/register.
                // Inserting here with PasswordHash="" would create a phantom record that
                // shadows the correctly-hashed user and breaks email-based auth lookups.
                skp++;
            }
            else if (item.UpdatedAt.ToUniversalTime() >= found.UpdatedAt.ToUniversalTime()
                     || (item.IsDeleted && !found.IsDeleted))
            {
                item.PasswordHash = found.PasswordHash; // preserve — not included in push payload
                item.UpdatedAt    = serverNow;
                _context.Entry(item).State = EntityState.Modified;
                upd++;
            }
            else
            {
                skp++;
            }
        }

        return (ins, upd, skp);
    }

    private async Task<(int I, int U, int S)> UpsertAll<T>(
        DbSet<T> dbSet, List<T> incoming)
        where T : class, ILocalSyncEntity
    {
        if (incoming.Count == 0) return (0, 0, 0);

        var ids = incoming.Select(x => x.Id).ToList();
        var existing = await dbSet.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        int ins = 0, upd = 0, skp = 0;

        var serverNow = DateTime.UtcNow;
        foreach (var item in incoming)
        {
            if (!existing.TryGetValue(item.Id, out var found))
            {
                item.UpdatedAt = serverNow;
                _context.Entry(item).State = EntityState.Added;
                ins++;
            }
            else if (item.UpdatedAt.ToUniversalTime() >= found.UpdatedAt.ToUniversalTime()
                     || (item.IsDeleted && !found.IsDeleted))
            {
                item.UpdatedAt = serverNow;
                _context.Entry(item).State = EntityState.Modified;
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

// ── Payloads ──────────────────────────────────────────────────────────────────

public class SyncPushPayload
{
    public List<Project>         Projects         { get; set; } = new();
    public List<User>            Users            { get; set; } = new();
    public List<Employee>        Employees        { get; set; } = new();
    public List<Equipment>       Equipments       { get; set; } = new();
    public List<Companion>       Companions       { get; set; } = new();
    public List<Report>          Reports          { get; set; } = new();
    public List<WeatherDetail>   WeatherDetails   { get; set; } = new();
    public List<Activity>        Activities       { get; set; } = new();
    public List<Occurrence>      Occurrences      { get; set; } = new();
    public List<Material>        Materials        { get; set; } = new();
    public List<Photo>           Photos           { get; set; } = new();
    public List<Signature>       Signatures       { get; set; } = new();
    public List<ProjectMember>   ProjectMembers   { get; set; } = new();
    public List<ReportEquipment> ReportEquipments { get; set; } = new();
    public List<ReportCompanion> ReportCompanions { get; set; } = new();
    public List<Empresa>         Empresas         { get; set; } = new();
}

public class SyncPullPayload
{
    public DateTime ServerTime { get; set; }

    public List<Project>         Projects         { get; set; } = new();
    public List<User>            Users            { get; set; } = new();
    public List<Employee>        Employees        { get; set; } = new();
    public List<Equipment>       Equipments       { get; set; } = new();
    public List<Companion>       Companions       { get; set; } = new();
    public List<Report>          Reports          { get; set; } = new();
    public List<WeatherDetail>   WeatherDetails   { get; set; } = new();
    public List<Activity>        Activities       { get; set; } = new();
    public List<Occurrence>      Occurrences      { get; set; } = new();
    public List<Material>        Materials        { get; set; } = new();
    public List<Photo>           Photos           { get; set; } = new();
    public List<Signature>       Signatures       { get; set; } = new();
    public List<ProjectMember>   ProjectMembers   { get; set; } = new();
    public List<ReportEquipment> ReportEquipments { get; set; } = new();
    public List<ReportCompanion> ReportCompanions { get; set; } = new();
    public List<Empresa>         Empresas         { get; set; } = new();
}
