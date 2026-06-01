using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmmzLive.Data;

/// <summary>
/// Used by EF Core tooling (dotnet ef migrations add) at design time.
/// The placeholder connection string lets the tool build the model without a live database.
/// </summary>
internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=emmz_design_time;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
