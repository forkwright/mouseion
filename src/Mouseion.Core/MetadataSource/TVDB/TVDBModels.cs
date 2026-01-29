// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json.Serialization;

namespace Mouseion.Core.MetadataSource.TVDB;

/// <summary>
/// TVDB v4 API response wrapper
/// </summary>
/// <typeparam name="T">The data type contained in the response</typeparam>
public class TVDBResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("links")]
    public TVDBLinks? Links { get; set; }
}

public class TVDBLinks
{
    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

/// <summary>
/// TVDB series data from the API
/// </summary>
public class TVDBSeries
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("firstAired")]
    public string? FirstAired { get; set; }

    [JsonPropertyName("lastAired")]
    public string? LastAired { get; set; }

    [JsonPropertyName("nextAired")]
    public string? NextAired { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("status")]
    public TVDBStatus? Status { get; set; }

    [JsonPropertyName("originalCountry")]
    public string? OriginalCountry { get; set; }

    [JsonPropertyName("originalLanguage")]
    public string? OriginalLanguage { get; set; }

    [JsonPropertyName("defaultSeasonType")]
    public int DefaultSeasonType { get; set; }

    [JsonPropertyName("isOrderRandomized")]
    public bool IsOrderRandomized { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("averageRuntime")]
    public int? AverageRuntime { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("artworks")]
    public List<TVDBArtwork>? Artworks { get; set; }

    [JsonPropertyName("genres")]
    public List<TVDBGenre>? Genres { get; set; }

    [JsonPropertyName("remoteIds")]
    public List<TVDBRemoteId>? RemoteIds { get; set; }

    [JsonPropertyName("originalNetwork")]
    public TVDBNetwork? OriginalNetwork { get; set; }

    [JsonPropertyName("seasons")]
    public List<TVDBSeason>? Seasons { get; set; }
}

public class TVDBStatus
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("recordType")]
    public string? RecordType { get; set; }

    [JsonPropertyName("keepUpdated")]
    public bool KeepUpdated { get; set; }
}

public class TVDBArtwork
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

public class TVDBGenre
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
}

public class TVDBRemoteId
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("sourceName")]
    public string? SourceName { get; set; }
}

public class TVDBNetwork
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class TVDBSeason
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    [JsonPropertyName("type")]
    public TVDBSeasonType? Type { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("imageType")]
    public int? ImageType { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }
}

public class TVDBSeasonType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("alternateName")]
    public string? AlternateName { get; set; }
}

/// <summary>
/// TVDB episode data from the API
/// </summary>
public class TVDBEpisode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("aired")]
    public string? Aired { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("imageType")]
    public int? ImageType { get; set; }

    [JsonPropertyName("isMovie")]
    public int IsMovie { get; set; }

    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("absoluteNumber")]
    public int? AbsoluteNumber { get; set; }

    [JsonPropertyName("finaleType")]
    public string? FinaleType { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }
}

/// <summary>
/// TVDB series episodes response wrapper
/// </summary>
public class TVDBSeriesEpisodesData
{
    [JsonPropertyName("series")]
    public TVDBSeries? Series { get; set; }

    [JsonPropertyName("episodes")]
    public List<TVDBEpisode>? Episodes { get; set; }
}

/// <summary>
/// TVDB search result from the API
/// </summary>
public class TVDBSearchResult
{
    [JsonPropertyName("objectID")]
    public string? ObjectId { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("first_air_time")]
    public string? FirstAirTime { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("primary_language")]
    public string? PrimaryLanguage { get; set; }

    [JsonPropertyName("primary_type")]
    public string? PrimaryType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("tvdb_id")]
    public string? TvdbId { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("overviews")]
    public Dictionary<string, string>? Overviews { get; set; }

    [JsonPropertyName("translations")]
    public Dictionary<string, string>? Translations { get; set; }

    [JsonPropertyName("network")]
    public string? Network { get; set; }

    [JsonPropertyName("remote_ids")]
    public List<TVDBRemoteId>? RemoteIds { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }
}
