using System.Text.RegularExpressions;
using MetarReader.Models;

namespace MetarReader.Services;

public class MetarDecoder
{
    private static readonly Dictionary<string, string> WeatherCodes = new()
    {
        // Intensity
        { "-", "Light " },
        { "+", "Heavy " },
        { "VC", "In the vicinity: " },

        // Descriptor
        { "MI", "shallow " },
        { "PR", "partial " },
        { "BC", "patches of " },
        { "DR", "low drifting " },
        { "BL", "blowing " },
        { "SH", "showers of " },
        { "TS", "thunderstorm with " },
        { "FZ", "freezing " },

        // Precipitation
        { "DZ", "drizzle" },
        { "RA", "rain" },
        { "SN", "snow" },
        { "SG", "snow grains" },
        { "IC", "ice crystals" },
        { "PL", "ice pellets" },
        { "GR", "hail" },
        { "GS", "small hail" },
        { "UP", "unknown precipitation" },

        // Obscuration
        { "BR", "mist" },
        { "FG", "fog" },
        { "FU", "smoke" },
        { "VA", "volcanic ash" },
        { "DU", "dust" },
        { "SA", "sand" },
        { "HZ", "haze" },
        { "PY", "spray" },

        // Other
        { "PO", "dust/sand whirls" },
        { "SQ", "squalls" },
        { "FC", "funnel cloud/tornado" },
        { "SS", "sandstorm" },
        { "DS", "duststorm" }
    };

    private static readonly Dictionary<string, string> CloudCoverage = new()
    {
        { "SKC", "Clear skies" },
        { "CLR", "Clear skies" },
        { "NCD", "No clouds detected" },
        { "NSC", "No significant clouds" },
        { "FEW", "Few clouds" },
        { "SCT", "Scattered clouds" },
        { "BKN", "Broken clouds" },
        { "OVC", "Overcast" },
        { "VV", "Vertical visibility" }
    };

