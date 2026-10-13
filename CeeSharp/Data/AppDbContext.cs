using Microsoft.EntityFrameworkCore;

namespace CeeSharp.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherForecastRecord> WeatherForecasts => Set<WeatherForecastRecord>();
}
