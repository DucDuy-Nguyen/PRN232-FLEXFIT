using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Forwarder;
using Xunit;

namespace FlexFit.ApiGateway.Tests;

public sealed class GatewayRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly MockHttpMessageHandler _mockHandler = new();

    public GatewayRoutingTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("Gateway__RequestBodyLimitBytes", "1048576");
        Environment.SetEnvironmentVariable("Jwt__Key", new string('a', 32));
        Environment.SetEnvironmentVariable("Jwt__Issuer", "FlexFitIdentity");
        Environment.SetEnvironmentVariable("Jwt__Audience", "FlexFitClient");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Register mock client factory to redirect proxy requests to our MockHttpMessageHandler
                var invoker = new HttpMessageInvoker(_mockHandler);
                services.AddSingleton<IForwarderHttpClientFactory>(new MockForwarderHttpClientFactory(invoker));
            });
        });
    }

    [Fact]
    public async Task Get_HealthLive_ShouldReturnHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task Request_ShouldPassCorrelationIdDownstream()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login");
        request.Content = JsonContent.Create(new { email = "test@flexfit.com", password = "Password123" });
        request.Headers.Add("X-Correlation-ID", "custom-correlation-123");

        _mockHandler.ResponseFunc = req =>
        {
            // Verify Gateway forward headers
            Assert.True(req.Headers.Contains("X-Correlation-ID"));
            Assert.Equal("custom-correlation-123", req.Headers.GetValues("X-Correlation-ID").First());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ \"token\": \"dummy\" }") };
        };

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal("custom-correlation-123", response.Headers.GetValues("X-Correlation-ID").First());
    }

    [Fact]
    public async Task ProtectedRoute_WithoutJwtToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/profiles/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRoute_WithMalformedJwtToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profiles/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "malformed-token-12345");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Helper handler representing downstream server interception
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFunc { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ResponseFunc != null)
            {
                return Task.FromResult(ResponseFunc(request));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    // Helper implementation mapping to YARP requirements
    private sealed class MockForwarderHttpClientFactory : IForwarderHttpClientFactory
    {
        private readonly HttpMessageInvoker _invoker;
        public MockForwarderHttpClientFactory(HttpMessageInvoker invoker) => _invoker = invoker;

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            return _invoker;
        }
    }
}
