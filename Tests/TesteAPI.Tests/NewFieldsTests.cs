using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Tests;

public class NewFieldsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NewFieldsTests(CustomWebApplicationFactory factory)
        => _factory = factory;

    private HttpClient CreateClient() => _factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task SeedProjectAsync(int id = 1, string crea = "", string clientManager = "")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Projects.Add(new Project
        {
            Id = id, Name = "Obra Seed", Address = "Rua Seed", ART = "ART-0",
            Group = "G", Status = "In Progress", Manager = "M",
            ContractType = "C", Client = "Cl",
            Crea = crea, ClientManager = clientManager,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true, IsDeleted = false
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedUserAsync(int id = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(new User
        {
            Id = id, Name = "Seed User", Email = "seed@test.com",
            PasswordHash = "hash", Profile = "Technician",
            UpdatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true, IsDeleted = false
        });
        await db.SaveChangesAsync();
    }

    // ── Project: Crea + ClientManager ────────────────────────────────────────

    [Fact]
    public async Task Push_ShouldPersist_ProjectCrea_And_ClientManager()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var payload = new
        {
            Projects = new[]
            {
                new
                {
                    Id = 10, Name = "Obra Crea", Address = "Rua A", ART = "ART-010",
                    Group = "G1", Status = "In Progress", Manager = "M1",
                    Crea = "CREA-SP-123456", ClientManager = "João Silva",
                    ContractType = "Empreitada", Client = "Cliente A",
                    StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpectedEndDate = (DateTime?)null, ImagePath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 10, 12, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Projects.FindAsync(10);

        Assert.NotNull(saved);
        Assert.Equal("CREA-SP-123456", saved!.Crea);
        Assert.Equal("João Silva", saved.ClientManager);
    }

    [Fact]
    public async Task Pull_ShouldReturn_ProjectCrea_And_ClientManager()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync(id: 1, crea: "CREA-RJ-999", clientManager: "Maria Souza");

        var response = await CreateClient()
            .GetAsync($"/api/sync/pull?since={Uri.EscapeDataString("2026-01-01T00:00:00")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Projects);
        Assert.Equal("CREA-RJ-999", payload.Projects[0].Crea);
        Assert.Equal("Maria Souza", payload.Projects[0].ClientManager);
    }

    // ── Report: Revisao ──────────────────────────────────────────────────────

    [Fact]
    public async Task Push_ShouldPersist_ReportRevisao()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync();
        await SeedUserAsync();
        var client = CreateClient();

        var payload = new
        {
            Reports = new[]
            {
                new
                {
                    Id = 50, ProjectId = 1, UserId = 1, CompanionId = (int?)null,
                    Number = 1,
                    Date = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                    CheckInTime = "08:00", CheckOutTime = "17:00", BreakTime = "01:00",
                    GeneralNotes = "Nota", Status = "Filling",
                    IsSynced = false, IsDraft = false,
                    Revisao = 3,
                    CreatedAt = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Reports.FindAsync(50);

        Assert.NotNull(saved);
        Assert.Equal(3, saved!.Revisao);
    }

    [Fact]
    public async Task Pull_ShouldReturn_ReportRevisao()
    {
        await _factory.ResetDatabaseAsync();
        await SeedProjectAsync();
        await SeedUserAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Reports.Add(new Report
            {
                Id = 51, ProjectId = 1, UserId = 1, Number = 2,
                Date = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                Revisao = 7,
                UpdatedAt = new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var response = await CreateClient()
            .GetAsync($"/api/sync/pull?since={Uri.EscapeDataString("2026-01-01T00:00:00")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Reports);
        Assert.Equal(7, payload.Reports[0].Revisao);
    }

    // ── Companion: EmpresaId ─────────────────────────────────────────────────

    [Fact]
    public async Task Push_ShouldPersist_CompanionEmpresaId()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var payload = new
        {
            Companions = new[]
            {
                new
                {
                    Id = 20, Name = "Carlos", Role = "Fiscal", Group = "G",
                    Contact = "99999-0000",
                    EmpresaId = (int?)5,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Companions.FindAsync(20);

        Assert.NotNull(saved);
        Assert.Equal(5, saved!.EmpresaId);
    }

    [Fact]
    public async Task Pull_ShouldReturn_CompanionEmpresaId()
    {
        await _factory.ResetDatabaseAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Companions.Add(new Companion
            {
                Id = 21, Name = "Ana", Role = "Eng.", Group = "G", Contact = "",
                EmpresaId = 9,
                UpdatedAt = new DateTime(2026, 4, 15, 11, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var response = await CreateClient()
            .GetAsync($"/api/sync/pull?since={Uri.EscapeDataString("2026-01-01T00:00:00")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Companions);
        Assert.Equal(9, payload.Companions[0].EmpresaId);
    }

    // ── Empresa: CRUD via sync ────────────────────────────────────────────────

    [Fact]
    public async Task Push_ShouldInsert_Empresa()
    {
        await _factory.ResetDatabaseAsync();
        var client = CreateClient();

        var payload = new
        {
            Empresas = new[]
            {
                new
                {
                    Id = 1, Nome = "Construtora Alpha", ImagemPath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Inserted);
        Assert.Equal(0, result.Updated);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.Empresas.FindAsync(1);

        Assert.NotNull(saved);
        Assert.Equal("Construtora Alpha", saved!.Nome);
    }

    [Fact]
    public async Task Push_ShouldUpdate_Empresa_WhenIncomingIsNewer()
    {
        await _factory.ResetDatabaseAsync();

        // Seed one day earlier so the 24h gap beats any ±12h timezone offset
        // (SQLite returns DateTime as Unspecified; ToUniversalTime() treats it as local)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Empresas.Add(new Empresa
            {
                Id = 2, Nome = "Empresa Antiga",
                UpdatedAt = new DateTime(2026, 4, 19, 12, 0, 0, DateTimeKind.Utc),
                IsActive = true, IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var payload = new
        {
            Empresas = new[]
            {
                new
                {
                    Id = 2, Nome = "Empresa Atualizada", ImagemPath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await CreateClient().PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.Equal(1, result!.Updated);
        Assert.Equal(0, result.Skipped);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db2.Empresas.FindAsync(2);
        Assert.Equal("Empresa Atualizada", saved!.Nome);
    }

    [Fact]
    public async Task Push_ShouldSkip_Empresa_WhenIncomingIsOlder()
    {
        await _factory.ResetDatabaseAsync();

        // Seed one day later so incoming (older) is beaten by any ±12h timezone offset
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Empresas.Add(new Empresa
            {
                Id = 3, Nome = "Empresa Atual",
                UpdatedAt = new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true, IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var payload = new
        {
            Empresas = new[]
            {
                new
                {
                    Id = 3, Nome = "Empresa Velha", ImagemPath = (string?)null,
                    IsActive = true,
                    UpdatedAt = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            }
        };

        var response = await CreateClient().PostAsJsonAsync("/api/sync/push", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApiSyncPushResult>();
        Assert.Equal(0, result!.Updated);
        Assert.Equal(1, result.Skipped);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db2.Empresas.FindAsync(3);
        Assert.Equal("Empresa Atual", saved!.Nome);
    }

    [Fact]
    public async Task Pull_ShouldReturn_Empresa()
    {
        await _factory.ResetDatabaseAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Empresas.Add(new Empresa
            {
                Id = 4, Nome = "Empresa Pull Test",
                UpdatedAt = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc),
                IsActive = true, IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var response = await CreateClient()
            .GetAsync($"/api/sync/pull?since={Uri.EscapeDataString("2026-01-01T00:00:00")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();

        Assert.NotNull(payload);
        Assert.Single(payload!.Empresas);
        Assert.Equal("Empresa Pull Test", payload.Empresas[0].Nome);
        Assert.Equal(4, payload.Empresas[0].Id);
    }

    [Fact]
    public async Task Pull_ShouldNotReturn_Empresa_WhenBeforeSince()
    {
        await _factory.ResetDatabaseAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Empresas.Add(new Empresa
            {
                Id = 5, Nome = "Empresa Antiga",
                UpdatedAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
                IsActive = true, IsDeleted = false
            });
            await db.SaveChangesAsync();
        }

        var response = await CreateClient()
            .GetAsync($"/api/sync/pull?since={Uri.EscapeDataString("2026-04-01T00:00:00")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiSyncPullResult>();

        Assert.NotNull(payload);
        Assert.Empty(payload!.Empresas);
    }
}
