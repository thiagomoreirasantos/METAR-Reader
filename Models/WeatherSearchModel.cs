namespace MetarReader.Models;

public record WeatherSearchModel
{
    public string? AirportCode { get; init; }
    public MetarData? Result { get; init; }
}
