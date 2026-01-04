using FluentAssertions;
using MetarReader.Services;
using Xunit;

namespace MetarReader.Tests;

public class MetarDecoderTests
{
    private readonly MetarDecoder _decoder = new();

    #region Airport Code Parsing

    [Fact]
    public void Decode_ValidMetar_ShouldExtractAirportCode()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM FEW045 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.AirportCode.Should().Be("KJFK");
    }

    [Fact]
    public void Decode_MetarWithSpeciPrefix_ShouldExtractAirportCode()
    {
        // Arrange
        const string metar = "SPECI KHIO 041114Z 15009KT 10SM BKN014 09/09 A2940";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.AirportCode.Should().Be("KHIO");
    }

    [Fact]
    public void Decode_MetarWithMetarPrefix_ShouldExtractAirportCode()
    {
        // Arrange
        const string metar = "METAR KLAX 041853Z 25010KT 10SM CLR 18/08 A2992";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.AirportCode.Should().Be("KLAX");
    }

    #endregion

    #region Wind Parsing

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A3042", "310", 15, null)]
    [InlineData("KJFK 041856Z 18005KT 10SM CLR 20/10 A3042", "180", 5, null)]
    [InlineData("KJFK 041856Z 09025KT 10SM CLR 20/10 A3042", "090", 25, null)]
    public void Decode_WindWithoutGusts_ShouldParseCorrectly(
        string metar, string expectedDirection, int expectedSpeed, int? expectedGusts)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WindDirection.Should().Be(expectedDirection);
        result.WindSpeedKnots.Should().Be(expectedSpeed);
        result.GustSpeedKnots.Should().Be(expectedGusts);
    }

    [Fact]
    public void Decode_WindWithGusts_ShouldParseGustSpeed()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015G25KT 10SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WindDirection.Should().Be("310");
        result.WindSpeedKnots.Should().Be(15);
        result.GustSpeedKnots.Should().Be(25);
    }

    [Fact]
    public void Decode_VariableWind_ShouldParseAsVRB()
    {
        // Arrange
        const string metar = "KJFK 041856Z VRB05KT 10SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WindDirection.Should().Be("VRB");
        result.WindSpeedKnots.Should().Be(5);
    }

    [Fact]
    public void Decode_CalmWind_ShouldParseZeroSpeed()
    {
        // Arrange
        const string metar = "KJFK 041856Z 00000KT 10SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WindSpeedKnots.Should().Be(0);
    }

    #endregion

    #region Visibility Parsing

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A3042", "10 statute miles")]
    [InlineData("KJFK 041856Z 31015KT 5SM CLR 20/10 A3042", "5 statute miles")]
    [InlineData("KJFK 041856Z 31015KT 1SM CLR 20/10 A3042", "1 statute miles")]
    public void Decode_Visibility_ShouldParseStatuteMiles(string metar, string expectedVisibility)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.Visibility.Should().Be(expectedVisibility);
    }

    [Fact]
    public void Decode_FractionalVisibility_ShouldParseFraction()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 1/2SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.Visibility.Should().Be("1/2 statute miles");
    }

    [Fact]
    public void Decode_CAVOK_ShouldParseAsGreaterThan10km()
    {
        // Arrange
        const string metar = "EGLL 041856Z 31015KT CAVOK 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.Visibility.Should().Be("Greater than 10 km (CAVOK)");
    }

    #endregion

    #region Weather Phenomena Parsing

    [Fact]
    public void Decode_LightRain_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM -RA BKN020 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("Light rain");
    }

    [Fact]
    public void Decode_HeavySnow_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 2SM +SN OVC010 M02/M05 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("Heavy snow");
    }

    [Fact]
    public void Decode_Fog_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 00000KT 1/4SM FG VV002 10/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("fog");
    }

    [Fact]
    public void Decode_ThunderstormWithRain_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 5SM TSRA BKN020 25/20 A2980";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("thunderstorm with rain");
    }

    [Fact]
    public void Decode_Mist_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31005KT 5SM BR SCT020 15/14 A3010";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("mist");
    }

    [Fact]
    public void Decode_FreezingRain_ShouldDecodeCorrectly()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 3SM FZRA OVC010 M01/M02 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.WeatherPhenomena.Should().Contain("freezing rain");
    }

    #endregion

    #region Cloud Layer Parsing

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM FEW045 20/10 A3042", "FEW", 4500)]
    [InlineData("KJFK 041856Z 31015KT 10SM SCT020 20/10 A3042", "SCT", 2000)]
    [InlineData("KJFK 041856Z 31015KT 10SM BKN014 20/10 A3042", "BKN", 1400)]
    [InlineData("KJFK 041856Z 31015KT 10SM OVC008 20/10 A3042", "OVC", 800)]
    public void Decode_SingleCloudLayer_ShouldParseCorrectly(
        string metar, string expectedCoverage, int expectedAltitude)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.CloudLayers.Should().HaveCount(1);
        result.CloudLayers[0].Coverage.Should().Be(expectedCoverage);
        result.CloudLayers[0].AltitudeFeet.Should().Be(expectedAltitude);
    }

    [Fact]
    public void Decode_MultipleCloudLayers_ShouldParseAllLayers()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM FEW010 SCT025 BKN040 OVC100 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.CloudLayers.Should().HaveCount(4);
        result.CloudLayers[0].Coverage.Should().Be("FEW");
        result.CloudLayers[0].AltitudeFeet.Should().Be(1000);
        result.CloudLayers[1].Coverage.Should().Be("SCT");
        result.CloudLayers[1].AltitudeFeet.Should().Be(2500);
        result.CloudLayers[2].Coverage.Should().Be("BKN");
        result.CloudLayers[2].AltitudeFeet.Should().Be(4000);
        result.CloudLayers[3].Coverage.Should().Be("OVC");
        result.CloudLayers[3].AltitudeFeet.Should().Be(10000);
    }

    [Fact]
    public void Decode_ClearSkies_ShouldParseSKC()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM SKC 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.CloudLayers.Should().HaveCount(1);
        result.CloudLayers[0].Coverage.Should().Be("SKC");
    }

    [Fact]
    public void Decode_ClearSkiesCLR_ShouldParseCLR()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.CloudLayers.Should().HaveCount(1);
        result.CloudLayers[0].Coverage.Should().Be("CLR");
    }

    #endregion

    #region Temperature Parsing

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A3042", 20, 10)]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 35/25 A3042", 35, 25)]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 05/02 A3042", 5, 2)]
    public void Decode_PositiveTemperatures_ShouldParseCorrectly(
        string metar, int expectedTemp, int expectedDewPoint)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.TemperatureCelsius.Should().Be(expectedTemp);
        result.DewPointCelsius.Should().Be(expectedDewPoint);
    }

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR M05/M10 A3042", -5, -10)]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR M15/M20 A3042", -15, -20)]
    public void Decode_NegativeTemperatures_ShouldParseWithMinus(
        string metar, int expectedTemp, int expectedDewPoint)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.TemperatureCelsius.Should().Be(expectedTemp);
        result.DewPointCelsius.Should().Be(expectedDewPoint);
    }

    [Fact]
    public void Decode_MixedTemperatures_ShouldParseCorrectly()
    {
        // Arrange - positive temp, negative dew point
        const string metar = "KJFK 041856Z 31015KT 10SM CLR 02/M01 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.TemperatureCelsius.Should().Be(2);
        result.DewPointCelsius.Should().Be(-1);
    }

    #endregion

    #region Altimeter Parsing

    [Theory]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A3042", 30.42)]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A2992", 29.92)]
    [InlineData("KJFK 041856Z 31015KT 10SM CLR 20/10 A3015", 30.15)]
    public void Decode_Altimeter_ShouldParseInchesOfMercury(string metar, double expectedAltimeter)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.AltimeterInHg.Should().BeApproximately(expectedAltimeter, 0.01);
    }

    #endregion

    #region Human Readable Summary

    [Fact]
    public void Decode_ClearDayWithModerateWind_ShouldGenerateFriendlySummary()
    {
        // Arrange
        const string metar = "KJFK 041856Z 18010KT 10SM CLR 21/12 A3000";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain("Clear skies");
        result.HumanReadableSummary.Should().Contain("70°F"); // 21°C ≈ 70°F
        result.HumanReadableSummary.Should().Contain("south");
        result.HumanReadableSummary.Should().Contain("12 mph"); // 10 knots ≈ 12 mph
    }

    [Fact]
    public void Decode_RainyDay_ShouldIncludeWeatherInSummary()
    {
        // Arrange
        const string metar = "KJFK 041856Z 27015KT 5SM -RA BKN020 15/12 A2980";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain("Broken clouds");
        result.HumanReadableSummary.Should().Contain("2,000 feet");
        result.HumanReadableSummary.Should().Contain("light rain");
    }

    [Fact]
    public void Decode_GustyWind_ShouldIncludeGustsInSummary()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31020G35KT 10SM CLR 25/15 A3010";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain("gusting to");
        result.HumanReadableSummary.Should().Contain("40 mph"); // 35 knots ≈ 40 mph
    }

    [Fact]
    public void Decode_VariableWind_ShouldSayVariableInSummary()
    {
        // Arrange
        const string metar = "KJFK 041856Z VRB03KT 10SM CLR 20/10 A3000";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain("Variable winds");
    }

    [Fact]
    public void Decode_CalmWind_ShouldSayCalmInSummary()
    {
        // Arrange
        const string metar = "KJFK 041856Z 00000KT 10SM CLR 20/10 A3000";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain("Calm winds");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Decode_EmptyString_ShouldReturnError()
    {
        // Act
        var result = _decoder.Decode("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("No METAR data received");
    }

    [Fact]
    public void Decode_NullString_ShouldReturnError()
    {
        // Act
        var result = _decoder.Decode(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("No METAR data received");
    }

    [Fact]
    public void Decode_WhitespaceOnly_ShouldReturnError()
    {
        // Act
        var result = _decoder.Decode("   ");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("No METAR data received");
    }

    [Fact]
    public void Decode_ValidMetar_ShouldPreserveRawMetar()
    {
        // Arrange
        const string metar = "KJFK 041856Z 31015KT 10SM CLR 20/10 A3042";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.RawMetar.Should().Be(metar);
    }

    [Fact]
    public void Decode_RealWorldMetar_ShouldParseSuccessfully()
    {
        // Arrange - Real METAR from the API example
        const string metar = "SPECI KHIO 041114Z 15009KT 10SM -RA BKN014 BKN019 OVC043 09/09 A2940";

        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.IsValid.Should().BeTrue();
        result.AirportCode.Should().Be("KHIO");
        result.WindDirection.Should().Be("150");
        result.WindSpeedKnots.Should().Be(9);
        result.Visibility.Should().Be("10 statute miles");
        result.WeatherPhenomena.Should().Contain("Light rain");
        result.CloudLayers.Should().HaveCount(3);
        result.TemperatureCelsius.Should().Be(9);
        result.DewPointCelsius.Should().Be(9);
        result.AltimeterInHg.Should().BeApproximately(29.40, 0.01);
    }

    #endregion

    #region Wind Direction Cardinal Conversion

    [Theory]
    [InlineData("KJFK 041856Z 36010KT 10SM CLR 20/10 A3042", "north")]
    [InlineData("KJFK 041856Z 04510KT 10SM CLR 20/10 A3042", "northeast")]
    [InlineData("KJFK 041856Z 09010KT 10SM CLR 20/10 A3042", "east")]
    [InlineData("KJFK 041856Z 13510KT 10SM CLR 20/10 A3042", "southeast")]
    [InlineData("KJFK 041856Z 18010KT 10SM CLR 20/10 A3042", "south")]
    [InlineData("KJFK 041856Z 22510KT 10SM CLR 20/10 A3042", "southwest")]
    [InlineData("KJFK 041856Z 27010KT 10SM CLR 20/10 A3042", "west")]
    [InlineData("KJFK 041856Z 31510KT 10SM CLR 20/10 A3042", "northwest")]
    public void Decode_WindDirection_ShouldConvertToCardinalInSummary(
        string metar, string expectedCardinal)
    {
        // Act
        var result = _decoder.Decode(metar);

        // Assert
        result.HumanReadableSummary.Should().Contain(expectedCardinal);
    }

    #endregion
}
