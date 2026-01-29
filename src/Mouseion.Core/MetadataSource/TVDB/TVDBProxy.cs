// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.TV;

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
    private readonly ITVDBClient _client;

    public TVDBProxy(
        TVDBSettings settings,
        ILogger<TVDBProxy> logger,
        ITVDBClient client)
    {
        _settings = settings;
        _logger = logger;
        _client = client;
    }

    public async Task<Series?> GetSeriesByTvdbIdAsync(int tvdbId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching series from TVDB: {TvdbId}", tvdbId);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("TVDB API key not configured");
            return null;
        }

        try
        {
            var endpoint = $"/v4/series/{tvdbId}/extended";
            var response = await _client.GetAsync<TVDBResponse<TVDBSeries>>(endpoint, ct);

            if (response?.Data == null)
            {
                _logger.LogWarning("No series data returned from TVDB for {TvdbId}", tvdbId);
                return null;
            }

            return MapToSeries(response.Data);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error fetching series from TVDB: {TvdbId}", tvdbId);
            return null;
        }
    }

    public async Task<List<Episode>> GetEpisodesBySeriesIdAsync(int tvdbId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching episodes from TVDB for series: {TvdbId}", tvdbId);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("TVDB API key not configured");
            return [];
        }

        var episodes = new List<Episode>();

        try
        {
            var page = 0;
            string? nextPage;

            do
            {
                var endpoint = $"/v4/series/{tvdbId}/episodes/default?page={page}";
                var response = await _client.GetAsync<TVDBResponse<TVDBSeriesEpisodesData>>(endpoint, ct);

                if (response?.Data?.Episodes == null || response.Data.Episodes.Count == 0)
                {
                    break;
                }

                foreach (var tvdbEpisode in response.Data.Episodes)
                {
                    var episode = MapToEpisode(tvdbEpisode, tvdbId);
                    if (episode != null)
                    {
                        episodes.Add(episode);
                    }
                }

                nextPage = response.Links?.Next;
                page++;

                // Safety limit to prevent infinite loops
                if (page > 100)
                {
                    _logger.LogWarning("Reached page limit (100) fetching episodes for series {TvdbId}", tvdbId);
                    break;
                }
            }
            while (!string.IsNullOrEmpty(nextPage));

            _logger.LogDebug("Fetched {Count} episodes for series {TvdbId}", episodes.Count, tvdbId);
            return episodes;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error fetching episodes from TVDB for series: {TvdbId}", tvdbId);
            return [];
        }
    }

    public async Task<List<Series>> SearchSeriesAsync(string query, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching TVDB for: {Query}", query);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("TVDB API key not configured");
            return [];
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var endpoint = $"/v4/search?query={encodedQuery}&type=series";
            var response = await _client.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(endpoint, ct);

            if (response?.Data == null || response.Data.Count == 0)
            {
                _logger.LogDebug("No search results from TVDB for: {Query}", query);
                return [];
            }

            var results = new List<Series>();
            foreach (var result in response.Data)
            {
                var series = MapSearchResultToSeries(result);
                if (series != null)
                {
                    results.Add(series);
                }
            }

            _logger.LogDebug("Found {Count} series matching: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error searching TVDB for: {Query}", query);
            return [];
        }
    }

    private static Series MapToSeries(TVDBSeries tvdbSeries)
    {
        var series = new Series
        {
            TvdbId = tvdbSeries.Id,
            Title = tvdbSeries.Name ?? "Unknown",
            Overview = tvdbSeries.Overview,
            Status = tvdbSeries.Status?.Name ?? "Unknown",
            Network = tvdbSeries.OriginalNetwork?.Name,
            Runtime = tvdbSeries.AverageRuntime,
            Year = ParseYear(tvdbSeries.Year) ?? ParseYearFromDate(tvdbSeries.FirstAired)
        };

        // Parse first aired date
        if (!string.IsNullOrWhiteSpace(tvdbSeries.FirstAired) &&
            DateTime.TryParse(tvdbSeries.FirstAired, out var firstAired))
        {
            series.FirstAired = firstAired;
        }

        // Map genres
        if (tvdbSeries.Genres != null)
        {
            series.Genres = tvdbSeries.Genres
                .Where(g => !string.IsNullOrWhiteSpace(g.Name))
                .Select(g => g.Name!)
                .ToList();
        }

        // Map images (prefer poster artwork type = 2)
        if (tvdbSeries.Artworks != null)
        {
            series.Images = tvdbSeries.Artworks
                .Where(a => !string.IsNullOrWhiteSpace(a.Image))
                .OrderByDescending(a => a.Type == 2) // Poster first
                .ThenByDescending(a => a.Score ?? 0)
                .Select(a => a.Image!)
                .Take(5)
                .ToList();
        }

        // Try to get poster from main image if no artworks
        if (series.Images.Count == 0 && !string.IsNullOrWhiteSpace(tvdbSeries.Image))
        {
            series.Images.Add(tvdbSeries.Image);
        }

        // Map remote IDs (IMDb, TMDb)
        if (tvdbSeries.RemoteIds != null)
        {
            foreach (var remoteId in tvdbSeries.RemoteIds)
            {
                if (string.IsNullOrWhiteSpace(remoteId.Id))
                {
                    continue;
                }

                // Type 2 = IMDb
                if (remoteId.Type == 2 || string.Equals(remoteId.SourceName, "IMDB", StringComparison.OrdinalIgnoreCase))
                {
                    series.ImdbId = remoteId.Id;
                }
                // Type 12 = TheMovieDB.com
                else if (remoteId.Type == 12 || string.Equals(remoteId.SourceName, "TheMovieDB.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(remoteId.Id, out var tmdbId))
                    {
                        series.TmdbId = tmdbId;
                    }
                }
            }
        }

        return series;
    }

    private static Episode? MapToEpisode(TVDBEpisode tvdbEpisode, int seriesTvdbId)
    {
        // Skip specials (season 0) that don't have a valid episode number
        if (tvdbEpisode.SeasonNumber == 0 && tvdbEpisode.Number == 0)
        {
            return null;
        }

        var episode = new Episode
        {
            SeasonNumber = tvdbEpisode.SeasonNumber,
            EpisodeNumber = tvdbEpisode.Number,
            AbsoluteEpisodeNumber = tvdbEpisode.AbsoluteNumber,
            Title = tvdbEpisode.Name,
            Overview = tvdbEpisode.Overview
        };

        // Parse air date
        if (!string.IsNullOrWhiteSpace(tvdbEpisode.Aired) &&
            DateTime.TryParse(tvdbEpisode.Aired, out var airDate))
        {
            episode.AirDate = airDate;
            episode.AirDateUtc = DateTime.SpecifyKind(airDate, DateTimeKind.Utc);
        }

        return episode;
    }

    private static Series? MapSearchResultToSeries(TVDBSearchResult result)
    {
        // Only process series type results
        if (!string.Equals(result.Type, "series", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(result.PrimaryType, "series", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Parse TVDB ID from various fields
        int? tvdbId = null;
        if (!string.IsNullOrWhiteSpace(result.TvdbId) && int.TryParse(result.TvdbId, out var id1))
        {
            tvdbId = id1;
        }
        else if (!string.IsNullOrWhiteSpace(result.Id) && int.TryParse(result.Id, out var id2))
        {
            tvdbId = id2;
        }
        else if (!string.IsNullOrWhiteSpace(result.ObjectId))
        {
            // ObjectID format: "series-123456"
            var parts = result.ObjectId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var id3))
            {
                tvdbId = id3;
            }
        }

        if (!tvdbId.HasValue)
        {
            return null;
        }

        var series = new Series
        {
            TvdbId = tvdbId.Value,
            Title = result.Name ?? "Unknown",
            Overview = result.Overview,
            Status = result.Status ?? "Unknown",
            Network = result.Network,
            Year = ParseYear(result.Year) ?? ParseYearFromDate(result.FirstAirTime)
        };

        // Parse first aired date
        if (!string.IsNullOrWhiteSpace(result.FirstAirTime) &&
            DateTime.TryParse(result.FirstAirTime, out var firstAired))
        {
            series.FirstAired = firstAired;
        }

        // Add image
        if (!string.IsNullOrWhiteSpace(result.ImageUrl))
        {
            series.Images.Add(result.ImageUrl);
        }
        else if (!string.IsNullOrWhiteSpace(result.Thumbnail))
        {
            series.Images.Add(result.Thumbnail);
        }

        // Map remote IDs (IMDb, TMDb)
        if (result.RemoteIds != null)
        {
            foreach (var remoteId in result.RemoteIds)
            {
                if (string.IsNullOrWhiteSpace(remoteId.Id))
                {
                    continue;
                }

                if (remoteId.Type == 2 || string.Equals(remoteId.SourceName, "IMDB", StringComparison.OrdinalIgnoreCase))
                {
                    series.ImdbId = remoteId.Id;
                }
                else if (remoteId.Type == 12 || string.Equals(remoteId.SourceName, "TheMovieDB.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(remoteId.Id, out var tmdbId))
                    {
                        series.TmdbId = tmdbId;
                    }
                }
            }
        }

        return series;
    }

    private static int ParseYear(string? yearStr)
    {
        if (string.IsNullOrWhiteSpace(yearStr))
        {
            return 0;
        }

        return int.TryParse(yearStr, out var year) ? year : 0;
    }

    private static int ParseYearFromDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return 0;
        }

        return DateTime.TryParse(dateStr, out var date) ? date.Year : 0;
    }
}
