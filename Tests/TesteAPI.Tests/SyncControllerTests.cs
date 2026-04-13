using System.Net;
using System.Text;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TesteAPI.Data;
using TesteAPI.Models;
using Xunit.Abstractions;

namespace TesteAPI.Tests;

public class SyncControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public SyncControllerTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Pull_ShouldReturnData_WhenSinceIsUnspecifiedDateTime()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync(updatedAtUtc: new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc));

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var sinceUnspecified = "2026-04-13T11:30:00";
        var response = await client.GetAsync($"/api/sync/pull?since={Uri.EscapeDataString(sinceUnspecified)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Projects);
        Assert.Equal("Obra QA", payload.Projects[0].Name);
    }

    [Fact]
    public async Task Push_ShouldSkipOlderUpdate_ForExistingProject()
    {
        await _factory.ResetDatabaseAsync();

        var existingUpdatedAt = new DateTime(2026, 4, 13, 14, 0, 0, DateTimeKind.Utc);
        await SeedProjectAsync(existingUpdatedAt);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var olderPayload = new
        {
            Projects = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Nome Antigo",
                    Address = "Rua A",
                    ART = "ART-001",
                    Group = "Grupo 1",
                    Status = "In Progress",
                    Manager = "Gestor 1",
                    ContractType = "Contrato",
                    Client = "Cliente",
                    StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpectedEndDate = (DateTime?)null,
                    ImagePath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 13, 13, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var pushResponse = await client.PostAsJsonAsync("/api/sync/push", olderPayload);
        Assert.Equal(HttpStatusCode.OK, pushResponse.StatusCode);

        var result = await pushResponse.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.Updated);
        Assert.Equal(1, result.Skipped);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Projects.FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal("Obra QA", saved!.Name);
        Assert.Equal(existingUpdatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Push_ShouldInsertNewProject_WhenProjectDoesNotExist()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = new
        {
            Projects = new[]
            {
                new
                {
                    Id = 99,
                    Name = "Obra Inserida",
                    Address = "Rua Nova",
                    ART = "ART-099",
                    Group = "Grupo Insercao",
                    Status = "In Progress",
                    Manager = "Gestor Novo",
                    ContractType = "Empreitada",
                    Client = "Cliente Novo",
                    StartDate = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    ExpectedEndDate = (DateTime?)null,
                    ImagePath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 13, 15, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var pushResponse = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, pushResponse.StatusCode);

        var result = await pushResponse.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Inserted);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Projects.FindAsync(99);
        Assert.NotNull(saved);
        Assert.Equal("Obra Inserida", saved!.Name);
    }

    [Fact]
    public async Task Pull_ShouldReturnEmptyProjects_WhenSinceIsInFuture()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync(updatedAtUtc: new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc));

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var futureSince = "2099-01-01T00:00:00";
        var response = await client.GetAsync($"/api/sync/pull?since={Uri.EscapeDataString(futureSince)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Projects);
    }

    [Fact]
    public async Task Push_ShouldUpdateProject_WhenIncomingIsNewer()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync(updatedAtUtc: new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc));

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = new
        {
            Projects = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Obra QA Atualizada",
                    Address = "Rua QA",
                    ART = "ART-001",
                    Group = "Grupo QA",
                    Status = "In Progress",
                    Manager = "Gestor QA",
                    ContractType = "Empreitada",
                    Client = "Cliente QA",
                    StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpectedEndDate = (DateTime?)null,
                    ImagePath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 13, 16, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var pushResponse = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, pushResponse.StatusCode);

        var result = await pushResponse.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.Inserted);
        Assert.Equal(1, result.Updated);
        Assert.Equal(0, result.Skipped);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Projects.FindAsync(1);
        Assert.NotNull(saved);
        Assert.Equal("Obra QA Atualizada", saved!.Name);
        Assert.Equal(new DateTime(2026, 4, 13, 16, 0, 0, DateTimeKind.Utc), saved.UpdatedAt);
    }

    [Fact]
    public async Task Pull_ShouldReturnBadRequest_WhenSinceHasInvalidFormat()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/sync/pull?since=data-invalida");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        LogSimulatedError(
            "SYNC-PULL-400",
            "Formato de data inválido em 'since'.",
            "Revise o relógio/dispositivo e tente novamente.",
            "Parâmetro 'since' deve estar em ISO-8601, ex: 2026-04-13T10:30:00Z.");
    }

    [Fact]
    public async Task Push_ShouldReturnBadRequest_WhenBodyHasInvalidJson()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var malformedJson = "{ \"projects\": [ { \"id\": 1, \"name\": \"Obra\" } ";
        using var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/sync/push", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        LogSimulatedError(
            "SYNC-PUSH-400",
            "Payload JSON inválido.",
            "Atualize o aplicativo e repita a sincronização.",
            "A API não conseguiu desserializar o corpo da requisição.");
    }

    [Fact]
    public async Task Push_ShouldReturnServerError_WhenPayloadViolatesForeignKey()
    {
        await _factory.ResetDatabaseAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var invalidReportPayload = new
        {
            Reports = new[]
            {
                new
                {
                    Id = 700,
                    ProjectId = 99999,
                    UserId = 99999,
                    CompanionId = (int?)null,
                    Number = 1,
                    Date = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    CheckInTime = "08:00",
                    CheckOutTime = "17:00",
                    BreakTime = "01:00",
                    GeneralNotes = "Teste erro FK",
                    Status = "Filling",
                    IsSynced = false,
                    CreatedAt = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    IsDraft = false,
                    UpdatedAt = new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await client.PostAsJsonAsync("/api/sync/push", invalidReportPayload));

        LogSimulatedError(
            "SYNC-PUSH-500",
            "Falha interna ao persistir payload (violação de relacionamento).",
            "Enviar relatório para suporte com horário da ocorrência.",
            "Possível FK inválida em Report.ProjectId/UserId.");
    }

    private void LogSimulatedError(string code, string summary, string userAction, string technicalHint)
    {
        _output.WriteLine(
            "[SIMULATED-ERROR] code={0} summary=\"{1}\" action=\"{2}\" hint=\"{3}\"",
            code, summary, userAction, technicalHint);
    }

    private async Task SeedProjectAsync(DateTime updatedAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Projects.Add(new Project
        {
            Id = 1,
            Name = "Obra QA",
            Address = "Rua QA",
            ART = "ART-001",
            Group = "Grupo QA",
            Status = "In Progress",
            Manager = "Gestor QA",
            ContractType = "Empreitada",
            Client = "Cliente QA",
            StartDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAtUtc,
            IsActive = true,
            IsDeleted = false
        });

        await db.SaveChangesAsync();
    }
}

public sealed class ApiSyncPushResult
{
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
}

public sealed class ApiSyncPullResult
{
    public List<Project> Projects { get; set; } = new();
}
