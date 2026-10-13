using System.Net.Http.Json;

namespace CeeSharp.Client.Weather;

public sealed class WeatherForecastApiClient : IWeatherForecastReader
{
    private readonly HttpClient _http;

    public WeatherForecastApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        var forecasts = await _http.GetFromJsonAsync<List<WeatherForecast>>("api/weatherforecasts", cancellationToken);
        return forecasts ?? [];
    }
}
