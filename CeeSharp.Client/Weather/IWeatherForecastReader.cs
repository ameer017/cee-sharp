namespace CeeSharp.Client.Weather;

public interface IWeatherForecastReader
{
    Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default);
}
