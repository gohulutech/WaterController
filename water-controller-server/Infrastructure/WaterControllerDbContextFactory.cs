using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure;

/// <summary>
/// Design-time factory so `dotnet ef` can create the DbContext without
/// running the web application.
/// </summary>
public sealed class WaterControllerDbContextFactory
    : IDesignTimeDbContextFactory<WaterControllerDbContext>
{
    public WaterControllerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WaterControllerDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=water_controller;Username=postgres;Password=postgres")
            .Options;

        return new WaterControllerDbContext(options);
    }
}
