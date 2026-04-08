using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RDO.Data.Data;

public class RdoDbContextFactory : IDesignTimeDbContextFactory<RdoDbContext>
{
    public RdoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RdoDbContext>();
        optionsBuilder.UseSqlite($"Data Source={DbContextHelper.GetDbPath()}");
        return new RdoDbContext(optionsBuilder.Options);
    }
}