namespace MetarReader.Models;

public record MetarData
{
    public string RawMetar { get; init; } = string.Empty;
    public string AirportCode { get; init; } = string.Empty;
    public DateTime? ObservationTime { get; init; }
    public string? WindDirection { get; init; }
    public int? WindSpeedKnots { get; init; }
    public int? GustSpeedKnots { get; init; }
    public string? Visibility { get; init; }
    public List<string> WeatherPhenomena { get; init; } = [];
    public List<CloudLayer> CloudLayers { get; init; } = [];
    public int? TemperatureCelsius { get; init; }
    public int? DewPointCelsius { get; init; }
    public double? AltimeterInHg { get; init; }
    public string HumanReadableSummary { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
}
