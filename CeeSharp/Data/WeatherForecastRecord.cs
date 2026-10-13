namespace CeeSharp.Data;

public sealed class WeatherForecastRecord
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public string? Summary { get; set; }
}
