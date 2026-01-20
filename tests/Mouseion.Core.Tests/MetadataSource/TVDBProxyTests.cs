// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Reflection;
using System.Text.Json;
using Mouseion.Core.MetadataSource.TVDB;
using Mouseion.Core.TV;

namespace Mouseion.Core.Tests.MetadataSource;

public class TVDBProxyTests
{
    private static Series? InvokeMapToSeries(object tvdbSeries)
    {
        var method = typeof(TVDBProxy).GetMethod(
            "MapToSeries",
            BindingFlags.NonPublic | BindingFlags.Static);
        return method?.Invoke(null, new[] { tvdbSeries }) as Series;
    }

    private static Episode? InvokeMapToEpisode(object tvdbEpisode, int seriesId)
    {
        var method = typeof(TVDBProxy).GetMethod(
            "MapToEpisode",
            BindingFlags.NonPublic | BindingFlags.Static);
        return method?.Invoke(null, new[] { tvdbEpisode, seriesId }) as Episode;
    }

    private static string InvokeMapStatus(string? status)
    {
        var method = typeof(TVDBProxy).GetMethod(
            "MapStatus",
            BindingFlags.NonPublic | BindingFlags.Static);
        return method?.Invoke(null, new object?[] { status }) as string ?? "Continuing";
    }

    [Fact]
    public void MapStatus_ReturnsCorrectMappings()
    {
        Assert.Equal("Continuing", InvokeMapStatus("continuing"));
        Assert.Equal("Continuing", InvokeMapStatus("Continuing"));
        Assert.Equal("Ended", InvokeMapStatus("ended"));
        Assert.Equal("Ended", InvokeMapStatus("Ended"));
        Assert.Equal("Upcoming", InvokeMapStatus("upcoming"));
        Assert.Equal("Upcoming", InvokeMapStatus("Upcoming"));
        Assert.Equal("Continuing", InvokeMapStatus(null));
        Assert.Equal("Continuing", InvokeMapStatus("unknown"));
    }

    [Fact]
    public void TVDBSettings_HasCorrectDefaults()
    {
        var settings = new TVDBSettings();

        Assert.Equal("https://api4.thetvdb.com/v4", settings.ApiUrl);
        Assert.Null(settings.ApiKey);
        Assert.Null(settings.Pin);
        Assert.Equal(30, settings.TimeoutSeconds);
        Assert.Equal(3, settings.MaxRetries);
        Assert.Equal(20, settings.TokenRefreshMinutes);
    }

    [Fact]
    public void TVDBResponseModel_DeserializesCorrectly()
    {
        var json = """
        {
            "status": "success",
            "data": {
                "token": "test-token-123"
            }
        }
        """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<TestLoginResult>>(json, options);

        Assert.NotNull(response);
        Assert.Equal("success", response.Status);
        Assert.NotNull(response.Data);
        Assert.Equal("test-token-123", response.Data.Token);
    }

