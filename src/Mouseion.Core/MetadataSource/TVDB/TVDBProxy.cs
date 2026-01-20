// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Mouseion.Core.TV;
using Polly;
using Polly.Retry;

namespace Mouseion.Core.MetadataSource.TVDB;

public interface ITVDBProxy
{
    Task<Series?> GetSeriesByTvdbIdAsync(int tvdbId, CancellationToken ct = default);
    Task<List<Episode>> GetEpisodesBySeriesIdAsync(int tvdbId, CancellationToken ct = default);
    Task<List<Series>> SearchSeriesAsync(string query, CancellationToken ct = default);
}

public class TVDBProxy : ITVDBProxy
{
    private readonly TVDBSettings _settings;
    private readonly ILogger<TVDBProxy> _logger;
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _pipeline;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TVDBProxy(
        TVDBSettings settings,
        ILogger<TVDBProxy> logger,
        HttpClient httpClient)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
                    _logger.LogWarning("TVDB API retry attempt {AttemptNumber} after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<Series?> GetSeriesByTvdbIdAsync(int tvdbId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching series from TVDB: {TvdbId}", tvdbId);

        if (!await EnsureAuthenticatedAsync(ct).ConfigureAwait(false))
        {
            return null;
        }

        try
        {
            var response = await _pipeline.ExecuteAsync(async token =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiUrl}/series/{tvdbId}/extended");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TVDB API returned {StatusCode} for series {TvdbId}", response.StatusCode, tvdbId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<TVDBResponse<TVDBSeries>>(content, JsonOptions);

            if (result?.Data == null)
            {
                _logger.LogWarning("TVDB returned null data for series {TvdbId}", tvdbId);
                return null;
            }

            return MapToSeries(result.Data);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching series from TVDB: {TvdbId}", tvdbId);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request timed out fetching series from TVDB: {TvdbId}", tvdbId);
            return null;
        }
    }

    public async Task<List<Episode>> GetEpisodesBySeriesIdAsync(int tvdbId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching episodes from TVDB for series: {TvdbId}", tvdbId);

        if (!await EnsureAuthenticatedAsync(ct).ConfigureAwait(false))
        {
            return new List<Episode>();
        }

        try
        {
            var episodes = new List<Episode>();
            var page = 0;

            while (true)
            {
                var response = await _pipeline.ExecuteAsync(async token =>
                {
                    var url = $"{_settings.ApiUrl}/series/{tvdbId}/episodes/default?page={page}";
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
                }, ct).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("TVDB API returned {StatusCode} for episodes of series {TvdbId}",
                        response.StatusCode, tvdbId);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<TVDBResponse<TVDBEpisodesResult>>(content, JsonOptions);

                if (result?.Data?.Episodes == null || result.Data.Episodes.Count == 0)
                {
                    break;
                }

                foreach (var ep in result.Data.Episodes)
                {
                    episodes.Add(MapToEpisode(ep, tvdbId));
                }

                if (result.Links?.Next == null)
                {
                    break;
                }

                page++;
            }

            _logger.LogInformation("Retrieved {Count} episodes for series {TvdbId}", episodes.Count, tvdbId);
            return episodes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching episodes from TVDB for series: {TvdbId}", tvdbId);
            return new List<Episode>();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request timed out fetching episodes from TVDB for series: {TvdbId}", tvdbId);
            return new List<Episode>();
        }
    }

    public async Task<List<Series>> SearchSeriesAsync(string query, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching TVDB for: {Query}", query);

        if (!await EnsureAuthenticatedAsync(ct).ConfigureAwait(false))
        {
            return new List<Series>();
        }

        try
        {
            var response = await _pipeline.ExecuteAsync(async token =>
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.ApiUrl}/search?query={encodedQuery}&type=series");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                return await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TVDB API returned {StatusCode} for search: {Query}", response.StatusCode, query);
                return new List<Series>();
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<TVDBResponse<List<TVDBSearchResult>>>(content, JsonOptions);

            if (result?.Data == null)
            {
                return new List<Series>();
            }

            var series = result.Data
                .Where(r => r.TvdbId != null)
                .Select(MapSearchResultToSeries)
                .ToList();

            _logger.LogInformation("Found {Count} series for query: {Query}", series.Count, query);
            return series;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error searching TVDB for: {Query}", query);
            return new List<Series>();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Request timed out searching TVDB for: {Query}", query);
            return new List<Series>();
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("TVDB API key not configured");
            return false;
        }

        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
        {
            return true;
        }

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
            {
                return true;
            }

            _logger.LogInformation("Authenticating with TVDB API");

            var loginRequest = new { apikey = _settings.ApiKey, pin = _settings.Pin ?? "" };
            var response = await _httpClient.PostAsJsonAsync($"{_settings.ApiUrl}/login", loginRequest, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TVDB authentication failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<TVDBResponse<TVDBLoginResult>>(content, JsonOptions);

            if (string.IsNullOrEmpty(result?.Data?.Token))
            {
                _logger.LogError("TVDB authentication returned empty token");
                return false;
            }

            _token = result.Data.Token;
            _tokenExpiry = DateTime.UtcNow.AddMinutes(_settings.TokenRefreshMinutes);

            _logger.LogInformation("TVDB authentication successful, token valid until {Expiry}", _tokenExpiry);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with TVDB");
            return false;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static Series MapToSeries(TVDBSeries tvdb)
    {
        var series = new Series
        {
            TvdbId = tvdb.Id,
            Title = tvdb.Name ?? "Unknown",
            SortTitle = tvdb.Name?.ToLowerInvariant(),
            Overview = tvdb.Overview,
            Status = MapStatus(tvdb.Status?.Name),
            Network = tvdb.OriginalNetwork?.Name,
            Genres = tvdb.Genres?.Select(g => g.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>(),
            Images = new List<string>()
        };

        if (tvdb.FirstAired != null && DateTime.TryParse(tvdb.FirstAired, out var firstAired))
        {
            series.FirstAired = firstAired;
            series.Year = firstAired.Year;
        }

        if (tvdb.Image != null)
        {
            series.Images.Add(tvdb.Image);
        }

        if (tvdb.AirsDays?.FirstOrDefault() != null && !string.IsNullOrEmpty(tvdb.AirsTime))
        {
            series.AirTime = tvdb.AirsTime;
        }

        return series;
    }

    private static Episode MapToEpisode(TVDBEpisode tvdb, int seriesId)
    {
        var episode = new Episode
        {
            SeriesId = seriesId,
            SeasonNumber = tvdb.SeasonNumber ?? 0,
            EpisodeNumber = tvdb.Number ?? 0,
            AbsoluteEpisodeNumber = tvdb.AbsoluteNumber,
            Title = tvdb.Name,
            Overview = tvdb.Overview
        };

        if (!string.IsNullOrEmpty(tvdb.Aired) && DateTime.TryParse(tvdb.Aired, out var airDate))
        {
            episode.AirDate = airDate;
            episode.AirDateUtc = DateTime.SpecifyKind(airDate, DateTimeKind.Utc);
        }

        return episode;
    }

    private static Series MapSearchResultToSeries(TVDBSearchResult result)
    {
        var series = new Series
        {
            TvdbId = int.TryParse(result.TvdbId, out var id) ? id : 0,
            Title = result.Name ?? "Unknown",
            SortTitle = result.Name?.ToLowerInvariant(),
            Overview = result.Overview,
            Status = MapStatus(result.Status),
            Network = result.Network,
            Images = new List<string>()
        };

        if (result.Year != null && int.TryParse(result.Year, out var year))
        {
            series.Year = year;
        }

        if (!string.IsNullOrEmpty(result.ImageUrl))
        {
            series.Images.Add(result.ImageUrl);
        }

        return series;
    }

    private static string MapStatus(string? status)
    {
        return status?.ToLowerInvariant() switch
        {
            "continuing" => "Continuing",
            "ended" => "Ended",
            "upcoming" => "Upcoming",
            _ => "Continuing"
        };
    }
}

#region TVDB API Response Models

internal class TVDBResponse<T>
{
    public string? Status { get; set; }
    public T? Data { get; set; }
    public TVDBLinks? Links { get; set; }
}

internal class TVDBLinks
{
    public string? Prev { get; set; }
    public string? Self { get; set; }
    public string? Next { get; set; }
}

internal class TVDBLoginResult
{
    public string? Token { get; set; }
}

internal class TVDBSeries
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Image { get; set; }
    public string? Overview { get; set; }
    public string? FirstAired { get; set; }
    public string? LastAired { get; set; }
    public string? NextAired { get; set; }
    public int? Score { get; set; }
    public TVDBStatus? Status { get; set; }
    public TVDBNetwork? OriginalNetwork { get; set; }
    public string? AirsTime { get; set; }
    public List<string>? AirsDays { get; set; }
    public List<TVDBGenre>? Genres { get; set; }
}

internal class TVDBStatus
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public bool? KeepUpdated { get; set; }
}

internal class TVDBNetwork
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
}

internal class TVDBGenre
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

internal class TVDBEpisodesResult
{
    public TVDBSeries? Series { get; set; }
    public List<TVDBEpisode>? Episodes { get; set; }
}

internal class TVDBEpisode
{
    public int? Id { get; set; }
    public int? SeriesId { get; set; }
    public string? Name { get; set; }
    public string? Aired { get; set; }
    public int? Runtime { get; set; }
    public string? Overview { get; set; }
    public string? Image { get; set; }
    public int? Number { get; set; }
    public int? SeasonNumber { get; set; }
    public int? AbsoluteNumber { get; set; }
}

internal class TVDBSearchResult
{
    [JsonPropertyName("tvdb_id")]
    public string? TvdbId { get; set; }
    public string? Name { get; set; }
    public string? Year { get; set; }
    public string? Overview { get; set; }
    public string? Status { get; set; }
    public string? Network { get; set; }
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

#endregion
