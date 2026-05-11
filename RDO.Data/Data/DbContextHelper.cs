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
        var connectionString = $"Data Source={GetDbPath()};Mode=ReadWriteCreate;Cache=Shared;";
        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.CommandTimeout(30);
        });
        return optionsBuilder.Options;
    }
}