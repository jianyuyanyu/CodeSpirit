using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http;
using Moq.Protected;

namespace CodeSpirit.ConfigCenter.Sdk.Tests.Registration;

/// <summary>
/// 应用注册服务测试
/// </summary>
public class AppRegistrationServiceTests
{
    private readonly Mock<IOptions<ConfigCenterOptions>> _optionsMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<AppRegistrationService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public AppRegistrationServiceTests()
    {
        _optionsMock = new Mock<IOptions<ConfigCenterOptions>>();
        _configurationMock = new Mock<IConfiguration>();
        _environmentMock = new Mock<IHostEnvironment>();
        _loggerMock = new Mock<ILogger<AppRegistrationService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            AutoRegister = true
        });

        _environmentMock.Setup(e => e.ApplicationName).Returns("TestApplication");
    }

    #region GetCurrentAppId Tests

    [Fact]
    public void GetCurrentAppId_OptionsHasAppId_ReturnsOptionsAppId()
    {
        // Arrange
        var expectedAppId = "configured-app-id";
        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            AppId = expectedAppId,
            AutoRegister = true
        });

        var service = CreateService();

        // Act
        var result = service.GetCurrentAppId();

        // Assert
        result.Should().Be(expectedAppId);
    }

    [Fact]
    public void GetCurrentAppId_NoOptionsAppId_UsesServiceName()
    {
        // Arrange
        var expectedAppId = "service-from-config";
        _configurationMock.Setup(c => c["ServiceName"]).Returns(expectedAppId);

        var service = CreateService();

        // Act
        var result = service.GetCurrentAppId();

        // Assert
        result.Should().Be(expectedAppId);
    }

    [Fact]
    public void GetCurrentAppId_NoServiceName_UsesApplicationName()
    {
        // Arrange
        var expectedAppId = "app-name-from-config";
        _configurationMock.Setup(c => c["ServiceName"]).Returns((string?)null);
        _configurationMock.Setup(c => c["ApplicationName"]).Returns(expectedAppId);

        var service = CreateService();

        // Act
        var result = service.GetCurrentAppId();

        // Assert
        result.Should().Be(expectedAppId);
    }

    [Fact]
    public void GetCurrentAppId_FallbackToEnvironment_UsesEnvironmentApplicationName()
    {
        // Arrange
        var expectedAppId = "env-application";
        _configurationMock.Setup(c => c["ServiceName"]).Returns((string?)null);
        _configurationMock.Setup(c => c["ApplicationName"]).Returns((string?)null);
        _environmentMock.Setup(e => e.ApplicationName).Returns(expectedAppId);

        var service = CreateService();

        // Act
        var result = service.GetCurrentAppId();

        // Assert
        result.Should().Be(expectedAppId);
    }

    [Fact]
    public void GetCurrentAppId_Cached_ReturnsSameValue()
    {
        // Arrange
        var expectedAppId = "cached-app-id";
        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            AppId = expectedAppId,
            AutoRegister = true
        });

        var service = CreateService();

        // Act
        var result1 = service.GetCurrentAppId();
        var result2 = service.GetCurrentAppId();

        // Assert
        result1.Should().Be(expectedAppId);
        result2.Should().Be(expectedAppId);
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_AutoRegisterDisabled_ReturnsFalseWithoutCallingApi()
    {
        // Arrange
        _optionsMock.Setup(o => o.Value).Returns(new ConfigCenterOptions
        {
            AutoRegister = false
        });

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync();

        // Assert
        result.Should().BeFalse();
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_ReturnsTrue()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"success\":true}")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_FailedRegistration_ReturnsFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Registration failed\"}")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_HttpException_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_SendsCorrectRequestUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        var service = CreateService();

        // Act
        await service.RegisterAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("api/config/client/apps/register");
    }

    [Fact]
    public async Task RegisterAsync_CancellationRequested_ThrowsOrReturnsFalse()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync(cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Private Helpers

    private AppRegistrationService CreateService()
    {
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        return new AppRegistrationService(
            httpClient,
            _optionsMock.Object,
            _configurationMock.Object,
            _environmentMock.Object,
            _loggerMock.Object);
    }

    #endregion
}

