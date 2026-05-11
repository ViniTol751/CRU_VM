using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RDO.Data.Data;

/// <summary>
/// Factory for PostgreSQL design-time operations (migrations, etc.)
/// Used when running 'dotnet ef' commands against the PostgreSQL database
/// </summary>
public class RdoDbContextFactory : IDesignTimeDbContextFactory<RdoDbContext>
{
    public RdoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RdoDbContext>();
        
        // Use SQLite if --sqlite flag is passed, otherwise use PostgreSQL
        if (args.Length > 0 && args[0] == "--sqlite")
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RDOApp", "rdo_local.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
        else
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=RDO_FOCUS;Username=postgres;Password=1234");
        }
        
        return new RdoDbContext(optionsBuilder.Options);
    }
}
