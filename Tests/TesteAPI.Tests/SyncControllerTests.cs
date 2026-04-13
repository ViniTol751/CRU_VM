using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TesteAPI.Data;
using TesteAPI.Models;

namespace TesteAPI.Tests;

public class SyncControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SyncControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
