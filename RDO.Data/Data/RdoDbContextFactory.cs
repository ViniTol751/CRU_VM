using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RDO.Data.Data;

public class RdoDbContextFactory : IDesignTimeDbContextFactory<RdoDbContext>
{
    public RdoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RdoDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=RDO_FOCUS;Username=postgres;Password=1234");
        return new RdoDbContext(optionsBuilder.Options);
    }
}
