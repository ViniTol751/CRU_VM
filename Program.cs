using Microsoft.EntityFrameworkCore;
using TesteAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = 
    System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("AppDbConnectionString");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Necessário para compatibilidade de DateTime com o banco existente
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Logging já vem configurado por padrão no WebApplication —
// o ILogger<SyncController> é injetado automaticamente, sem nada extra aqui.

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }