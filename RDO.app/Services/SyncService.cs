using Microsoft.EntityFrameworkCore;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            SyncLogger.LogOffline(_apiBaseUrl);
            return SyncResult.Offline();
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var since = LoadLastSyncTime();
            var pushResult = await PushAsync();
            var pullResult = await PullAsync(since);
            sw.Stop();

            if (pushResult.Success && pullResult.Success)
            {
                SaveLastSyncTime(pullResult.ServerTime);
                SyncLogger.LogSuccess(_apiBaseUrl,
                    pushResult.Inserted + pushResult.Updated,
                    pullResult.TotalPulled,
                    sw.Elapsed.TotalMilliseconds);
            }

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
            sw.Stop();
            var (userMsg, errorType, diagnosis) = ClassifyException(ex);
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Sync (Push + Pull)",
                ErrorCode       = "SYNC-UNEXPECTED",
                ErrorType       = errorType,
                ApiUrl          = _apiBaseUrl,
                DurationMs      = sw.Elapsed.TotalMilliseconds,
                UserMessage     = userMsg,
                TechnicalDetail = ex.Message,
                StackTrace      = ex.StackTrace ?? "",
                Diagnosis       = diagnosis
            });
            return SyncResult.Failure("SYNC-UNEXPECTED", userMsg);
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

        var swPush = System.Diagnostics.Stopwatch.StartNew();
        HttpResponseMessage pushResponse;
        try
        {
            pushResponse = await _http.PostAsJsonAsync($"{_apiBaseUrl}/api/sync/push", payload, _jsonOptions);
        }
        catch (Exception ex)
        {
            swPush.Stop();
            var (userMsg, errorType, diagnosis) = ClassifyException(ex);
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Push",
                ErrorCode       = "SYNC-PUSH-CONN",
                ErrorType       = errorType,
                ApiUrl          = $"{_apiBaseUrl}/api/sync/push",
                DurationMs      = swPush.Elapsed.TotalMilliseconds,
                UserMessage     = userMsg,
                TechnicalDetail = ex.Message,
                StackTrace      = ex.StackTrace ?? "",
                Diagnosis       = diagnosis
            });
            return new PushResult { Success = false, ErrorCode = "SYNC-PUSH-CONN", Error = userMsg };
        }
        swPush.Stop();

        if (!pushResponse.IsSuccessStatusCode)
        {
            var (userMsg, diagnosis) = ClassifyHttpError(pushResponse.StatusCode, "Push");
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Push",
                ErrorCode       = "SYNC-PUSH-HTTP",
                ErrorType       = "HTTP Error",
                HttpStatusCode  = pushResponse.StatusCode,
                ApiUrl          = $"{_apiBaseUrl}/api/sync/push",
                DurationMs      = swPush.Elapsed.TotalMilliseconds,
                UserMessage     = userMsg,
                TechnicalDetail = $"HTTP {(int)pushResponse.StatusCode} {pushResponse.StatusCode}",
                Diagnosis       = diagnosis
            });
            return new PushResult { Success = false, ErrorCode = "SYNC-PUSH-HTTP", Error = userMsg };
        }
        var response = pushResponse;

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
        var pullUrl  = $"{_apiBaseUrl}/api/sync/pull?since={Uri.EscapeDataString(sinceStr)}";

        var swPull = System.Diagnostics.Stopwatch.StartNew();
        HttpResponseMessage pullResponse;
        try
        {
            pullResponse = await _http.GetAsync(pullUrl);
        }
        catch (Exception ex)
        {
            swPull.Stop();
            var (userMsg, errorType, diagnosis) = ClassifyException(ex);
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Pull",
                ErrorCode       = "SYNC-PULL-CONN",
                ErrorType       = errorType,
                ApiUrl          = pullUrl,
                DurationMs      = swPull.Elapsed.TotalMilliseconds,
                UserMessage     = userMsg,
                TechnicalDetail = ex.Message,
                StackTrace      = ex.StackTrace ?? "",
                Diagnosis       = diagnosis
            });
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-CONN", Error = userMsg };
        }
        swPull.Stop();

        if (!pullResponse.IsSuccessStatusCode)
        {
            var (userMsg, diagnosis) = ClassifyHttpError(pullResponse.StatusCode, "Pull");
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Pull",
                ErrorCode       = "SYNC-PULL-HTTP",
                ErrorType       = "HTTP Error",
                HttpStatusCode  = pullResponse.StatusCode,
                ApiUrl          = pullUrl,
                DurationMs      = swPull.Elapsed.TotalMilliseconds,
                UserMessage     = userMsg,
                TechnicalDetail = $"HTTP {(int)pullResponse.StatusCode} {pullResponse.StatusCode}",
                Diagnosis       = diagnosis
            });
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-HTTP", Error = userMsg };
        }

        var payload = await pullResponse.Content.ReadFromJsonAsync<SyncPullPayload>(_jsonOptions);
        if (payload is null)
        {
            const string emptyMsg  = "Resposta vazia do servidor";
            const string emptyDiag = "• A API retornou um corpo vazio — verifique os logs da API\n" +
                                     "• Confirme que a versão da API é compatível com o aplicativo";
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Pull",
                ErrorCode       = "SYNC-PULL-EMPTY",
                ErrorType       = "Resposta Vazia",
                ApiUrl          = pullUrl,
                DurationMs      = swPull.Elapsed.TotalMilliseconds,
                UserMessage     = emptyMsg,
                TechnicalDetail = "Content-Length: 0 ou corpo não serializável",
                Diagnosis       = emptyDiag
            });
            return new PullResult { Success = false, ErrorCode = "SYNC-PULL-EMPTY", Error = emptyMsg };
        }

        using var db = new RdoDbContext(DbContextHelper.GetOptions());
        int total = 0;
        string _currentEntity = "Projects";
        try {

        total += await UpsertLocal(db.Projects, payload.Projects, db,
            (e, i) => { e.Name = i.Name; e.Address = i.Address; e.ART = i.ART;
                e.Group = i.Group; e.Status = i.Status; e.Manager = i.Manager;
                e.ClientManager = i.ClientManager;
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

        _currentEntity = "SaveChangesAsync";
        await db.SaveChangesAsync();
        } catch (Exception ex) {
            // Constrói cadeia completa de mensagens para diagnóstico
            var innerChain = new System.Text.StringBuilder();
            var inner = ex.InnerException;
            while (inner != null)
            {
                innerChain.Append(" → ").Append(inner.Message);
                inner = inner.InnerException;
            }
            var fullMessage = ex.Message + (innerChain.Length > 0 ? innerChain.ToString() : "");
            var error = $"Erro ao salvar {_currentEntity} localmente: {fullMessage}";
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = $"Pull — Upsert {_currentEntity}",
                ErrorCode       = "SYNC-PULL-UPSERT",
                ErrorType       = ex.GetType().Name,
                ApiUrl          = pullUrl,
                UserMessage     = error,
                TechnicalDetail = fullMessage,
                StackTrace      = ex.StackTrace ?? "",
                Diagnosis       = $"• Erro ao persistir entidade '{_currentEntity}' no banco local\n" +
                                  $"• Verifique se as migrations do SQLite estão atualizadas\n" +
                                  $"• Logs em: {SyncLogger.GetLogDirectory()}"
            });
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
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation = "LoadLastSyncTime", ErrorCode = "SYNC-STATE-READ",
                ErrorType = ex.GetType().Name, ApiUrl = _apiBaseUrl,
                UserMessage = "Falha ao ler estado de sincronização local.",
                TechnicalDetail = ex.Message, StackTrace = ex.StackTrace ?? "",
                Diagnosis = $"• Verifique permissões em: {StateFilePath}"
            });
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
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation = "SaveLastSyncTime", ErrorCode = "SYNC-STATE-WRITE",
                ErrorType = ex.GetType().Name, ApiUrl = _apiBaseUrl,
                UserMessage = "Falha ao salvar estado de sincronização local.",
                TechnicalDetail = ex.Message, StackTrace = ex.StackTrace ?? "",
                Diagnosis = $"• Verifique permissões em: {StateFilePath}"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Classificação de exceções de rede
    // ─────────────────────────────────────────────────────────────────
    private (string userMessage, string errorType, string diagnosis) ClassifyException(Exception ex)
    {
        if (ex is TaskCanceledException or TimeoutException)
            return (
                "A API não respondeu — timeout de 30 s atingido",
                "Timeout",
                $"• Verifique se a API está em execução: dotnet run\n" +
                $"• Verifique se o Docker/PostgreSQL está ativo\n" +
                $"• Confirme a URL configurada: {_apiBaseUrl}\n" +
                $"• Logs em: {SyncLogger.GetLogDirectory()}"
            );

        if (ex is HttpRequestException httpEx)
        {
            if (IsConnectionRefused(httpEx))
                return (
                    "API offline — conexão recusada",
                    "Conexão Recusada (ECONNREFUSED)",
                    $"• Inicie a API: cd TesteAPI && dotnet run\n" +
                    $"• Verifique se o Docker está rodando: docker ps\n" +
                    $"• Confirme que o PostgreSQL está ativo: docker ps | grep postgres\n" +
                    $"• Confirme a porta na URL: {_apiBaseUrl}\n" +
                    $"• Teste no navegador: {_apiBaseUrl}/swagger"
                );

            if (IsHostNotFound(httpEx))
                return (
                    "Host não encontrado — verifique a URL da API",
                    "Host Não Encontrado (DNS)",
                    $"• Confirme a URL configurada: {_apiBaseUrl}\n" +
                    $"• Verifique a conectividade de rede"
                );

            return (
                "Falha na comunicação com a API",
                "Erro de Rede (HttpRequestException)",
                $"• Verifique se a API está acessível: {_apiBaseUrl}\n" +
                $"• Detalhe: {httpEx.Message}"
            );
        }

        return (
            "Erro inesperado durante a sincronização",
            ex.GetType().Name,
            $"• Verifique os logs em: {SyncLogger.GetLogDirectory()}\n" +
            $"• Detalhe: {ex.Message}"
        );
    }

    private static bool IsConnectionRefused(HttpRequestException ex)
    {
        if (ex.InnerException is SocketException se &&
            (se.SocketErrorCode == SocketError.ConnectionRefused ||
             se.SocketErrorCode == SocketError.TimedOut))
            return true;
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return msg.Contains("connection refused") || msg.Contains("actively refused") ||
               msg.Contains("no connection could be made") || msg.Contains("econnrefused");
    }

    private static bool IsHostNotFound(HttpRequestException ex)
    {
        if (ex.InnerException is SocketException se &&
            se.SocketErrorCode == SocketError.HostNotFound) return true;
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return msg.Contains("no such host") || msg.Contains("name or service not known");
    }

    // ─────────────────────────────────────────────────────────────────
    // Classificação de erros HTTP
    // ─────────────────────────────────────────────────────────────────
    private (string userMessage, string diagnosis) ClassifyHttpError(HttpStatusCode code, string operation)
    {
        return code switch
        {
            HttpStatusCode.InternalServerError => (
                $"Erro interno na API (500) — {operation} falhou",
                $"• Erro 500 geralmente indica problema no banco de dados PostgreSQL\n" +
                $"• Verifique se o container PostgreSQL está saudável: docker ps\n" +
                $"• Consulte os logs da API para o detalhe da exceção\n" +
                $"• Execute as migrations se necessário: dotnet ef database update"
            ),
            HttpStatusCode.ServiceUnavailable => (
                $"Serviço indisponível (503) — {operation} falhou",
                $"• A API está sobrecarregada ou em manutenção\n" +
                $"• Verifique se o banco PostgreSQL está acessível\n" +
                $"• Aguarde e tente novamente"
            ),
            HttpStatusCode.BadGateway => (
                $"Erro de gateway (502) — {operation} falhou",
                $"• Problema entre serviços Docker\n" +
                $"• Verifique se todos os containers estão rodando: docker ps\n" +
                $"• Reinicie se necessário: docker-compose restart"
            ),
            HttpStatusCode.GatewayTimeout => (
                $"Timeout no gateway (504) — {operation} falhou",
                $"• O banco de dados PostgreSQL pode estar lento ou travado\n" +
                $"• Verifique os logs do PostgreSQL"
            ),
            HttpStatusCode.NotFound => (
                $"Endpoint não encontrado (404) — {operation} falhou",
                $"• A URL da API pode estar incorreta: {_apiBaseUrl}\n" +
                $"• Verifique se a versão da API é compatível com o aplicativo"
            ),
            _ => (
                $"Falha HTTP {(int)code} {code} — {operation} falhou",
                $"• Verifique os logs da API para mais informações"
            )
        };
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
