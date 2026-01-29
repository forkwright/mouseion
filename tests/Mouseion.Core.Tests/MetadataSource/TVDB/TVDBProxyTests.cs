// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Moq;
using Mouseion.Core.MetadataSource.TVDB;

namespace Mouseion.Core.Tests.MetadataSource.TVDB;

public class TVDBProxyTests
{
    private readonly Mock<ITVDBClient> _mockClient;
    private readonly TVDBSettings _settings;
    private readonly TVDBProxy _proxy;

    public TVDBProxyTests()
    {
        _mockClient = new Mock<ITVDBClient>();
        _settings = new TVDBSettings { ApiKey = "test-api-key" };
        var logger = Mock.Of<ILogger<TVDBProxy>>();
        _proxy = new TVDBProxy(_settings, logger, _mockClient.Object);
    }

    #region GetSeriesByTvdbIdAsync Tests

    [Fact]
    public async Task GetSeriesByTvdbIdAsync_returns_null_when_api_key_not_configured()
    {
        var settingsNoKey = new TVDBSettings { ApiKey = null };
        var proxy = new TVDBProxy(settingsNoKey, Mock.Of<ILogger<TVDBProxy>>(), _mockClient.Object);

        var result = await proxy.GetSeriesByTvdbIdAsync(12345);

        Assert.Null(result);
        _mockClient.Verify(c => c.GetAsync<TVDBResponse<TVDBSeries>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSeriesByTvdbIdAsync_returns_null_when_api_returns_null()
    {
        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeries>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TVDBResponse<TVDBSeries>?)null);

        var result = await proxy.GetSeriesByTvdbIdAsync(12345);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeriesByTvdbIdAsync_maps_series_data_correctly()
    {
        var tvdbResponse = new TVDBResponse<TVDBSeries>
        {
            Status = "success",
            Data = new TVDBSeries
            {
                Id = 81189,
                Name = "Breaking Bad",
                Overview = "A high school chemistry teacher diagnosed with inoperable lung cancer",
                Year = "2008",
                FirstAired = "2008-01-20",
                AverageRuntime = 45,
                Status = new TVDBStatus { Name = "Ended" },
                OriginalNetwork = new TVDBNetwork { Name = "AMC" },
                Genres = new List<TVDBGenre>
                {
                    new() { Name = "Drama" },
                    new() { Name = "Crime" }
                },
                Artworks = new List<TVDBArtwork>
                {
                    new() { Image = "https://artworks.thetvdb.com/banners/posters/81189-1.jpg", Type = 2, Score = 100 }
                },
                RemoteIds = new List<TVDBRemoteId>
                {
                    new() { Id = "tt0903747", Type = 2, SourceName = "IMDB" },
                    new() { Id = "1396", Type = 12, SourceName = "TheMovieDB.com" }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeries>>("/v4/series/81189/extended", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.GetSeriesByTvdbIdAsync(81189);

        Assert.NotNull(result);
        Assert.Equal(81189, result.TvdbId);
        Assert.Equal("Breaking Bad", result.Title);
        Assert.Contains("lung cancer", result.Overview ?? "");
        Assert.Equal(2008, result.Year);
        Assert.Equal("Ended", result.Status);
        Assert.Equal("AMC", result.Network);
        Assert.Equal(45, result.Runtime);
        Assert.Contains("Drama", result.Genres);
        Assert.Contains("Crime", result.Genres);
        Assert.Equal("tt0903747", result.ImdbId);
        Assert.Equal(1396, result.TmdbId);
        Assert.NotEmpty(result.Images);
    }

    [Fact]
    public async Task GetSeriesByTvdbIdAsync_handles_missing_optional_fields()
    {
        var tvdbResponse = new TVDBResponse<TVDBSeries>
        {
            Status = "success",
            Data = new TVDBSeries
            {
                Id = 12345,
                Name = "Test Show"
            }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeries>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.GetSeriesByTvdbIdAsync(12345);

        Assert.NotNull(result);
        Assert.Equal(12345, result.TvdbId);
        Assert.Equal("Test Show", result.Title);
        Assert.Null(result.Overview);
        Assert.Equal("Unknown", result.Status);
    }

    #endregion

    #region GetEpisodesBySeriesIdAsync Tests

    [Fact]
    public async Task GetEpisodesBySeriesIdAsync_returns_empty_when_api_key_not_configured()
    {
        var settingsNoKey = new TVDBSettings { ApiKey = "" };
        var proxy = new TVDBProxy(settingsNoKey, Mock.Of<ILogger<TVDBProxy>>(), _mockClient.Object);

        var result = await proxy.GetEpisodesBySeriesIdAsync(12345);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetEpisodesBySeriesIdAsync_returns_mapped_episodes()
    {
        var tvdbResponse = new TVDBResponse<TVDBSeriesEpisodesData>
        {
            Status = "success",
            Data = new TVDBSeriesEpisodesData
            {
                Episodes = new List<TVDBEpisode>
                {
                    new()
                    {
                        Id = 1,
                        SeriesId = 81189,
                        Name = "Pilot",
                        SeasonNumber = 1,
                        Number = 1,
                        AbsoluteNumber = 1,
                        Aired = "2008-01-20",
                        Overview = "A chemistry teacher receives a terminal diagnosis."
                    },
                    new()
                    {
                        Id = 2,
                        SeriesId = 81189,
                        Name = "Cat's in the Bag...",
                        SeasonNumber = 1,
                        Number = 2,
                        AbsoluteNumber = 2,
                        Aired = "2008-01-27"
                    }
                }
            },
            Links = new TVDBLinks { Next = null }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeriesEpisodesData>>("/v4/series/81189/episodes/default?page=0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.GetEpisodesBySeriesIdAsync(81189);

        Assert.Equal(2, result.Count);
        Assert.Equal("Pilot", result[0].Title);
        Assert.Equal(1, result[0].SeasonNumber);
        Assert.Equal(1, result[0].EpisodeNumber);
        Assert.Equal(1, result[0].AbsoluteEpisodeNumber);
        Assert.Equal(new DateTime(2008, 1, 20), result[0].AirDate);
        Assert.Equal("Cat's in the Bag...", result[1].Title);
    }

    [Fact]
    public async Task GetEpisodesBySeriesIdAsync_handles_pagination()
    {
        var page0Response = new TVDBResponse<TVDBSeriesEpisodesData>
        {
            Status = "success",
            Data = new TVDBSeriesEpisodesData
            {
                Episodes = new List<TVDBEpisode>
                {
                    new() { Id = 1, Name = "Episode 1", SeasonNumber = 1, Number = 1 }
                }
            },
            Links = new TVDBLinks { Next = "/v4/series/12345/episodes/default?page=1" }
        };

        var page1Response = new TVDBResponse<TVDBSeriesEpisodesData>
        {
            Status = "success",
            Data = new TVDBSeriesEpisodesData
            {
                Episodes = new List<TVDBEpisode>
                {
                    new() { Id = 2, Name = "Episode 2", SeasonNumber = 1, Number = 2 }
                }
            },
            Links = new TVDBLinks { Next = null }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeriesEpisodesData>>("/v4/series/12345/episodes/default?page=0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page0Response);

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeriesEpisodesData>>("/v4/series/12345/episodes/default?page=1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(page1Response);

        var result = await proxy.GetEpisodesBySeriesIdAsync(12345);

        Assert.Equal(2, result.Count);
        Assert.Equal("Episode 1", result[0].Title);
        Assert.Equal("Episode 2", result[1].Title);
    }

    [Fact]
    public async Task GetEpisodesBySeriesIdAsync_skips_invalid_specials()
    {
        var tvdbResponse = new TVDBResponse<TVDBSeriesEpisodesData>
        {
            Status = "success",
            Data = new TVDBSeriesEpisodesData
            {
                Episodes = new List<TVDBEpisode>
                {
                    new() { Id = 1, Name = "Valid Episode", SeasonNumber = 1, Number = 1 },
                    new() { Id = 2, Name = "Invalid Special", SeasonNumber = 0, Number = 0 }
                }
            },
            Links = new TVDBLinks { Next = null }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<TVDBSeriesEpisodesData>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.GetEpisodesBySeriesIdAsync(12345);

        Assert.Single(result);
        Assert.Equal("Valid Episode", result[0].Title);
    }

    #endregion

    #region SearchSeriesAsync Tests

    [Fact]
    public async Task SearchSeriesAsync_returns_empty_when_api_key_not_configured()
    {
        var settingsNoKey = new TVDBSettings { ApiKey = "  " };
        var proxy = new TVDBProxy(settingsNoKey, Mock.Of<ILogger<TVDBProxy>>(), _mockClient.Object);

        var result = await proxy.SearchSeriesAsync("test");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSeriesAsync_returns_empty_for_empty_query()
    {
        var result = await proxy.SearchSeriesAsync("");

        Assert.Empty(result);
        _mockClient.Verify(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SearchSeriesAsync_maps_search_results_correctly()
    {
        var tvdbResponse = new TVDBResponse<List<TVDBSearchResult>>
        {
            Status = "success",
            Data = new List<TVDBSearchResult>
            {
                new()
                {
                    ObjectId = "series-81189",
                    TvdbId = "81189",
                    Name = "Breaking Bad",
                    Overview = "A teacher turns to crime",
                    Year = "2008",
                    Type = "series",
                    Status = "Ended",
                    Network = "AMC",
                    ImageUrl = "https://artworks.thetvdb.com/banners/posters/81189.jpg",
                    RemoteIds = new List<TVDBRemoteId>
                    {
                        new() { Id = "tt0903747", Type = 2 }
                    }
                },
                new()
                {
                    ObjectId = "movie-12345",
                    Name = "Breaking Bad: El Camino",
                    Type = "movie"  // Should be filtered out
                }
            }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>("/v4/search?query=Breaking%20Bad&type=series", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.SearchSeriesAsync("Breaking Bad");

        Assert.Single(result);
        Assert.Equal(81189, result[0].TvdbId);
        Assert.Equal("Breaking Bad", result[0].Title);
        Assert.Equal("Ended", result[0].Status);
        Assert.Equal("AMC", result[0].Network);
        Assert.Equal("tt0903747", result[0].ImdbId);
    }

    [Fact]
    public async Task SearchSeriesAsync_url_encodes_query()
    {
        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TVDBResponse<List<TVDBSearchResult>> { Data = new List<TVDBSearchResult>() });

        await proxy.SearchSeriesAsync("The Office (US)");

        _mockClient.Verify(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(
            "/v4/search?query=The%20Office%20%28US%29&type=series",
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SearchSeriesAsync_handles_missing_tvdb_id_gracefully()
    {
        var tvdbResponse = new TVDBResponse<List<TVDBSearchResult>>
        {
            Status = "success",
            Data = new List<TVDBSearchResult>
            {
                new()
                {
                    Name = "No ID Show",
                    Type = "series"
                    // No TvdbId, Id, or ObjectId set
                }
            }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.SearchSeriesAsync("test");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSeriesAsync_parses_id_from_object_id()
    {
        var tvdbResponse = new TVDBResponse<List<TVDBSearchResult>>
        {
            Status = "success",
            Data = new List<TVDBSearchResult>
            {
                new()
                {
                    ObjectId = "series-99999",
                    Name = "Test Show",
                    Type = "series"
                }
            }
        };

        _mockClient
            .Setup(c => c.GetAsync<TVDBResponse<List<TVDBSearchResult>>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tvdbResponse);

        var result = await proxy.SearchSeriesAsync("test");

        Assert.Single(result);
        Assert.Equal(99999, result[0].TvdbId);
    }

    #endregion

    private TVDBProxy proxy => _proxy;
}
