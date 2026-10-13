using CeeSharp.Client.Weather;
using Microsoft.EntityFrameworkCore;

namespace CeeSharp.Data;

public sealed class WeatherForecastReader : IWeatherForecastReader
{
    private readonly AppDbContext _db;

    public WeatherForecastReader(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.WeatherForecasts
            .AsNoTracking()
            .OrderBy(forecast => forecast.Date)
            .Select(forecast => new WeatherForecast
            {
                Date = forecast.Date,
                TemperatureC = forecast.TemperatureC,
                Summary = forecast.Summary
            })
            .ToListAsync(cancellationToken);
    }
}
