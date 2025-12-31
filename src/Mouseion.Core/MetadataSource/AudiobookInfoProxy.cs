// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mouseion.Common.Http;
using Mouseion.Core.Audiobooks;

namespace Mouseion.Core.MetadataSource;

/// <summary>
/// Audnexus metadata provider for audiobooks
/// </summary>
public class AudiobookInfoProxy : IProvideAudiobookInfo
{
    private const string BaseUrl = "https://api.audnex.us";
    private const string BooksUrl = "https://api.audnex.us/books";
    private const string UserAgent = "Mouseion/1.0 (https://github.com/forkwright/mouseion)";

    private readonly IHttpClient _httpClient;
    private readonly ILogger<AudiobookInfoProxy> _logger;

    public AudiobookInfoProxy(IHttpClient httpClient, ILogger<AudiobookInfoProxy> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Audiobook? GetByAsin(string asin)
    {
        try
        {
            _logger.LogDebug("Fetching audiobook by ASIN: {Asin}", asin);

            var url = $"{BooksUrl}/{asin}";
            var request = new HttpRequestBuilder(url)
                .SetHeader("User-Agent", UserAgent)
                .Build();

            var response = _httpClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning("Audnexus returned {StatusCode} for ASIN {Asin}", response.StatusCode, asin);
                return null;
            }

            return ParseAudiobook(response.Content, asin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching audiobook by ASIN: {Asin}", asin);
            return null;
        }
    }

    public Audiobook? GetById(int id)
    {
        _logger.LogDebug("GetById not supported by Audnexus (uses ASIN)");
        return null;
    }

    public List<Audiobook> SearchByTitle(string title)
    {
        try
        {
            _logger.LogDebug("Searching audiobooks by title: {Title}", title);

            var url = $"{BooksUrl}?title={Uri.EscapeDataString(title)}";
            var request = new HttpRequestBuilder(url)
                .SetHeader("User-Agent", UserAgent)
                .Build();

            var response = _httpClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning("Audnexus search returned {StatusCode}", response.StatusCode);
                return new List<Audiobook>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audiobooks by title: {Title}", title);
            return new List<Audiobook>();
        }
    }

    public List<Audiobook> SearchByAuthor(string author)
    {
        try
        {
            _logger.LogDebug("Searching audiobooks by author: {Author}", author);

            var url = $"{BooksUrl}?author={Uri.EscapeDataString(author)}";
            var request = new HttpRequestBuilder(url)
                .SetHeader("User-Agent", UserAgent)
                .Build();

            var response = _httpClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning("Audnexus search returned {StatusCode}", response.StatusCode);
                return new List<Audiobook>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audiobooks by author: {Author}", author);
            return new List<Audiobook>();
        }
    }

    public List<Audiobook> SearchByNarrator(string narrator)
    {
        try
        {
            _logger.LogDebug("Searching audiobooks by narrator: {Narrator}", narrator);

            var url = $"{BooksUrl}?narrator={Uri.EscapeDataString(narrator)}";
            var request = new HttpRequestBuilder(url)
                .SetHeader("User-Agent", UserAgent)
                .Build();

            var response = _httpClient.Get(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                _logger.LogWarning("Audnexus search returned {StatusCode}", response.StatusCode);
                return new List<Audiobook>();
            }

            return ParseSearchResults(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audiobooks by narrator: {Narrator}", narrator);
            return new List<Audiobook>();
        }
    }

    public List<Audiobook> GetTrending()
    {
        _logger.LogDebug("GetTrending not supported by Audnexus");
        return new List<Audiobook>();
    }

    public List<Audiobook> GetPopular()
    {
        _logger.LogDebug("GetPopular not supported by Audnexus");
        return new List<Audiobook>();
    }

    private Audiobook? ParseAudiobook(string json, string asin)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var audiobook = new Audiobook
            {
                Title = root.GetProperty("title").GetString() ?? "Unknown",
                Year = ExtractYear(root),
                Metadata = new AudiobookMetadata
                {
                    Description = GetStringProperty(root, "summary"),
                    ForeignAudiobookId = asin,
                    AudnexusId = asin,
                    AudibleId = asin,
                    Asin = asin,
                    ReleaseDate = ExtractDate(root),
                    Publisher = GetStringProperty(root, "publisher"),
                    Language = GetStringProperty(root, "language"),
                    Narrator = GetStringProperty(root, "narrator"),
                    Narrators = GetStringArrayProperty(root, "narrators"),
                    DurationMinutes = ExtractDuration(root),
                    IsAbridged = GetBoolProperty(root, "isAbridged"),
                    Genres = GetStringArrayProperty(root, "genres")
                }
            };

            return audiobook;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Audnexus audiobook JSON");
            return null;
        }
    }

    private List<Audiobook> ParseSearchResults(string json)
    {
        var audiobooks = new List<Audiobook>();

        try
        {
            using var doc = JsonDocument.Parse(json);

            // Handle both array and object responses
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var audiobook = ParseSearchResult(item);
                    if (audiobook != null)
                    {
                        audiobooks.Add(audiobook);
                    }
                }
            }
            else if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    var audiobook = ParseSearchResult(item);
                    if (audiobook != null)
                    {
                        audiobooks.Add(audiobook);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Audnexus search results");
        }

        return audiobooks;
    }

    private Audiobook? ParseSearchResult(JsonElement item)
    {
        try
        {
            var asin = GetStringProperty(item, "asin");
            if (string.IsNullOrWhiteSpace(asin))
            {
                return null;
            }

            var audiobook = new Audiobook
            {
                Title = item.GetProperty("title").GetString() ?? "Unknown",
                Year = ExtractYear(item),
                Metadata = new AudiobookMetadata
                {
                    ForeignAudiobookId = asin,
                    AudnexusId = asin,
                    AudibleId = asin,
                    Asin = asin,
                    Description = GetStringProperty(item, "summary"),
                    ReleaseDate = ExtractDate(item),
                    Publisher = GetStringProperty(item, "publisher"),
                    Language = GetStringProperty(item, "language"),
                    Narrator = GetStringProperty(item, "narrator"),
                    Narrators = GetStringArrayProperty(item, "narrators"),
                    DurationMinutes = ExtractDuration(item),
                    IsAbridged = GetBoolProperty(item, "isAbridged"),
                    Genres = GetStringArrayProperty(item, "genres")
                }
            };

            return audiobook;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing individual search result");
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static bool GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            else if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }
        }

        return false;
    }

    private static List<string> GetStringArrayProperty(JsonElement element, string propertyName)
    {
        var result = new List<string>();

        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        result.Add(value);
                    }
                }
            }
        }

        return result;
    }

    private static int ExtractYear(JsonElement element)
    {
        var releaseDate = ExtractDate(element);
        return releaseDate?.Year ?? 0;
    }

    private static DateTime? ExtractDate(JsonElement element)
    {
        var dateStr = GetStringProperty(element, "releaseDate");
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            dateStr = GetStringProperty(element, "publishDate");
        }

        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        if (DateTime.TryParse(dateStr, out var date))
        {
            return date;
        }

        return null;
    }

    private static int? ExtractDuration(JsonElement element)
    {
        if (element.TryGetProperty("runtimeLengthMin", out var runtime) && runtime.ValueKind == JsonValueKind.Number)
        {
            return runtime.GetInt32();
        }

        if (element.TryGetProperty("duration", out var duration) && duration.ValueKind == JsonValueKind.Number)
        {
            return duration.GetInt32();
        }

        return null;
    }
}
