using System.Net;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using MetarReader.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace MetarReader.Tests;

public class MetarServiceTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<ILogger<MetarService>> _loggerMock = new();
    private readonly MetarDecoder _decoder = new();

    private MetarService CreateService(HttpClient httpClient)
    {
        return new MetarService(httpClient, _decoder, _loggerMock.Object);
    }

    private static HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object);
    }

    #region Input Validation

    [Fact]
    public async Task GetMetarAsync_EmptyAirportCode_ShouldReturnError()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Please enter an airport code");
    }

    [Fact]
    public async Task GetMetarAsync_NullAirportCode_ShouldReturnError()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync(null!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Please enter an airport code");
    }

    [Fact]
    public async Task GetMetarAsync_WhitespaceAirportCode_ShouldReturnError()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("   ");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Please enter an airport code");
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDE")]
    [InlineData("ABCDEF")]
    public async Task GetMetarAsync_InvalidLengthAirportCode_ShouldReturnError(string airportCode)
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync(airportCode);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("3-4 characters");
    }

    #endregion

    #region Successful API Calls

    [Fact]
    public async Task GetMetarAsync_ValidResponse_ShouldReturnDecodedMetar()
    {
        // Arrange
        const string mockMetar = "KJFK 041856Z 31015KT 10SM CLR 20/10 A3042";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KJFK");

        // Assert
        result.IsValid.Should().BeTrue();
        result.AirportCode.Should().Be("KJFK");
        result.RawMetar.Should().Be(mockMetar);
    }

    [Fact]
    public async Task GetMetarAsync_LowercaseAirportCode_ShouldConvertToUppercase()
    {
        // Arrange
        const string mockMetar = "KJFK 041856Z 31015KT 10SM CLR 20/10 A3042";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("kjfk");

        // Assert
        result.IsValid.Should().BeTrue();
        result.AirportCode.Should().Be("KJFK");
    }

    [Fact]
    public async Task GetMetarAsync_AirportCodeWithSpaces_ShouldTrim()
    {
        // Arrange
        const string mockMetar = "KJFK 041856Z 31015KT 10SM CLR 20/10 A3042";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("  KJFK  ");

        // Assert
        result.IsValid.Should().BeTrue();
        result.AirportCode.Should().Be("KJFK");
    }

    [Fact]
    public async Task GetMetarAsync_ThreeCharacterCode_ShouldBeValid()
    {
        // Arrange
        const string mockMetar = "LAX 041856Z 25010KT 10SM CLR 18/08 A2992";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("LAX");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task GetMetarAsync_EmptyResponse_ShouldReturnNotFoundError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("")
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("XXXX");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No METAR data found");
        result.ErrorMessage.Should().Contain("XXXX");
    }

    [Fact]
    public async Task GetMetarAsync_HttpError_ShouldReturnConnectionError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KJFK");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unable to connect");
    }

    [Fact]
    public async Task GetMetarAsync_ServerError_ShouldReturnConnectionError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handlerMock.Object);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KJFK");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unable to connect");
    }

    [Fact]
    public async Task GetMetarAsync_UnexpectedException_ShouldReturnGenericError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var httpClient = new HttpClient(handlerMock.Object);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KJFK");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unexpected error");
    }

    #endregion

    #region Real World METAR Scenarios

    [Fact]
    public async Task GetMetarAsync_ComplexMetar_ShouldDecodeAllFields()
    {
        // Arrange
        const string mockMetar = "SPECI KHIO 041114Z 15009KT 10SM -RA BKN014 BKN019 OVC043 09/09 A2940";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KHIO");

        // Assert
        result.IsValid.Should().BeTrue();
        result.AirportCode.Should().Be("KHIO");
        result.WindDirection.Should().Be("150");
        result.WindSpeedKnots.Should().Be(9);
        result.Visibility.Should().Be("10 statute miles");
        result.WeatherPhenomena.Should().Contain("Light rain");
        result.CloudLayers.Should().HaveCount(3);
        result.TemperatureCelsius.Should().Be(9);
        result.AltimeterInHg.Should().BeApproximately(29.40, 0.01);
        result.HumanReadableSummary.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMetarAsync_WinterWeather_ShouldDecodeNegativeTemperatures()
    {
        // Arrange
        const string mockMetar = "KORD 041856Z 36020G30KT 1SM +SN BLSN OVC005 M10/M12 A2950";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockMetar)
        };
        var httpClient = CreateMockHttpClient(response);
        var service = CreateService(httpClient);

        // Act
        var result = await service.GetMetarAsync("KORD");

        // Assert
        result.IsValid.Should().BeTrue();
        result.TemperatureCelsius.Should().Be(-10);
        result.DewPointCelsius.Should().Be(-12);
        result.GustSpeedKnots.Should().Be(30);
        result.WeatherPhenomena.Should().Contain("Heavy snow");
    }

    #endregion
}
