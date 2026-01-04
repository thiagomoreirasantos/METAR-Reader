# METAR Reader

A web application that fetches aviation weather reports (METAR) and translates them into plain, human-readable English.

## What is METAR?

METAR (Meteorological Aerodrome Report) is a standardized format for reporting weather observations at airports. While essential for pilots and aviation professionals, the format is cryptic and difficult for the average person to understand.

**Example METAR:**
```
KJFK 041856Z 31015G23KT 10SM FEW045 BKN250 M02/M17 A3042
```

**Decoded by this app:**
> Few clouds at 4,500 feet. Temperature 28°F (-2°C). Wind from the northwest at 17 mph, gusting to 26 mph. Visibility 10 statute miles.

## Features

- Enter any airport code (ICAO format, e.g., KJFK, KLAX, EGLL)
- Fetches real-time METAR data from the Aviation Weather Center API
- Converts temperatures to Fahrenheit
- Converts wind speeds from knots to mph
- Translates cloud coverage codes to plain English
- Decodes weather phenomena (rain, snow, fog, etc.)
- Displays both the human-readable summary and raw METAR

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Getting Started

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd KODECLOUD-METAR-READER
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:5000
   ```

4. Enter an airport code and click "Get Weather"

## Example Airport Codes

| Code | Airport |
|------|---------|
| KJFK | New York JFK |
| KLAX | Los Angeles International |
| KORD | Chicago O'Hare |
| KSFO | San Francisco International |
| EGLL | London Heathrow |
| LFPG | Paris Charles de Gaulle |

## Project Structure

```
├── Controllers/
│   └── HomeController.cs       # Handles HTTP requests
├── Models/
│   ├── MetarData.cs            # Parsed METAR data record
│   ├── CloudLayer.cs           # Cloud layer record
│   └── WeatherSearchModel.cs   # View model for search
├── Services/
│   ├── IMetarService.cs        # Service interface
│   ├── MetarService.cs         # API client
│   └── MetarDecoder.cs         # METAR parsing logic
├── Views/
│   ├── Home/Index.cshtml       # Main page
│   └── Shared/_Layout.cshtml   # Bootstrap layout
├── MetarReader.Tests/          # Unit test project
│   ├── MetarDecoderTests.cs    # Decoder unit tests
│   └── MetarServiceTests.cs    # Service unit tests
└── Program.cs                  # Application entry point
```

## Testing

The project includes a comprehensive unit test suite with 70 tests.

### Running Tests

```bash
dotnet test
```

### Test Coverage

**MetarDecoderTests** - Tests the core METAR parsing logic:
- Airport code extraction (with METAR/SPECI prefixes)
- Wind parsing (direction, speed, gusts, variable, calm)
- Visibility parsing (statute miles, fractions, CAVOK)
- Weather phenomena decoding (rain, snow, fog, thunderstorm, etc.)
- Cloud layer parsing (FEW/SCT/BKN/OVC, altitude, multiple layers)
- Temperature/dew point parsing (positive and negative values)
- Altimeter reading
- Human-readable summary generation
- Cardinal wind direction conversion (N/NE/E/SE/S/SW/W/NW)

**MetarServiceTests** - Tests the HTTP service with mocked dependencies:
- Input validation (empty, null, whitespace, invalid length)
- Successful API responses
- Error handling (network errors, empty responses, server errors)
- Real-world METAR scenarios

### Test Libraries

- [xUnit](https://xunit.net/) - Test framework
- [FluentAssertions](https://fluentassertions.com/) - Readable assertions
- [Moq](https://github.com/moq/moq4) - Mocking framework
- [AutoFixture](https://github.com/AutoFixture/AutoFixture) - Test data generation

## Technology Stack

- ASP.NET Core MVC (.NET 8)
- Bootstrap 5
- Aviation Weather Center API

## Data Source

Weather data is provided by the [Aviation Weather Center](https://aviationweather.gov) API.
