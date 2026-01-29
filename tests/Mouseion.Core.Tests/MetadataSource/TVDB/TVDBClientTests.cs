// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Mouseion.Core.MetadataSource.TVDB;

namespace Mouseion.Core.Tests.MetadataSource.TVDB;

public class TVDBClientTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly TVDBSettings _settings;
    private readonly TVDBClient _client;

    public TVDBClientTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api4.thetvdb.com/v4")
        };
        _settings = new TVDBSettings
        {
            ApiKey = "test-api-key",
            ApiUrl = "https://api4.thetvdb.com/v4",
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        _client = new TVDBClient(_settings, Mock.Of<ILogger<TVDBClient>>(), _httpClient);
    }

    public void Dispose()
    {
        _client.Dispose();
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAsync_returns_null_when_api_key_not_configured()
    {
        var settingsNoKey = new TVDBSettings { ApiKey = null };
        using var client = new TVDBClient(settingsNoKey, Mock.Of<ILogger<TVDBClient>>(), _httpClient);

        var result = await client.GetAsync("/test");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_authenticates_before_first_request()
    {
        SetupAuthenticationResponse("test-token");
        SetupGetResponse("/v4/test", """{"data": "test"}""");

        var result = await _client.GetAsync("/v4/test");

        Assert.NotNull(result);
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.PathAndQuery.Contains("login")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_reuses_token_for_subsequent_requests()
    {
        SetupAuthenticationResponse("test-token");
        SetupGetResponse("/v4/test1", """{"data": "test1"}""");
        SetupGetResponse("/v4/test2", """{"data": "test2"}""");

        await _client.GetAsync("/v4/test1");
        await _client.GetAsync("/v4/test2");

        // Login should only be called once
        _mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.RequestUri!.PathAndQuery.Contains("login")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_returns_response_content()
    {
        SetupAuthenticationResponse("test-token");
        var expectedJson = """{"status": "success", "data": {"id": 123, "name": "Test"}}""";
        SetupGetResponse("/v4/series/123", expectedJson);

        var result = await _client.GetAsync("/v4/series/123");

        Assert.Equal(expectedJson, result);
    }

    [Fact]
    public async Task GetAsync_generic_deserializes_response()
    {
        SetupAuthenticationResponse("test-token");
        SetupGetResponse("/v4/series/123", """{"status": "success", "data": {"id": 81189, "name": "Breaking Bad"}}""");

        var result = await _client.GetAsync<TVDBResponse<TVDBSeries>>("/v4/series/123");

        Assert.NotNull(result);
        Assert.Equal("success", result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(81189, result.Data.Id);
        Assert.Equal("Breaking Bad", result.Data.Name);
    }

    [Fact]
    public async Task GetAsync_returns_null_on_non_success_status()
    {
        SetupAuthenticationResponse("test-token");
        SetupGetResponse("/v4/series/999999", null, HttpStatusCode.NotFound);

        var result = await _client.GetAsync("/v4/series/999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_handles_authentication_failure()
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("login")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await _client.GetAsync("/v4/test");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_generic_returns_null_on_invalid_json()
    {
        SetupAuthenticationResponse("test-token");
        SetupGetResponse("/v4/series/123", "not valid json");

        var result = await _client.GetAsync<TVDBResponse<TVDBSeries>>("/v4/series/123");

        Assert.Null(result);
    }

    private void SetupAuthenticationResponse(string token)
    {
        var authResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"status": "success", "data": {"token": "{{token}}"}}""")
        };

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.PathAndQuery.Contains("login")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(authResponse);
    }

    private void SetupGetResponse(string path, string? content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = new StringContent(content);
        }

        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri!.PathAndQuery.Contains(path.TrimStart('/'))),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
