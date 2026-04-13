using Microsoft.EntityFrameworkCore;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RDO.app.Services;

public class SyncService
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string LastSyncKey = "lastSync";
    private static readonly string LogDirectoryPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "logs");
    private static readonly string LogFilePath = System.IO.Path.Combine(LogDirectoryPath, "sync-errors.log");
    private static readonly string StateFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "sync_state.json");

    public SyncService(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!IsNetworkAvailable())
        {
            WriteErrorLog("SYNC-OFFLINE", "Sem conectividade de rede detectada.");
            return SyncResult.Offline();
        }

        try
        {
            var since = LoadLastSyncTime();
            var pushResult = await PushAsync();
            var pullResult = await PullAsync(since);

            if (pushResult.Success && pullResult.Success)
                SaveLastSyncTime(pullResult.ServerTime);

            return new SyncResult
            {
                Success        = pushResult.Success && pullResult.Success,
                PushedInserted = pushResult.Inserted,
                PushedUpdated  = pushResult.Updated,
                PushedSkipped  = pushResult.Skipped,
                PulledRecords  = pullResult.TotalPulled,
                Error          = pushResult.Error ?? pullResult.Error,
                ErrorCode      = pushResult.ErrorCode ?? pullResult.ErrorCode
            };
        }
        catch (Exception ex)
        {
            WriteErrorLog("SYNC-UNEXPECTED", ex.Message, ex);
            return SyncResult.Failure("SYNC-UNEXPECTED", ex.Message);
        }
    }

    private async Task<PushResult> PushAsync()
    {

        using var db = new RdoDbContext(DbContextHelper.GetOptions());
        var since = LoadLastSyncTime();

        // IDs de relatórios não sincronizados
        var unsyncedReportIds = await db.Reports
            .Where(r => !r.IsSynced)
            .Select(r => r.Id)
            .ToListAsync();

        var payload = new SyncPushPayload
        {
            Projects         = await db.Projects.Where(x => x.UpdatedAt > since).ToListAsync(),
            Users            = await db.Users.Where(x => x.UpdatedAt > since).ToListAsync(),
            Employees        = await db.Employees.Where(x => x.UpdatedAt > since).ToListAsync(),
            Equipments       = await db.Equipments.Where(x => x.UpdatedAt > since).ToListAsync(),
            Companions       = await db.Companions.Where(x => x.UpdatedAt > since).ToListAsync(),
            Reports          = await db.Reports.Where(x => x.UpdatedAt > since || !x.IsSynced).ToListAsync(),
            WeatherDetails   = await db.WeatherDetails.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Activities       = await db.Activities.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Occurrences      = await db.Occurrences.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Materials        = await db.Materials.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Photos           = await db.Photos.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Signatures       = await db.Signatures.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            ProjectMembers   = await db.ProjectMembers.Where(x => x.UpdatedAt > since).ToListAsync(),
            ReportEquipments = await db.ReportEquipments.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            ReportCompanions = await db.ReportCompanions.Where(x => x.UpdatedAt > since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
        };

        System.Diagnostics.Debug.WriteLine("PUSH payload: " + payload.Reports.Count + " reports, unsyncedIds: " + string.Join(",", unsyncedReportIds));
        var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/api/sync/push", payload, _jsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var error = $"Push failed: {response.StatusCode}";
            WriteErrorLog("SYNC-PUSH-HTTP", error);
            return new PushResult { Success = false, ErrorCode = "SYNC-PUSH-HTTP", Error = error };
        }

        var result = await response.Content.ReadFromJsonAsync<ApiSyncPushResult>(_jsonOptions);

        // Marca IsSynced apenas nos relatórios que foram enviados
        var sentIds = payload.Reports.Select(r => r.Id).ToList();
        if (sentIds.Any())
            await db.Reports
                .Where(r => sentIds.Contains(r.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsSynced, true));

        return new PushResult
        {
            Success  = true,
            Inserted = result?.Inserted ?? 0,
            Updated  = result?.Updated ?? 0,
            Skipped  = result?.Skipped ?? 0
        };
    }

    private async Task<PullResult> PullAsync(DateTime since)
    {
        var sinceStr = since.ToString("o");
        var response = await _http.GetAsync($"{_apiBaseUrl}/api/sync/pull?since={Uri.EscapeDataString(sinceStr)}");
        if (!response.IsSuccessStatusCode)
        {
            var error = $"Pull failed: {response.StatusCode}";
            WriteErrorLog("SYNC-PULL-HTTP", error);
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-HTTP", Error = error };
        }

        var payload = await response.Content.ReadFromJsonAsync<SyncPullPayload>(_jsonOptions);
        if (payload is null)
        {
            WriteErrorLog("SYNC-PULL-EMPTY", "Empty response from server");
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-EMPTY", Error = "Empty response from server" };
        }

        using var db = new RdoDbContext(DbContextHelper.GetOptions());
        int total = 0;
        string _currentEntity = "Projects";
        try {

        total += await UpsertLocal(db.Projects, payload.Projects, db,
            (e, i) => { e.Name = i.Name; e.Address = i.Address; e.ART = i.ART;
                e.Group = i.Group; e.Status = i.Status; e.Manager = i.Manager;
                e.ContractType = i.ContractType; e.Client = i.Client;
                e.StartDate = i.StartDate; e.ExpectedEndDate = i.ExpectedEndDate;
                e.ImagePath = i.ImagePath; e.IsActive = i.IsActive;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Users";
        total += await UpsertLocal(db.Users, payload.Users, db,
            (e, i) => { e.Name = i.Name; e.Email = i.Email;
                e.PasswordHash = i.PasswordHash; e.Profile = i.Profile;
                e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Employees";
        total += await UpsertLocal(db.Employees, payload.Employees, db,
            (e, i) => { e.Name = i.Name; e.JobTitle = i.JobTitle;
                e.Company = i.Company; e.Type = i.Type; e.Contact = i.Contact;
                e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Equipments";
        total += await UpsertLocal(db.Equipments, payload.Equipments, db,
            (e, i) => { e.Name = i.Name; e.Manufacturer = i.Manufacturer;
                e.Model = i.Model; e.SerialNumber = i.SerialNumber;
                e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Companions";
        total += await UpsertLocal(db.Companions, payload.Companions, db,
            (e, i) => { e.Name = i.Name; e.Role = i.Role; e.Group = i.Group;
                e.Contact = i.Contact; e.IsActive = i.IsActive;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Reports";
        total += await UpsertLocal(db.Reports, payload.Reports, db,
            (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                e.CompanionId = i.CompanionId; e.Number = i.Number;
                e.Date = i.Date; e.CheckInTime = i.CheckInTime;
                e.CheckOutTime = i.CheckOutTime; e.BreakTime = i.BreakTime;
                e.GeneralNotes = i.GeneralNotes; e.Status = i.Status;
                e.IsDraft = i.IsDraft;
                e.IsDeleted = i.IsDeleted;
                e.IsSynced = true; });

        _currentEntity = "WeatherDetails";
        total += await UpsertLocal(db.WeatherDetails, payload.WeatherDetails, db,
            (e, i) => { e.ReportId = i.ReportId; e.Period = i.Period;
                e.IsActive = i.IsActive; e.Weather = i.Weather;
                e.Condition = i.Condition; e.RainfallIndex = i.RainfallIndex;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Activities";
        total += await UpsertLocal(db.Activities, payload.Activities, db,
            (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                e.Location = i.Location; e.Status = i.Status;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Occurrences";
        total += await UpsertLocal(db.Occurrences, payload.Occurrences, db,
            (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                e.Tags = i.Tags; e.StartTime = i.StartTime; e.EndTime = i.EndTime;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Materials";
        total += await UpsertLocal(db.Materials, payload.Materials, db,
            (e, i) => { e.ReportId = i.ReportId; e.Name = i.Name;
                e.Quantity = i.Quantity; e.Unit = i.Unit;
                e.Type = i.Type; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Photos";
        total += await UpsertLocal(db.Photos, payload.Photos, db,
            (e, i) => { e.ReportId = i.ReportId; e.FilePath = i.FilePath;
                e.Caption = i.Caption; e.RelatedActivity = i.RelatedActivity;
                e.TakenAt = i.TakenAt; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "Signatures";
        total += await UpsertLocal(db.Signatures, payload.Signatures, db,
            (e, i) => { e.ReportId = i.ReportId; e.SignerName = i.SignerName;
                e.Role = i.Role; e.SignedAt = i.SignedAt;
                e.IsSigned = i.IsSigned; e.EmployeeId = i.EmployeeId;
                e.CheckInTime = i.CheckInTime; e.CheckOutTime = i.CheckOutTime;
                e.BreakTime = i.BreakTime; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "ProjectMembers";
        total += await UpsertLocal(db.ProjectMembers, payload.ProjectMembers, db,
            (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                e.Role = i.Role; e.IsDeleted = i.IsDeleted; });

        _currentEntity = "ReportEquipments";
        total += await UpsertLocal(db.ReportEquipments, payload.ReportEquipments, db,
            (e, i) => { e.ReportId = i.ReportId; e.EquipmentId = i.EquipmentId;
                e.IsDeleted = i.IsDeleted; });

        _currentEntity = "ReportCompanions";
        total += await UpsertLocal(db.ReportCompanions, payload.ReportCompanions, db,
            (e, i) => { e.ReportId = i.ReportId; e.CompanionId = i.CompanionId;
                e.IsDeleted = i.IsDeleted; });

        await db.SaveChangesAsync();
        } catch (Exception ex) {
            var error = $"Erro em {_currentEntity}: {ex.Message}";
            WriteErrorLog("SYNC-PULL-UPSERT", error, ex);
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-UPSERT", Error = error };
        }
        var serverTime = payload.ServerTime == default
            ? DateTime.UtcNow
            : payload.ServerTime.ToUniversalTime();
        return new PullResult { Success = true, TotalPulled = total, ServerTime = serverTime };
    }

    private static async Task<int> UpsertLocal<T>(
        DbSet<T> dbSet,
        List<T> incoming,
        RdoDbContext db,
        Action<T, T> applyChanges) where T : class, ILocalSyncEntity
    {
        int count = 0;
        foreach (var item in incoming)
        {
            foreach (var nav in db.Entry(item).Navigations)
                nav.CurrentValue = null;
            var existing = await dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id);
            if (existing is null)
            {
                item.UpdatedAt = item.UpdatedAt == default ? DateTime.UtcNow : item.UpdatedAt;
                if (item is Report r) r.IsSynced = true;
                db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                count++;
            }
            else if (item.UpdatedAt.ToUniversalTime() >= existing.UpdatedAt.ToUniversalTime())
            {
                applyChanges(existing, item);
                existing.UpdatedAt = item.UpdatedAt;
                db.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                count++;
            }
        }
        return count;
    }

    public static bool IsNetworkAvailable() =>
        NetworkInterface.GetIsNetworkAvailable();

    private DateTime LoadLastSyncTime()
    {
        try
        {
            if (!File.Exists(StateFilePath)) return DateTime.MinValue;
            var json = File.ReadAllText(StateFilePath);
            var state = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (state is not null && state.TryGetValue(LastSyncKey, out var val))
                return DateTime.Parse(val, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        catch (Exception ex)
        {
            WriteErrorLog("SYNC-STATE-READ", "Falha ao ler estado de sincronização local.", ex);
        }
        return DateTime.MinValue;
    }

    private void SaveLastSyncTime(DateTime time)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(StateFilePath)!;
            Directory.CreateDirectory(dir);
            var state = new Dictionary<string, string> { [LastSyncKey] = time.ToString("o") };
            File.WriteAllText(StateFilePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            WriteErrorLog("SYNC-STATE-WRITE", "Falha ao salvar estado de sincronização local.", ex);
        }
    }

    private static void WriteErrorLog(string code, string message, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectoryPath);
            var line = $"[{DateTime.UtcNow:O}] code={code} message=\"{message}\"";
            if (ex is not null)
                line += $" exception=\"{ex.GetType().Name}: {ex.Message}\"";
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
        catch
        {
            // Último fallback: nunca interromper o app por falha de log.
        }
    }
}

public class SyncPullPayload
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
    public DateTime ServerTime { get; set; }
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

public class ApiSyncPushResult
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}

public class SyncResult
{
    public bool Success { get; set; }
    public bool IsOffline { get; set; }
    public int PushedInserted { get; set; }
    public int PushedUpdated { get; set; }
    public int PushedSkipped { get; set; }
    public int PulledRecords { get; set; }
    public string? ErrorCode { get; set; }
    public string? Error { get; set; }

    public static SyncResult Offline() => new() { Success = false, IsOffline = true, ErrorCode = "SYNC-OFFLINE" };
    public static SyncResult Failure(string errorCode, string error) =>
        new() { Success = false, ErrorCode = errorCode, Error = error };
}

internal class PushResult
{
    public bool Success { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public string? ErrorCode { get; set; }
    public string? Error { get; set; }
}

internal class PullResult
{
    public bool Success { get; set; }
    public int TotalPulled { get; set; }
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
    public string? ErrorCode { get; set; }
    public string? Error { get; set; }
}
