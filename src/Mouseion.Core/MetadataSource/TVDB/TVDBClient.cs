// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Mouseion.Core.MetadataSource.TVDB;

/// <summary>
/// TVDB v4 API client with JWT authentication and Polly resilience
/// </summary>
public interface ITVDBClient
{
    Task<string?> GetAsync(string endpoint, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default) where T : class;
}

public class TVDBClient : ITVDBClient, IDisposable
{
    private readonly TVDBSettings _settings;
    private readonly ILogger<TVDBClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _pipeline;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TVDBClient(TVDBSettings settings, ILogger<TVDBClient> logger, HttpClient httpClient)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.BaseAddress = new Uri(_settings.ApiUrl);

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _settings.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "TVDB API retry attempt {AttemptNumber} after {Delay}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string?> GetAsync(string endpoint, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("TVDB API key not configured");
            return null;
        }

        await EnsureAuthenticatedAsync(ct);

        if (string.IsNullOrWhiteSpace(_token))
        {
            return null;
        }

        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request, token);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("TVDB token expired, re-authenticating");
                    _token = null;
                    _tokenExpiry = DateTime.MinValue;
                    await EnsureAuthenticatedAsync(token);

                    if (string.IsNullOrWhiteSpace(_token))
                    {
                        return null;
                    }

                    using var retryRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    retryRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    response = await _httpClient.SendAsync(retryRequest, token);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("TVDB API returned {StatusCode} for {Endpoint}", response.StatusCode, endpoint);
                    return null;
                }

                return await response.Content.ReadAsStringAsync(token);
            }, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling TVDB API: {Endpoint}", endpoint);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "TVDB API request timed out or cancelled: {Endpoint}", endpoint);
            return null;
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default) where T : class
    {
        var json = await GetAsync(endpoint, ct);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize TVDB API response: {Endpoint}", endpoint);
            return null;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_token) && DateTime.UtcNow < _tokenExpiry)
        {
            return;
        }

        await _authLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrWhiteSpace(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                return;
            }

            await AuthenticateAsync(ct);
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        _logger.LogDebug("Authenticating with TVDB API");

        try
        {
            var loginPayload = JsonSerializer.Serialize(new { apikey = _settings.ApiKey });
            using var content = new StringContent(loginPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v4/login", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TVDB authentication failed with status {StatusCode}", response.StatusCode);
                return;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("token", out var tokenElement))
            {
                _token = tokenElement.GetString();
                // TVDB tokens are valid for 1 month, but we'll refresh every 23 hours to be safe
                _tokenExpiry = DateTime.UtcNow.AddHours(23);
                _logger.LogDebug("TVDB authentication successful, token expires at {Expiry}", _tokenExpiry);
            }
            else
            {
                _logger.LogError("TVDB authentication response missing token");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during TVDB authentication");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse TVDB authentication response");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "TVDB authentication request timed out");
        }
    }

    public void Dispose()
    {
        _authLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
