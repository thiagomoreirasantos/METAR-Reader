using MetarReader.Models;

namespace MetarReader.Services;

public class MetarService : IMetarService
{
    private readonly HttpClient _httpClient;
    private readonly MetarDecoder _decoder;
    private readonly ILogger<MetarService> _logger;

    public MetarService(HttpClient httpClient, MetarDecoder decoder, ILogger<MetarService> logger)
    {
        _httpClient = httpClient;
        _decoder = decoder;
        _logger = logger;
    }

    public async Task<MetarData> GetMetarAsync(string airportCode)
    {
        if (string.IsNullOrWhiteSpace(airportCode))
        {
            return new MetarData
            {
                ErrorMessage = "Please enter an airport code"
            };
        }

        // Clean up airport code
        airportCode = airportCode.Trim().ToUpperInvariant();

        // Validate format (typically 4 characters, starting with K for US airports)
        if (airportCode.Length < 3 || airportCode.Length > 4)
        {
            return new MetarData
            {
                AirportCode = airportCode,
                ErrorMessage = "Airport code should be 3-4 characters (e.g., KJFK, LAX)"
            };
        }

        try
        {
            var url = $"https://aviationweather.gov/api/data/metar?ids={airportCode}";
            _logger.LogInformation("Fetching METAR from: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var rawMetar = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(rawMetar))
            {
                return new MetarData
                {
                    AirportCode = airportCode,
                    ErrorMessage = $"No METAR data found for airport code '{airportCode}'. Please verify the code is correct."
                };
            }

            _logger.LogInformation("Received METAR: {Metar}", rawMetar);

            var metarData = _decoder.Decode(rawMetar);
            return metarData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching METAR for {AirportCode}", airportCode);
            return new MetarData
            {
                AirportCode = airportCode,
                ErrorMessage = "Unable to connect to the weather service. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching METAR for {AirportCode}", airportCode);
            return new MetarData
            {
                AirportCode = airportCode,
                ErrorMessage = "An unexpected error occurred. Please try again."
            };
        }
    }
}
