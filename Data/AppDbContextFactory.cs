using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QLNS.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection.");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
            .Options;
        return new AppDbContext(options);
    }
}
