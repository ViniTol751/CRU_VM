using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace RDO.Data.Data;

public static class DbContextHelper
{
    public static string GetDbPath()
    {
        var pasta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RDOApp");

        Directory.CreateDirectory(pasta);
        return Path.Combine(pasta, "rdo_local.db");
    }

    public static DbContextOptions<RdoDbContext> GetOptions()
    {
        var optionsBuilder = new DbContextOptionsBuilder<RdoDbContext>();
        optionsBuilder.UseSqlite($"Data Source={GetDbPath()}");
        return optionsBuilder.Options;
    }
}