    public MetarData Decode(string rawMetar)
    {
        if (string.IsNullOrWhiteSpace(rawMetar))
        {
            return new MetarData { ErrorMessage = "No METAR data received" };
        }

        var parts = rawMetar.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Remove METAR/SPECI prefix if present
        if (parts.Count > 0 && (parts[0] == "METAR" || parts[0] == "SPECI"))
        {
            parts.RemoveAt(0);
        }

        // Parse airport code
        string airportCode = string.Empty;
        if (parts.Count > 0)
        {
            airportCode = parts[0];
            parts.RemoveAt(0);
        }

        // Parse observation time (format: DDHHMMz)
        DateTime? observationTime = null;
        if (parts.Count > 0 && Regex.IsMatch(parts[0], @"^\d{6}Z$"))
        {
            observationTime = ParseObservationTime(parts[0]);
            parts.RemoveAt(0);
        }

        // Parse wind (format: DDDssKT or DDDssGggKT or VRBssKT)
        string? windDirection = null;
        int? windSpeedKnots = null;
        int? gustSpeedKnots = null;
        if (parts.Count > 0)
        {
            var windMatch = Regex.Match(parts[0], @"^(VRB|\d{3})(\d{2,3})(G(\d{2,3}))?KT$");
            if (windMatch.Success)
            {
                windDirection = windMatch.Groups[1].Value;
                windSpeedKnots = int.Parse(windMatch.Groups[2].Value);
                if (windMatch.Groups[4].Success)
                {
                    gustSpeedKnots = int.Parse(windMatch.Groups[4].Value);
                }
                parts.RemoveAt(0);
            }
        }

        // Parse visibility
        string? visibility = null;
        if (parts.Count > 0)
        {
            var visMatch = Regex.Match(parts[0], @"^(\d+)SM$");
            if (visMatch.Success)
            {
                visibility = visMatch.Groups[1].Value + " statute miles";
                parts.RemoveAt(0);
            }
            else if (parts[0] == "CAVOK")
            {
                visibility = "Greater than 10 km (CAVOK)";
                parts.RemoveAt(0);
            }
            else if (parts.Count > 1 && Regex.IsMatch(parts[0], @"^\d+$") && Regex.IsMatch(parts[1], @"^\d/\d+SM$"))
            {
                visibility = parts[0] + " " + parts[1].Replace("SM", "") + " statute miles";
                parts.RemoveAt(0);
                parts.RemoveAt(0);
            }
            else if (Regex.IsMatch(parts[0], @"^\d/\d+SM$"))
            {
                visibility = parts[0].Replace("SM", "") + " statute miles";
                parts.RemoveAt(0);
            }
        }

        // Parse weather phenomena
        var weatherPhenomena = new List<string>();
        while (parts.Count > 0)
        {
            var weatherMatch = Regex.Match(parts[0], @"^([+-]|VC)?([A-Z]{2,})$");
            if (weatherMatch.Success && IsWeatherCode(parts[0]))
            {
                weatherPhenomena.Add(DecodeWeather(parts[0]));
                parts.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        // Parse cloud layers
        var cloudLayers = new List<CloudLayer>();
        while (parts.Count > 0)
        {
            var cloudMatch = Regex.Match(parts[0], @"^(SKC|CLR|NCD|NSC|FEW|SCT|BKN|OVC|VV)(\d{3})?");
            if (cloudMatch.Success)
            {
                var coverage = cloudMatch.Groups[1].Value;
                var altitude = cloudMatch.Groups[2].Success ? int.Parse(cloudMatch.Groups[2].Value) * 100 : 0;
                cloudLayers.Add(new CloudLayer(coverage, altitude));
                parts.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        // Parse temperature/dewpoint (format: TT/DD or MTT/MDD for negative)
        int? temperatureCelsius = null;
        int? dewPointCelsius = null;
        if (parts.Count > 0)
        {
            var tempMatch = Regex.Match(parts[0], @"^(M)?(\d{2})/(M)?(\d{2})$");
            if (tempMatch.Success)
            {
                var temp = int.Parse(tempMatch.Groups[2].Value);
                if (tempMatch.Groups[1].Value == "M") temp = -temp;
                temperatureCelsius = temp;

                var dewPoint = int.Parse(tempMatch.Groups[4].Value);
                if (tempMatch.Groups[3].Value == "M") dewPoint = -dewPoint;
                dewPointCelsius = dewPoint;

                parts.RemoveAt(0);
            }
        }

        // Parse altimeter (format: Axxxx)
        double? altimeterInHg = null;
        if (parts.Count > 0)
        {
            var altMatch = Regex.Match(parts[0], @"^A(\d{4})$");
            if (altMatch.Success)
            {
                altimeterInHg = int.Parse(altMatch.Groups[1].Value) / 100.0;
                parts.RemoveAt(0);
            }
        }

        // Build the record
        var data = new MetarData
        {
            RawMetar = rawMetar.Trim(),
            AirportCode = airportCode,
            ObservationTime = observationTime,
            WindDirection = windDirection,
            WindSpeedKnots = windSpeedKnots,
            GustSpeedKnots = gustSpeedKnots,
            Visibility = visibility,
            WeatherPhenomena = weatherPhenomena,
            CloudLayers = cloudLayers,
            TemperatureCelsius = temperatureCelsius,
            DewPointCelsius = dewPointCelsius,
            AltimeterInHg = altimeterInHg,
            HumanReadableSummary = GenerateSummary(
                cloudLayers, weatherPhenomena, temperatureCelsius,
                windSpeedKnots, windDirection, gustSpeedKnots,
                visibility, altimeterInHg)
        };

        return data;
    }

    private static DateTime? ParseObservationTime(string timeStr)
    {
        if (timeStr.Length < 6) return null;

        var day = int.Parse(timeStr.Substring(0, 2));
        var hour = int.Parse(timeStr.Substring(2, 2));
        var minute = int.Parse(timeStr.Substring(4, 2));

        var now = DateTime.UtcNow;
        var obsTime = new DateTime(now.Year, now.Month, day, hour, minute, 0, DateTimeKind.Utc);

        if (obsTime > now.AddDays(1))
        {
            obsTime = obsTime.AddMonths(-1);
        }

        return obsTime;
    }

    private bool IsWeatherCode(string code)
    {
        var cleanCode = code.TrimStart('-', '+');
        if (cleanCode.StartsWith("VC")) cleanCode = cleanCode.Substring(2);

        for (int i = 0; i < cleanCode.Length; i += 2)
        {
            if (i + 2 > cleanCode.Length) return false;
            var twoChar = cleanCode.Substring(i, 2);
            if (!WeatherCodes.ContainsKey(twoChar)) return false;
        }
        return cleanCode.Length > 0;
    }

    private string DecodeWeather(string code)
    {
        var result = "";
        var remaining = code;

        if (remaining.StartsWith("-"))
        {
            result += "Light ";
            remaining = remaining.Substring(1);
        }
        else if (remaining.StartsWith("+"))
        {
            result += "Heavy ";
            remaining = remaining.Substring(1);
        }

        if (remaining.StartsWith("VC"))
        {
            result += "In vicinity: ";
            remaining = remaining.Substring(2);
        }

        var descriptions = new List<string>();
        for (int i = 0; i < remaining.Length; i += 2)
        {
            if (i + 2 <= remaining.Length)
            {
                var twoChar = remaining.Substring(i, 2);
                if (WeatherCodes.TryGetValue(twoChar, out var desc))
                {
                    descriptions.Add(desc.Trim());
                }
            }
        }

        result += string.Join(" ", descriptions);
        return result.Trim();
    }

    private static string GenerateSummary(
        List<CloudLayer> cloudLayers,
        List<string> weatherPhenomena,
        int? temperatureCelsius,
        int? windSpeedKnots,
        string? windDirection,
        int? gustSpeedKnots,
        string? visibility,
        double? altimeterInHg)
    {
        var parts = new List<string>();

        // Sky conditions
        if (cloudLayers.Count > 0)
        {
            var primaryCloud = cloudLayers[0];
            var cloudDesc = CloudCoverage.GetValueOrDefault(primaryCloud.Coverage, primaryCloud.Coverage);

            if (primaryCloud.AltitudeFeet > 0)
            {
                parts.Add($"{cloudDesc} at {primaryCloud.AltitudeFeet:N0} feet");
            }
            else
            {
                parts.Add(cloudDesc);
            }
        }
        else
        {
            parts.Add("Clear skies");
        }

        // Weather phenomena
        if (weatherPhenomena.Count > 0)
        {
            parts.Add(string.Join(", ", weatherPhenomena).ToLower());
        }

        // Temperature
        if (temperatureCelsius.HasValue)
        {
            var fahrenheit = CelsiusToFahrenheit(temperatureCelsius.Value);
            parts.Add($"Temperature {fahrenheit}°F ({temperatureCelsius}°C)");
        }

        // Wind
        if (windSpeedKnots.HasValue)
        {
            var windMph = KnotsToMph(windSpeedKnots.Value);
            var windDesc = "";

            if (windSpeedKnots == 0)
            {
                windDesc = "Calm winds";
            }
            else if (windDirection == "VRB")
            {
                windDesc = $"Variable winds at {windMph} mph";
            }
            else if (int.TryParse(windDirection, out var degrees))
            {
                var direction = GetCardinalDirection(degrees);
                windDesc = $"Wind from the {direction} at {windMph} mph";

                if (gustSpeedKnots.HasValue)
                {
                    var gustMph = KnotsToMph(gustSpeedKnots.Value);
                    windDesc += $", gusting to {gustMph} mph";
                }
            }

            parts.Add(windDesc);
        }

        // Visibility
        if (!string.IsNullOrEmpty(visibility))
        {
            parts.Add($"Visibility {visibility}");
        }

        // Altimeter
        if (altimeterInHg.HasValue)
        {
            parts.Add($"Altimeter {altimeterInHg:F2}\" Hg");
        }

        return string.Join(". ", parts) + ".";
    }

    private static int CelsiusToFahrenheit(int celsius)
    {
        return (int)Math.Round(celsius * 9.0 / 5.0 + 32);
    }

    private static int KnotsToMph(int knots)
    {
        return (int)Math.Round(knots * 1.15078);
    }

    private static string GetCardinalDirection(int degrees)
    {
        degrees = ((degrees % 360) + 360) % 360;

        if (degrees >= 337.5 || degrees < 22.5) return "north";
        if (degrees >= 22.5 && degrees < 67.5) return "northeast";
        if (degrees >= 67.5 && degrees < 112.5) return "east";
        if (degrees >= 112.5 && degrees < 157.5) return "southeast";
        if (degrees >= 157.5 && degrees < 202.5) return "south";
        if (degrees >= 202.5 && degrees < 247.5) return "southwest";
        if (degrees >= 247.5 && degrees < 292.5) return "west";
        return "northwest";
    }
}
