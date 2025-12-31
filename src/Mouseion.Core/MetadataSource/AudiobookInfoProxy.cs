// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.Audiobooks;

namespace Mouseion.Core.MetadataSource;

/// <summary>
/// Audnexus metadata provider for audiobooks
/// </summary>
public class AudiobookInfoProxy : IProvideAudiobookInfo
{
    private const string BaseUrl = "https://api.audnex.us";
    private const string UserAgent = "Mouseion/1.0 (https://github.com/forkwright/mouseion)";

    private readonly ILogger<AudiobookInfoProxy> _logger;

    public AudiobookInfoProxy(ILogger<AudiobookInfoProxy> logger)
    {
        _logger = logger;
    }

    public Audiobook? GetByAsin(string asin)
    {
        _logger.LogDebug("GetByAsin called for: {Asin}", asin);
        // TODO: Implement Audnexus ASIN lookup
        return null;
    }

    public Audiobook? GetById(int id)
    {
        _logger.LogDebug("GetById called for: {Id}", id);
        return null;
    }

    public List<Audiobook> SearchByTitle(string title)
    {
        _logger.LogDebug("SearchByTitle called for: {Title}", title);
        // TODO: Implement Audnexus title search
        return new List<Audiobook>();
    }

    public List<Audiobook> SearchByAuthor(string author)
    {
        _logger.LogDebug("SearchByAuthor called for: {Author}", author);
        // TODO: Implement Audnexus author search
        return new List<Audiobook>();
    }

    public List<Audiobook> SearchByNarrator(string narrator)
    {
        _logger.LogDebug("SearchByNarrator called for: {Narrator}", narrator);
        // TODO: Implement Audnexus narrator search
        return new List<Audiobook>();
    }

    public List<Audiobook> GetTrending()
    {
        _logger.LogDebug("GetTrending called");
        // TODO: Implement Audnexus trending audiobooks
        return new List<Audiobook>();
    }

    public List<Audiobook> GetPopular()
    {
        _logger.LogDebug("GetPopular called");
        // TODO: Implement Audnexus popular audiobooks
        return new List<Audiobook>();
    }
}
