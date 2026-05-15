using Microsoft.EntityFrameworkCore;
using RDO.App.Services;
using RDO.Data.Data;
using RDO.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string LastSyncKey = "lastSync";
    private static readonly string StateFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RDOApp", "sync_state.json");

    public SyncService(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };
    }

    // Autentica na API e devolve false se as credenciais falharem
    private async Task<bool> AuthenticateAsync()
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            return true;

        var email    = UserSession.Email;
        var password = UserSession.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Auth",
                ErrorCode       = "SYNC-AUTH-NOCRED",
                ErrorType       = "Sem credenciais",
                ApiUrl          = $"{_apiBaseUrl}/api/auth/login",
                UserMessage     = "Usuário não identificado para sincronização.",
                TechnicalDetail = "UserSession.Email está vazio — faça login novamente.",
                Diagnosis       = "• Feche o app e faça login novamente."
            });
            return false;
        }

        try
        {
            var resp = await _http.PostAsJsonAsync(
                $"{_apiBaseUrl}/api/auth/login",
                new { Email = email, Password = password },
                _jsonOptions);

            if (!resp.IsSuccessStatusCode)
            {
                // Conta criada offline? Tenta registrar no servidor e logar novamente.
                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var name = string.IsNullOrEmpty(UserSession.Name) ? email : UserSession.Name;
                    var regResp = await _http.PostAsJsonAsync(
                        $"{_apiBaseUrl}/api/auth/register",
                        new { Name = name, Email = email, Password = password },
                        _jsonOptions);
                    // 200 = registrado agora; 409 = já existia — tenta login mesmo assim
                    if (regResp.IsSuccessStatusCode ||
                        regResp.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        resp = await _http.PostAsJsonAsync(
                            $"{_apiBaseUrl}/api/auth/login",
                            new { Email = email, Password = password },
                            _jsonOptions);
                    }
                }

                if (!resp.IsSuccessStatusCode)
                {
                    SyncLogger.LogError(new SyncLogEntry
                    {
                        Operation       = "Auth",
                        ErrorCode       = "SYNC-AUTH-401",
                        ErrorType       = "Credenciais inválidas",
                        HttpStatusCode  = resp.StatusCode,
                        ApiUrl          = $"{_apiBaseUrl}/api/auth/login",
                        UserMessage     = "Falha de autenticação na API.",
                        TechnicalDetail = $"HTTP {(int)resp.StatusCode}",
                        Diagnosis       = "• Confirme se o usuário existe e está ativo no servidor."
                    });
                    return false;
                }
            }

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            _cachedToken = body.GetProperty("token").GetString();
            _tokenExpiry = DateTime.UtcNow.AddHours(11); // 1h de margem antes de expirar
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _cachedToken);
            return true;
        }
        catch (Exception ex)
        {
            SyncLogger.LogError(new SyncLogEntry
            {
                Operation       = "Auth",
                ErrorCode       = "SYNC-AUTH-ERR",
                ErrorType       = ex.GetType().Name,
                ApiUrl          = $"{_apiBaseUrl}/api/auth/login",
                UserMessage     = "Erro ao autenticar na API.",
                TechnicalDetail = ex.Message,
                StackTrace      = ex.StackTrace ?? "",
                Diagnosis       = "• Verifique se a API está acessível na rede."
            });
            return false;
        }
    }

    public async Task<SyncResult> SyncAsync()
    {
        if (!IsNetworkAvailable())
        {
            SyncLogger.LogOffline(_apiBaseUrl);
            return SyncResult.Offline();
        }

        if (!await AuthenticateAsync())
        {
            // #region agent log
            DebugAgentLog.Write("H1", "SyncService.cs:SyncAsync", "AuthenticateAsync returned false",
                new { apiBase = _apiBaseUrl, hasEmail = !string.IsNullOrEmpty(UserSession.Email) });
            // #endregion
            return SyncResult.Failure("SYNC-AUTH-FAIL", "Falha de autenticação. Faça login novamente.");
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

                // Copia logos locais para a pasta do NAS
                var logosCfg = RDO.App.Services.LogosConfig.Load();
                if (logosCfg.IsConfigured)
                    _ = RDO.App.Services.LogoService.UploadPendingLogosAsync(logosCfg);
            }

            var combined = new SyncResult
            {
                Success        = pushResult.Success && pullResult.Success,
                PushedInserted = pushResult.Inserted,
                PushedUpdated  = pushResult.Updated,
                PushedSkipped  = pushResult.Skipped,
                PulledRecords  = pullResult.TotalPulled,
                Error          = pushResult.Error ?? pullResult.Error,
                ErrorCode      = pushResult.ErrorCode ?? pullResult.ErrorCode
            };
            // #region agent log
            if (!combined.Success)
                DebugAgentLog.Write("H4", "SyncService.cs:SyncAsync", "push/pull reported failure",
                    new
                    {
                        pushOk = pushResult.Success,
                        pullOk = pullResult.Success,
                        combined.ErrorCode,
                        pushCode = pushResult.ErrorCode,
                        pullCode = pullResult.ErrorCode
                    });
            // #endregion
            return combined;
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
            // #region agent log
            DebugAgentLog.Write("H3", "SyncService.cs:SyncAsync", "outer catch SYNC-UNEXPECTED",
                new { exType = ex.GetType().Name, exMessage = ex.Message });
            // #endregion
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
            Projects         = await db.Projects.Where(x => x.UpdatedAt >= since || !x.IsActive || x.IsDeleted).ToListAsync(),
            Users            = await db.Users.Where(x => x.UpdatedAt >= since).ToListAsync(),
            Employees        = await db.Employees.Where(x => x.UpdatedAt >= since || !x.IsActive || x.IsDeleted).ToListAsync(),
            Equipments       = await db.Equipments.Where(x => x.UpdatedAt >= since || !x.IsActive || x.IsDeleted).ToListAsync(),
            Companions       = await db.Companions.Where(x => x.UpdatedAt >= since || !x.IsActive || x.IsDeleted).ToListAsync(),
            Reports          = await db.Reports.Where(x => x.UpdatedAt >= since || !x.IsSynced || x.IsDeleted).ToListAsync(),
            WeatherDetails   = await db.WeatherDetails.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Activities       = await db.Activities.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Occurrences      = await db.Occurrences.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Materials        = await db.Materials.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Photos           = await db.Photos.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Signatures       = await db.Signatures.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            ProjectMembers   = await db.ProjectMembers.Where(x => x.UpdatedAt >= since).ToListAsync(),
            ReportEquipments = await db.ReportEquipments.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            ReportCompanions = await db.ReportCompanions.Where(x => x.UpdatedAt >= since || unsyncedReportIds.Contains(x.ReportId)).ToListAsync(),
            Empresas         = await db.Empresas.Where(x => x.UpdatedAt >= since || !x.IsActive || x.IsDeleted).ToListAsync(),
        };

        System.Diagnostics.Debug.WriteLine("PUSH payload: " + payload.Reports.Count + " reports, unsyncedIds: " + string.Join(",", unsyncedReportIds));
        SyncLogger.LogDebug($"[PUSH] since={since:O} | projects={payload.Projects.Count} reports={payload.Reports.Count}");
        foreach (var p in payload.Projects.Where(p => !p.IsActive || p.IsDeleted))
            SyncLogger.LogDebug($"[PUSH] project #{p.Id} isActive={p.IsActive} isDeleted={p.IsDeleted} updatedAt={p.UpdatedAt:O}");
        foreach (var r in payload.Reports.Where(r => r.IsDeleted))
            SyncLogger.LogDebug($"[PUSH] report #{r.Id} isDeleted={r.IsDeleted} isSynced={r.IsSynced} updatedAt={r.UpdatedAt:O}");

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
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.IsSynced, true)
                    .SetProperty(r => r.Sincronizado, true));

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
        var tables   = new[]
        {
            "projects", "users", "employees", "equipments", "companions", "reports",
            "weatherdetails", "activities", "occurrences", "materials", "photos",
            "signatures", "projectmembers", "reportequipments", "reportcompanions", "empresas"
        };

        using var db = new RdoDbContext(DbContextHelper.GetOptions());
        // FK enforcement é por-conexão. Desabilita durante o pull porque o servidor pode omitir
        // companions/projects soft-deleted que reports ainda referenciam, causando SQLITE_CONSTRAINT_FOREIGNKEY.
        // A ordem de pull (projects→users→companions→reports) já garante consistência para dados novos.
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");
        int      total         = 0;
        DateTime serverTime    = DateTime.UtcNow;
        bool     gotServerTime = false;

        var updatedReportIds    = new HashSet<int>();
        var inPayloadActivities = new HashSet<int>();
        var inPayloadWeather    = new HashSet<int>();
        var inPayloadOccurr     = new HashSet<int>();
        var inPayloadSignatures = new HashSet<int>();
        var inPayloadMaterials  = new HashSet<int>();
        var inPayloadPhotos     = new HashSet<int>();
        var inPayloadRepEquip   = new HashSet<int>();
        var inPayloadRepComp    = new HashSet<int>();

        foreach (var table in tables)
        {
            var pullUrl = $"{_apiBaseUrl}/api/sync/pull?since={Uri.EscapeDataString(sinceStr)}&table={table}";

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
                    Operation       = $"Pull/{table}",
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
                    Operation       = $"Pull/{table}",
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
                    Operation       = $"Pull/{table}",
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

            if (!gotServerTime && payload.ServerTime != default)
            {
                serverTime    = payload.ServerTime.ToUniversalTime();
                gotServerTime = true;
            }

            if (table == "projects")
            {
                SyncLogger.LogDebug($"[PULL] projects received={payload.Projects.Count} since={sinceStr}");
                foreach (var p in payload.Projects.Where(p => !p.IsActive || p.IsDeleted))
                    SyncLogger.LogDebug($"[PULL] project #{p.Id} isActive={p.IsActive} isDeleted={p.IsDeleted} updatedAt={p.UpdatedAt:O}");
            }
            if (table == "reports")
            {
                SyncLogger.LogDebug($"[PULL] reports received={payload.Reports.Count}");
                foreach (var r in payload.Reports.Where(r => r.IsDeleted))
                    SyncLogger.LogDebug($"[PULL] report #{r.Id} isDeleted={r.IsDeleted} updatedAt={r.UpdatedAt:O}");
            }

            if (table == "reports")          updatedReportIds.UnionWith(payload.Reports.Select(r => r.Id));
            if (table == "activities")       inPayloadActivities.UnionWith(payload.Activities.Select(a => a.Id));
            if (table == "weatherdetails")   inPayloadWeather.UnionWith(payload.WeatherDetails.Select(w => w.Id));
            if (table == "occurrences")      inPayloadOccurr.UnionWith(payload.Occurrences.Select(o => o.Id));
            if (table == "signatures")       inPayloadSignatures.UnionWith(payload.Signatures.Select(s => s.Id));
            if (table == "materials")        inPayloadMaterials.UnionWith(payload.Materials.Select(m => m.Id));
            if (table == "photos")           inPayloadPhotos.UnionWith(payload.Photos.Select(p => p.Id));
            if (table == "reportequipments") inPayloadRepEquip.UnionWith(payload.ReportEquipments.Select(re => re.Id));
            if (table == "reportcompanions") inPayloadRepComp.UnionWith(payload.ReportCompanions.Select(rc => rc.Id));

            try
            {
                total += table switch
                {
                    "projects" => await UpsertLocal(db.Projects, payload.Projects, db,
                        (e, i) => { e.Name = i.Name; e.Address = i.Address; e.ART = i.ART;
                            e.Group = i.Group; e.Status = i.Status; e.Manager = i.Manager;
                            e.ClientManager = i.ClientManager;
                            e.ContractType = i.ContractType; e.Client = i.Client;
                            e.StartDate = i.StartDate; e.ExpectedEndDate = i.ExpectedEndDate;
                            e.ImagePath = i.ImagePath; e.IsActive = i.IsActive;
                            e.IsDeleted = i.IsDeleted; }),

                    "users" => await UpsertLocal(db.Users, payload.Users, db,
                        (e, i) => { e.Name = i.Name; e.Email = i.Email;
                            // Server omits PasswordHash ([JsonIgnore]). Never overwrite a valid
                            // local hash with empty string — that would break local login.
                            if (!string.IsNullOrEmpty(i.PasswordHash)) e.PasswordHash = i.PasswordHash;
                            e.Profile = i.Profile;
                            e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; }),

                    "employees" => await UpsertLocal(db.Employees, payload.Employees, db,
                        (e, i) => { e.Name = i.Name; e.JobTitle = i.JobTitle;
                            e.Company = i.Company; e.Type = i.Type; e.Contact = i.Contact;
                            e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; }),

                    "equipments" => await UpsertLocal(db.Equipments, payload.Equipments, db,
                        (e, i) => { e.Name = i.Name; e.Manufacturer = i.Manufacturer;
                            e.Model = i.Model; e.SerialNumber = i.SerialNumber;
                            e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; }),

                    "companions" => await UpsertLocal(db.Companions, payload.Companions, db,
                        (e, i) => { e.Name = i.Name; e.Role = i.Role; e.Group = i.Group;
                            e.Contact = i.Contact; e.IsActive = i.IsActive;
                            e.IsDeleted = i.IsDeleted; }),

                    "reports" => await UpsertLocal(db.Reports, payload.Reports, db,
                        (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                            e.CompanionId = i.CompanionId; e.Number = i.Number;
                            e.Date = i.Date; e.CheckInTime = i.CheckInTime;
                            e.CheckOutTime = i.CheckOutTime; e.BreakTime = i.BreakTime;
                            e.GeneralNotes = i.GeneralNotes; e.Status = i.Status;
                            e.IsDraft = i.IsDraft; e.IsDeleted = i.IsDeleted;
                            e.Revisao = i.Revisao;
                            e.IsSynced = true; }),

                    "weatherdetails" => await UpsertLocal(db.WeatherDetails, payload.WeatherDetails, db,
                        (e, i) => { e.ReportId = i.ReportId; e.Period = i.Period;
                            e.IsActive = i.IsActive; e.Weather = i.Weather;
                            e.Condition = i.Condition; e.RainfallIndex = i.RainfallIndex;
                            e.IsDeleted = i.IsDeleted; }),

                    "activities" => await UpsertLocal(db.Activities, payload.Activities, db,
                        (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                            e.Location = i.Location; e.Status = i.Status;
                            e.IsDeleted = i.IsDeleted; }),

                    "occurrences" => await UpsertLocal(db.Occurrences, payload.Occurrences, db,
                        (e, i) => { e.ReportId = i.ReportId; e.Description = i.Description;
                            e.Tags = i.Tags; e.StartTime = i.StartTime; e.EndTime = i.EndTime;
                            e.IsDeleted = i.IsDeleted; }),

                    "materials" => await UpsertLocal(db.Materials, payload.Materials, db,
                        (e, i) => { e.ReportId = i.ReportId; e.Name = i.Name;
                            e.Quantity = i.Quantity; e.Unit = i.Unit;
                            e.Type = i.Type; e.IsDeleted = i.IsDeleted; }),

                    "photos" => await UpsertLocal(db.Photos, payload.Photos, db,
                        (e, i) => { e.ReportId = i.ReportId; e.FilePath = i.FilePath;
                            e.Caption = i.Caption; e.RelatedActivity = i.RelatedActivity;
                            e.TakenAt = i.TakenAt; e.IsDeleted = i.IsDeleted; }),

                    "signatures" => await UpsertLocal(db.Signatures, payload.Signatures, db,
                        (e, i) => { e.ReportId = i.ReportId; e.SignerName = i.SignerName;
                            e.Role = i.Role; e.SignedAt = i.SignedAt;
                            e.IsSigned = i.IsSigned; e.EmployeeId = i.EmployeeId;
                            e.CheckInTime = i.CheckInTime; e.CheckOutTime = i.CheckOutTime;
                            e.BreakTime = i.BreakTime; e.IsDeleted = i.IsDeleted; }),

                    "projectmembers" => await UpsertLocal(db.ProjectMembers, payload.ProjectMembers, db,
                        (e, i) => { e.ProjectId = i.ProjectId; e.UserId = i.UserId;
                            e.Role = i.Role; e.IsDeleted = i.IsDeleted; }),

                    "reportequipments" => await UpsertLocal(db.ReportEquipments, payload.ReportEquipments, db,
                        (e, i) => { e.ReportId = i.ReportId; e.EquipmentId = i.EquipmentId;
                            e.IsDeleted = i.IsDeleted; }),

                    "reportcompanions" => await UpsertLocal(db.ReportCompanions, payload.ReportCompanions, db,
                        (e, i) => { e.ReportId = i.ReportId; e.CompanionId = i.CompanionId;
                            e.IsDeleted = i.IsDeleted; }),

                    "empresas" => await UpsertLocal(db.Empresas, payload.Empresas, db,
                        (e, i) => { e.Nome = i.Nome; e.ImagemPath = i.ImagemPath;
                            e.IsActive = i.IsActive; e.IsDeleted = i.IsDeleted; }),

                    _ => 0
                };

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var innerChain = new System.Text.StringBuilder();
                var inner = ex.InnerException;
                while (inner != null) { innerChain.Append(" → ").Append(inner.Message); inner = inner.InnerException; }
                var fullMessage = ex.Message + (innerChain.Length > 0 ? innerChain.ToString() : "");
                var error = $"Erro ao salvar {table} localmente: {fullMessage}";
                SyncLogger.LogError(new SyncLogEntry
                {
                    Operation       = $"Pull — Upsert {table}",
                    ErrorCode       = "SYNC-PULL-UPSERT",
                    ErrorType       = ex.GetType().Name,
                    ApiUrl          = pullUrl,
                    UserMessage     = error,
                    TechnicalDetail = fullMessage,
                    StackTrace      = ex.StackTrace ?? "",
                    Diagnosis       = $"• Erro ao persistir entidade '{table}' no banco local\n" +
                                      $"• Verifique se as migrations do SQLite estão atualizadas\n" +
                                      $"• Logs em: {SyncLogger.GetLogDirectory()}"
                });
                return new PullResult { Success = false, ErrorCode = "SYNC-PULL-UPSERT", Error = error };
            }
        }

        // Remove entidades filhas de relatórios revisados que não estão no payload
        // (foram hard-deleted localmente ao criar nova revisão, mas o servidor ainda as tem)
        if (updatedReportIds.Count > 0)
        {
            var staleAct = await db.Activities.Where(a => updatedReportIds.Contains(a.ReportId) && !inPayloadActivities.Contains(a.Id)).ToListAsync();
            var staleWth = await db.WeatherDetails.Where(w => updatedReportIds.Contains(w.ReportId) && !inPayloadWeather.Contains(w.Id)).ToListAsync();
            var staleOcc = await db.Occurrences.Where(o => updatedReportIds.Contains(o.ReportId) && !inPayloadOccurr.Contains(o.Id)).ToListAsync();
            var staleSig = await db.Signatures.Where(s => updatedReportIds.Contains(s.ReportId) && !inPayloadSignatures.Contains(s.Id)).ToListAsync();
            var staleMat = await db.Materials.Where(m => updatedReportIds.Contains(m.ReportId) && !inPayloadMaterials.Contains(m.Id)).ToListAsync();
            var stalePho = await db.Photos.Where(p => updatedReportIds.Contains(p.ReportId) && !inPayloadPhotos.Contains(p.Id)).ToListAsync();
            var staleReq = await db.ReportEquipments.Where(re => updatedReportIds.Contains(re.ReportId) && !inPayloadRepEquip.Contains(re.Id)).ToListAsync();
            var staleRcp = await db.ReportCompanions.Where(rc => updatedReportIds.Contains(rc.ReportId) && !inPayloadRepComp.Contains(rc.Id)).ToListAsync();
            db.Activities.RemoveRange(staleAct);
            db.WeatherDetails.RemoveRange(staleWth);
            db.Occurrences.RemoveRange(staleOcc);
            db.Signatures.RemoveRange(staleSig);
            db.Materials.RemoveRange(staleMat);
            db.Photos.RemoveRange(stalePho);
            db.ReportEquipments.RemoveRange(staleReq);
            db.ReportCompanions.RemoveRange(staleRcp);
            var staleCount = staleAct.Count + staleWth.Count + staleOcc.Count + staleSig.Count +
                             staleMat.Count + stalePho.Count + staleReq.Count + staleRcp.Count;
            if (staleCount > 0)
                await db.SaveChangesAsync();
        }

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
            else if (item.UpdatedAt.ToUniversalTime() >= existing.UpdatedAt.ToUniversalTime()
                     || (item.IsDeleted && !existing.IsDeleted))
            {
                applyChanges(existing, item);
                existing.UpdatedAt = item.UpdatedAt;
                db.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                count++;
            }
        }
        return count;
    }

    // Retorna um timestamp garantidamente > lastSync para usar em UpdatedAt ao deletar/editar.
    // Evita que o relógio da máquina (ligeiramente atrás do servidor) faça o registro
    // não entrar no filtro `UpdatedAt >= since` do push.
    public static DateTime GetPushTimestamp()
    {
        var lastSync = LoadLastSyncTime();
        var now      = DateTime.UtcNow;
        return lastSync == DateTime.MinValue ? now
            : new DateTime(Math.Max(now.Ticks, lastSync.AddSeconds(1).Ticks), DateTimeKind.Utc);
    }

    // Apaga todos os dados locais e reseta o timestamp de sync.
    // Na próxima chamada a SyncAsync(), um pull completo traz tudo do servidor.
    public static async Task ResetLocalDataAsync()
    {
        await Task.Run(() =>
        {
            using var db = new RdoDbContext(DbContextHelper.GetOptions());
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");
            // Filhos primeiro (dependem de FK), depois pais
            foreach (var table in new[]
            {
                "ReportCompanion", "ReportEquipment", "EmployeePresence",
                "Signature", "Photo", "Material", "Occurrence", "Activity", "WeatherDetail",
                "Report", "ProjectMember",
                "Companion", "Equipment", "Employee", "User", "Project", "Empresas"
            })
                db.Database.ExecuteSqlRaw($"DELETE FROM \"{table}\"");
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
        });

        // Reseta o timestamp — próximo pull traz tudo desde o início
        if (File.Exists(StateFilePath)) File.Delete(StateFilePath);
    }

    public static bool IsNetworkAvailable() =>
        NetworkInterface.GetIsNetworkAvailable();

    public static DateTime LoadLastSyncTime()
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
                ErrorType = ex.GetType().Name,
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
    public List<Empresa>         Empresas         { get; set; } = new();
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
    public List<Empresa>         Empresas         { get; set; } = new();
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
