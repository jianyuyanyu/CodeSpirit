using System.Net;
using System.Text.Json;
using CodeSpirit.Core;
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;

namespace CodeSpirit.Tests;

public class WebTests : IAsyncDisposable
{
    private IDistributedApplicationBuilder _appBuilder;
    private DistributedApplication _app;

    public WebTests()
    {
        _appBuilder = CreateAppBuilder();
    }

    private IDistributedApplicationBuilder CreateAppBuilder()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        return builder;
    }

    private async Task InitializeAsync()
    {
        if (_app == null)
        {
            _app = _appBuilder.Build();
            await _app.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private async Task WaitForServiceAsync(string serviceName)
    {
        // 使用简单的延迟来等待服务启动
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync("webfrontend");

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5000/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetIdentityHealthCheckReturnsHealthy()
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync("identity");

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5001/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task GetConfigServiceHealthCheckReturnsHealthy()
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync("config");

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("http://localhost:5002/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Theory]
    [InlineData("identity", "http://localhost:5001/api/identity/users")]
    [InlineData("identity", "http://localhost:5001/api/identity/roles")]
    [InlineData("config", "http://localhost:5002/api/config/apps")]
    [InlineData("config", "http://localhost:5002/api/config/configitems")]
    public async Task GetProtectedEndpointsReturnUnauthorizedForAnonymousUser(string service, string endpoint)
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync(service);

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(endpoint);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RedisCommanderUiIsAccessible()
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync("cache");

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"http://localhost:{61689}/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SeqUiIsAccessible()
    {
        // Arrange
        await InitializeAsync();
        await WaitForServiceAsync("seq");

        // Act
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"http://localhost:{61688}/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ServiceDependenciesAreProperlyConfigured()
    {
        // Arrange
        await InitializeAsync();

        // Act & Assert
        var resources = _appBuilder.Resources;
        
        // Verify core services
        Assert.Contains(resources, r => r.Name == "cache");
        Assert.Contains(resources, r => r.Name == "seq");
        
        // Verify application services
        Assert.Contains(resources, r => r.Name == "identity");
        Assert.Contains(resources, r => r.Name == "config");
        Assert.Contains(resources, r => r.Name == "webfrontend");
        
        // Verify UI endpoints
        Assert.Contains(resources, r => r.Name == "commander-ui");
        Assert.Contains(resources, r => r.Name == "seq-ui");

        //// Verify service dependencies
        //var webfrontend = resources.First(r => r.Name == "webfrontend");
        //Assert.Contains(webfrontend.Dependencies, d => d.Target.Name == "cache");
        //Assert.Contains(webfrontend.Dependencies, d => d.Target.Name == "seq");
        //Assert.Contains(webfrontend.Dependencies, d => d.Target.Name == "identity");
        //Assert.Contains(webfrontend.Dependencies, d => d.Target.Name == "config");
    }
}
