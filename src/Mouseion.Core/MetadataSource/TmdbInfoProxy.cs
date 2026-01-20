// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mouseion.Common.Extensions;
using Mouseion.Common.Http;
using Mouseion.Core.Movies;

namespace Mouseion.Core.MetadataSource;

public partial class TmdbInfoProxy : IProvideMovieInfo
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private readonly string _apiKey;

    private readonly IHttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TmdbInfoProxy> _logger;

    public TmdbInfoProxy(
        IHttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<TmdbInfoProxy> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = configuration["TMDb:ApiKey"] ?? Environment.GetEnvironmentVariable("TMDB_API_KEY") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogApiKeyNotConfiguredWithInstructions(_logger);
        }
    }

    public async Task<Movie?> GetByTmdbIdAsync(int tmdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogApiKeyNotConfigured(_logger);
            return null;
        }

        var cacheKey = $"movie_tmdb_{tmdbId}";

        if (_cache.TryGetValue(cacheKey, out Movie? cached))
        {
            LogCacheHitTmdbId(_logger, tmdbId);
            return cached;
        }

        try
        {
            LogFetchingByTmdbId(_logger, tmdbId);

            var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&append_to_response=credits,release_dates";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                LogTmdbReturnedStatus(_logger, response.StatusCode, tmdbId);
                return null;
            }

            var result = ParseMovie(response.Content);
            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorTmdbId(_logger, ex, tmdbId);
            return null;
        }
        catch (JsonException ex)
        {
            LogParseErrorTmdbId(_logger, ex, tmdbId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutTmdbId(_logger, ex, tmdbId);
            return null;
        }
    }

    public async Task<Movie?> GetByImdbIdAsync(string imdbId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogApiKeyNotConfigured(_logger);
            return null;
        }

        var cacheKey = $"movie_imdb_{imdbId}";

        if (_cache.TryGetValue(cacheKey, out Movie? cached))
        {
            LogCacheHitImdbId(_logger, imdbId);
            return cached;
        }

        try
        {
            LogFetchingByImdbId(_logger, imdbId);

            var url = $"{BaseUrl}/find/{imdbId}?api_key={_apiKey}&external_source=imdb_id";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                LogTmdbReturnedStatusImdb(_logger, response.StatusCode, imdbId);
                return null;
            }

            var result = ParseFindResult(response.Content);
            if (result != null)
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorImdbId(_logger, ex, imdbId);
            return null;
        }
        catch (JsonException ex)
        {
            LogParseErrorImdbId(_logger, ex, imdbId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutImdbId(_logger, ex, imdbId);
            return null;
        }
    }

    public async Task<List<Movie>> SearchByTitleAsync(string title, int? year = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogApiKeyNotConfigured(_logger);
            return new List<Movie>();
        }

        var cacheKey = $"movie_search_{title.ToLowerInvariant()}_{year}";

        if (_cache.TryGetValue(cacheKey, out List<Movie>? cached))
        {
            LogCacheHitSearch(_logger, title.SanitizeForLog());
            return cached ?? new List<Movie>();
        }

        try
        {
            LogSearchingByTitle(_logger, title.SanitizeForLog());

            var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(title)}";
            if (year.HasValue)
            {
                url += $"&year={year.Value}";
            }

            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                LogTmdbSearchReturnedStatus(_logger, response.StatusCode);
                return new List<Movie>();
            }

            var result = ParseSearchResults(response.Content);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));

            return result;
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorSearch(_logger, ex, title.SanitizeForLog());
            return new List<Movie>();
        }
        catch (JsonException ex)
        {
            LogParseErrorSearch(_logger, ex, title.SanitizeForLog());
            return new List<Movie>();
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutSearch(_logger, ex, title.SanitizeForLog());
            return new List<Movie>();
        }
    }

    public async Task<List<Movie>> GetTrendingAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new List<Movie>();
        }

        try
        {
            var url = $"{BaseUrl}/trending/movie/week?api_key={_apiKey}";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new List<Movie>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorTrending(_logger, ex);
            return new List<Movie>();
        }
        catch (JsonException ex)
        {
            LogParseErrorTrending(_logger, ex);
            return new List<Movie>();
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutTrending(_logger, ex);
            return new List<Movie>();
        }
    }

    public async Task<List<Movie>> GetPopularAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new List<Movie>();
        }

        try
        {
            var url = $"{BaseUrl}/movie/popular?api_key={_apiKey}";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new List<Movie>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorPopular(_logger, ex);
            return new List<Movie>();
        }
        catch (JsonException ex)
        {
            LogParseErrorPopular(_logger, ex);
            return new List<Movie>();
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutPopular(_logger, ex);
            return new List<Movie>();
        }
    }

    public async Task<List<Movie>> GetUpcomingAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new List<Movie>();
        }

        try
        {
            var url = $"{BaseUrl}/movie/upcoming?api_key={_apiKey}";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new List<Movie>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorUpcoming(_logger, ex);
            return new List<Movie>();
        }
        catch (JsonException ex)
        {
            LogParseErrorUpcoming(_logger, ex);
            return new List<Movie>();
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutUpcoming(_logger, ex);
            return new List<Movie>();
        }
    }

    public async Task<List<Movie>> GetNowPlayingAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return new List<Movie>();
        }

        try
        {
            var url = $"{BaseUrl}/movie/now_playing?api_key={_apiKey}";
            var request = new HttpRequestBuilder(url).Build();

            var response = await _httpClient.GetAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return new List<Movie>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (HttpRequestException ex)
        {
            LogNetworkErrorNowPlaying(_logger, ex);
            return new List<Movie>();
        }
        catch (JsonException ex)
        {
            LogParseErrorNowPlaying(_logger, ex);
            return new List<Movie>();
        }
        catch (TaskCanceledException ex)
        {
            LogTimeoutNowPlaying(_logger, ex);
            return new List<Movie>();
        }
    }

    private Movie? ParseMovie(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var movie = new Movie
            {
                Title = root.GetProperty("title").GetString() ?? "Unknown",
                Year = ExtractYear(root),
                Overview = JsonHelpers.GetStringProperty(root, "overview"),
                Runtime = JsonHelpers.GetIntProperty(root, "runtime"),
                TmdbId = JsonHelpers.GetIntProperty(root, "id")?.ToString(),
                ImdbId = JsonHelpers.GetStringProperty(root, "imdb_id"),
                Genres = GetGenres(root),
                Certification = ExtractCertification(root),
                Studio = JsonHelpers.GetStringProperty(root, "production_companies"),
                Website = JsonHelpers.GetStringProperty(root, "homepage"),
                Popularity = JsonHelpers.GetFloatProperty(root, "popularity"),
                Images = GetImages(root)
            };

            if (root.TryGetProperty("release_date", out var releaseDate) &&
                DateTime.TryParse(releaseDate.GetString(), out var date))
            {
                movie.PhysicalRelease = date;
            }

            return movie;
        }
        catch (JsonException ex)
        {
            LogParseErrorMovieJson(_logger, ex);
            return null;
        }
    }

    private Movie? ParseFindResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("movie_results", out var results) &&
                results.GetArrayLength() > 0)
            {
                var first = results[0];
                return ParseMovieFromSearchResult(first);
            }

            return null;
        }
        catch (JsonException ex)
        {
            LogParseErrorFindResult(_logger, ex);
            return null;
        }
    }

    private List<Movie> ParseSearchResults(string json)
    {
        var movies = new List<Movie>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    var movie = ParseMovieFromSearchResult(item);
                    if (movie != null)
                    {
                        movies.Add(movie);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            LogParseErrorSearchResults(_logger, ex);
        }

        return movies;
    }

    private Movie? ParseMovieFromSearchResult(JsonElement item)
    {
        try
        {
            var tmdbId = JsonHelpers.GetIntProperty(item, "id");
            if (!tmdbId.HasValue)
            {
                return null;
            }

            var movie = new Movie
            {
                Title = item.GetProperty("title").GetString() ?? "Unknown",
                Year = ExtractYear(item),
                Overview = JsonHelpers.GetStringProperty(item, "overview"),
                TmdbId = tmdbId.Value.ToString(),
                Genres = GetGenres(item),
                Popularity = JsonHelpers.GetFloatProperty(item, "popularity"),
                Images = GetImages(item)
            };

            if (item.TryGetProperty("release_date", out var releaseDate) &&
                DateTime.TryParse(releaseDate.GetString(), out var date))
            {
                movie.PhysicalRelease = date;
            }

            return movie;
        }
        catch (JsonException ex)
        {
            LogParseErrorSearchResultItem(_logger, ex);
            return null;
        }
    }


    private static int ExtractYear(JsonElement element)
    {
        var releaseDate = JsonHelpers.GetStringProperty(element, "release_date");
        if (string.IsNullOrWhiteSpace(releaseDate))
        {
            return 0;
        }

        if (DateTime.TryParse(releaseDate, out var date))
        {
            return date.Year;
        }

        return 0;
    }

    private static List<string> GetGenres(JsonElement element)
    {
        var genres = new List<string>();

        if (element.TryGetProperty("genres", out var genresArray) && genresArray.ValueKind == JsonValueKind.Array)
        {
            ExtractGenreNames(genresArray, genres);
        }
        else if (element.TryGetProperty("genre_ids", out var genreIds) && genreIds.ValueKind == JsonValueKind.Array)
        {
            ExtractGenreIds(genreIds, genres);
        }

        return genres;
    }

    private static void ExtractGenreNames(JsonElement genresArray, List<string> genres)
    {
        foreach (var genre in genresArray.EnumerateArray())
        {
            if (genre.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                var value = name.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    genres.Add(value);
                }
            }
        }
    }

    private static void ExtractGenreIds(JsonElement genreIds, List<string> genres)
    {
        foreach (var genreId in genreIds.EnumerateArray())
        {
            if (genreId.ValueKind == JsonValueKind.Number)
            {
                genres.Add(genreId.GetInt32().ToString());
            }
        }
    }

    private static List<string> GetImages(JsonElement element)
    {
        var images = new List<string>();

        var posterPath = JsonHelpers.GetStringProperty(element, "poster_path");
        if (!string.IsNullOrWhiteSpace(posterPath))
        {
            images.Add($"https://image.tmdb.org/t/p/original{posterPath}");
        }

        var backdropPath = JsonHelpers.GetStringProperty(element, "backdrop_path");
        if (!string.IsNullOrWhiteSpace(backdropPath))
        {
            images.Add($"https://image.tmdb.org/t/p/original{backdropPath}");
        }

        return images;
    }

    private static string? ExtractCertification(JsonElement element)
    {
        if (element.TryGetProperty("release_dates", out var releaseDates) &&
            releaseDates.TryGetProperty("results", out var results))
        {
            foreach (var country in results.EnumerateArray())
            {
                if (country.TryGetProperty("iso_3166_1", out var iso) &&
                    iso.GetString() == "US" &&
                    country.TryGetProperty("release_dates", out var dates))
                {
                    foreach (var date in dates.EnumerateArray())
                    {
                        var cert = JsonHelpers.GetStringProperty(date, "certification");
                        if (!string.IsNullOrWhiteSpace(cert))
                        {
                            return cert;
                        }
                    }
                }
            }
        }

        return null;
    }

    // LoggerMessage source generators for zero-cost logging when disabled
    [LoggerMessage(Level = LogLevel.Warning, Message = "TMDb API key not configured. Set TMDb:ApiKey in config or TMDB_API_KEY env var")]
    private static partial void LogApiKeyNotConfiguredWithInstructions(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TMDb API key not configured")]
    private static partial void LogApiKeyNotConfigured(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for TMDb ID: {TmdbId}")]
    private static partial void LogCacheHitTmdbId(ILogger logger, int tmdbId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching movie by TMDb ID: {TmdbId}")]
    private static partial void LogFetchingByTmdbId(ILogger logger, int tmdbId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TMDb returned {StatusCode} for TMDb ID {TmdbId}")]
    private static partial void LogTmdbReturnedStatus(ILogger logger, System.Net.HttpStatusCode statusCode, int tmdbId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching movie by TMDb ID: {TmdbId}")]
    private static partial void LogNetworkErrorTmdbId(ILogger logger, Exception ex, int tmdbId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for movie TMDb ID: {TmdbId}")]
    private static partial void LogParseErrorTmdbId(ILogger logger, Exception ex, int tmdbId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: movie by TMDb ID {TmdbId}")]
    private static partial void LogTimeoutTmdbId(ILogger logger, Exception ex, int tmdbId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for IMDB ID: {ImdbId}")]
    private static partial void LogCacheHitImdbId(ILogger logger, string imdbId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching movie by IMDB ID: {ImdbId}")]
    private static partial void LogFetchingByImdbId(ILogger logger, string imdbId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TMDb returned {StatusCode} for IMDB ID {ImdbId}")]
    private static partial void LogTmdbReturnedStatusImdb(ILogger logger, System.Net.HttpStatusCode statusCode, string imdbId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching movie by IMDB ID: {ImdbId}")]
    private static partial void LogNetworkErrorImdbId(ILogger logger, Exception ex, string imdbId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for movie IMDB ID: {ImdbId}")]
    private static partial void LogParseErrorImdbId(ILogger logger, Exception ex, string imdbId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: movie by IMDB ID {ImdbId}")]
    private static partial void LogTimeoutImdbId(ILogger logger, Exception ex, string imdbId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for movie search: {Title}")]
    private static partial void LogCacheHitSearch(ILogger logger, string title);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching movies by title: {Title}")]
    private static partial void LogSearchingByTitle(ILogger logger, string title);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TMDb search returned {StatusCode}")]
    private static partial void LogTmdbSearchReturnedStatus(ILogger logger, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error searching movies by title: {Title}")]
    private static partial void LogNetworkErrorSearch(ILogger logger, Exception ex, string title);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse search results for movie title: {Title}")]
    private static partial void LogParseErrorSearch(ILogger logger, Exception ex, string title);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: movie title search {Title}")]
    private static partial void LogTimeoutSearch(ILogger logger, Exception ex, string title);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching trending movies")]
    private static partial void LogNetworkErrorTrending(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for trending movies")]
    private static partial void LogParseErrorTrending(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: trending movies")]
    private static partial void LogTimeoutTrending(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching popular movies")]
    private static partial void LogNetworkErrorPopular(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for popular movies")]
    private static partial void LogParseErrorPopular(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: popular movies")]
    private static partial void LogTimeoutPopular(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching upcoming movies")]
    private static partial void LogNetworkErrorUpcoming(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for upcoming movies")]
    private static partial void LogParseErrorUpcoming(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: upcoming movies")]
    private static partial void LogTimeoutUpcoming(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Network error fetching now playing movies")]
    private static partial void LogNetworkErrorNowPlaying(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to parse response for now playing movies")]
    private static partial void LogParseErrorNowPlaying(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request timed out or was cancelled: now playing movies")]
    private static partial void LogTimeoutNowPlaying(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error parsing TMDb movie JSON")]
    private static partial void LogParseErrorMovieJson(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error parsing TMDb find result")]
    private static partial void LogParseErrorFindResult(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error parsing TMDb search results")]
    private static partial void LogParseErrorSearchResults(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error parsing movie from search result")]
    private static partial void LogParseErrorSearchResultItem(ILogger logger, Exception ex);
}