    [Fact]
    public void TVDBSeriesResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "status": "success",
            "data": {
                "id": 73739,
                "name": "Lost",
                "overview": "A plane crashes on a mysterious island.",
                "firstAired": "2004-09-22",
                "status": {
                    "id": 2,
                    "name": "Ended"
                },
                "originalNetwork": {
                    "id": 2,
                    "name": "ABC"
                },
                "genres": [
                    {"id": 3, "name": "Drama"},
                    {"id": 4, "name": "Mystery"}
                ]
            }
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<TestTVDBSeries>>(json, options);

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal(73739, response.Data.Id);
        Assert.Equal("Lost", response.Data.Name);
        Assert.NotNull(response.Data.Status);
        Assert.Equal("Ended", response.Data.Status.Name);
        Assert.NotNull(response.Data.OriginalNetwork);
        Assert.Equal("ABC", response.Data.OriginalNetwork.Name);
        Assert.NotNull(response.Data.Genres);
        Assert.Equal(2, response.Data.Genres.Count);
    }

    [Fact]
    public void TVDBEpisodeResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "status": "success",
            "data": {
                "series": {
                    "id": 73739,
                    "name": "Lost"
                },
                "episodes": [
                    {
                        "id": 127131,
                        "seriesId": 73739,
                        "name": "Pilot (1)",
                        "aired": "2004-09-22",
                        "runtime": 42,
                        "overview": "The survivors of a plane crash...",
                        "number": 1,
                        "seasonNumber": 1,
                        "absoluteNumber": 1
                    },
                    {
                        "id": 127132,
                        "seriesId": 73739,
                        "name": "Pilot (2)",
                        "aired": "2004-09-29",
                        "number": 2,
                        "seasonNumber": 1,
                        "absoluteNumber": 2
                    }
                ]
            },
            "links": {
                "prev": null,
                "self": "https://api4.thetvdb.com/v4/series/73739/episodes/default?page=0",
                "next": null
            }
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<TestTVDBEpisodesResult>>(json, options);

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.NotNull(response.Data.Episodes);
        Assert.Equal(2, response.Data.Episodes.Count);

        var ep1 = response.Data.Episodes[0];
        Assert.Equal(127131, ep1.Id);
        Assert.Equal("Pilot (1)", ep1.Name);
        Assert.Equal(1, ep1.SeasonNumber);
        Assert.Equal(1, ep1.Number);
        Assert.Equal(1, ep1.AbsoluteNumber);
        Assert.Equal("2004-09-22", ep1.Aired);
    }

    [Fact]
    public void TVDBSearchResponse_DeserializesCorrectly()
    {
        var json = """
        {
            "status": "success",
            "data": [
                {
                    "tvdb_id": "73739",
                    "name": "Lost",
                    "year": "2004",
                    "overview": "A plane crashes on a mysterious island.",
                    "status": "Ended",
                    "network": "ABC",
                    "image_url": "https://artworks.thetvdb.com/banners/posters/73739-1.jpg"
                }
            ]
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<List<TestTVDBSearchResult>>>(json, options);

        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);

        var result = response.Data[0];
        Assert.Equal("73739", result.TvdbId);
        Assert.Equal("Lost", result.Name);
        Assert.Equal("2004", result.Year);
        Assert.Equal("ABC", result.Network);
    }

    [Fact]
    public void TVDBLinks_DeserializesCorrectly()
    {
        var json = """
        {
            "status": "success",
            "data": null,
            "links": {
                "prev": "https://api4.thetvdb.com/v4/series/73739/episodes?page=0",
                "self": "https://api4.thetvdb.com/v4/series/73739/episodes?page=1",
                "next": "https://api4.thetvdb.com/v4/series/73739/episodes?page=2"
            }
        }
        """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<object>>(json, options);

        Assert.NotNull(response);
        Assert.NotNull(response.Links);
        Assert.NotNull(response.Links.Prev);
        Assert.NotNull(response.Links.Self);
        Assert.NotNull(response.Links.Next);
    }

    [Fact]
    public void TVDBLinks_HandlesNullValues()
    {
        var json = """
        {
            "status": "success",
            "data": null,
            "links": {
                "prev": null,
                "self": "https://api4.thetvdb.com/v4/series/73739/episodes?page=0",
                "next": null
            }
        }
        """;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var response = JsonSerializer.Deserialize<TestTVDBResponse<object>>(json, options);

        Assert.NotNull(response);
        Assert.NotNull(response.Links);
        Assert.Null(response.Links.Prev);
        Assert.NotNull(response.Links.Self);
        Assert.Null(response.Links.Next);
    }
}

#region Test Models (mirror internal types for testing deserialization)

internal class TestTVDBResponse<T>
{
    public string? Status { get; set; }
    public T? Data { get; set; }
    public TestTVDBLinks? Links { get; set; }
}

internal class TestTVDBLinks
{
    public string? Prev { get; set; }
    public string? Self { get; set; }
    public string? Next { get; set; }
}

internal class TestLoginResult
{
    public string? Token { get; set; }
}

internal class TestTVDBSeries
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Overview { get; set; }
    public string? FirstAired { get; set; }
    public TestTVDBStatus? Status { get; set; }
    public TestTVDBNetwork? OriginalNetwork { get; set; }
    public List<TestTVDBGenre>? Genres { get; set; }
}

internal class TestTVDBStatus
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

internal class TestTVDBNetwork
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

internal class TestTVDBGenre
{
    public int? Id { get; set; }
    public string? Name { get; set; }
}

internal class TestTVDBEpisodesResult
{
    public TestTVDBSeries? Series { get; set; }
    public List<TestTVDBEpisode>? Episodes { get; set; }
}

internal class TestTVDBEpisode
{
    public int? Id { get; set; }
    public int? SeriesId { get; set; }
    public string? Name { get; set; }
    public string? Aired { get; set; }
    public int? Runtime { get; set; }
    public string? Overview { get; set; }
    public int? Number { get; set; }
    public int? SeasonNumber { get; set; }
    public int? AbsoluteNumber { get; set; }
}

internal class TestTVDBSearchResult
{
    [System.Text.Json.Serialization.JsonPropertyName("tvdb_id")]
    public string? TvdbId { get; set; }
    public string? Name { get; set; }
    public string? Year { get; set; }
    public string? Overview { get; set; }
    public string? Status { get; set; }
    public string? Network { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

#endregion
