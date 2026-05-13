using System.Text;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TesteAPI.Data;
using TesteAPI.Services;

// Garante cultura invariante para evitar CultureNotFoundException em ambientes sem ICU completo
CultureInfo.DefaultThreadCurrentCulture   = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("AppDbConnectionString")
    ?? throw new InvalidOperationException(
        "Connection string não configurada. " +
        "Defina a variável de ambiente ConnectionStrings__AppDbConnectionString.");

builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "JWT Secret não configurado. Defina a variável de ambiente Jwt__Secret.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer   = false,
            ValidateAudience = false,
            ClockSkew        = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: máx 10 tentativas/minuto por IP
    options.AddFixedWindowLimiter("login", o =>
    {
        o.Window            = TimeSpan.FromMinutes(1);
        o.PermitLimit       = 10;
        o.QueueLimit        = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // API geral: 1000 req/min por IP
    options.AddFixedWindowLimiter("api", o =>
    {
        o.Window            = TimeSpan.FromMinutes(1);
        o.PermitLimit       = 1000;
        o.QueueLimit        = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ── CORS (uso interno) ────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── Health Check ──────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler(err => err.Run(async ctx =>
{
    ctx.Response.StatusCode  = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { message = "Erro interno no servidor." });
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").RequireRateLimiting("api");

app.Run();

public partial class Program { }
