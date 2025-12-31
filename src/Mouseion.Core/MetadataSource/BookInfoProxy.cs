// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Mouseion.Core.Books;

namespace Mouseion.Core.MetadataSource;

/// <summary>
/// OpenLibrary metadata provider for books
/// </summary>
public class BookInfoProxy : IProvideBookInfo
{
    private const string BaseUrl = "https://openlibrary.org";
    private const string SearchUrl = "https://openlibrary.org/search.json";
    private const string UserAgent = "Mouseion/1.0 (https://github.com/forkwright/mouseion)";

    private readonly ILogger<BookInfoProxy> _logger;

    public BookInfoProxy(ILogger<BookInfoProxy> logger)
    {
        _logger = logger;
    }

    public Book? GetByExternalId(string externalId)
    {
        _logger.LogDebug("GetByExternalId called for: {ExternalId}", externalId);
        // TODO: Implement OpenLibrary work lookup
        return null;
    }

    public Book? GetById(int id)
    {
        _logger.LogDebug("GetById called for: {Id} (OpenLibrary uses string IDs)", id);
        return null;
    }

    public List<Book> SearchByTitle(string title)
    {
        _logger.LogDebug("SearchByTitle called for: {Title}", title);
        // TODO: Implement OpenLibrary title search
        return new List<Book>();
    }

    public List<Book> SearchByAuthor(string author)
    {
        _logger.LogDebug("SearchByAuthor called for: {Author}", author);
        // TODO: Implement OpenLibrary author search
        return new List<Book>();
    }

    public List<Book> SearchByIsbn(string isbn)
    {
        _logger.LogDebug("SearchByIsbn called for: {Isbn}", isbn);
        // TODO: Implement OpenLibrary ISBN lookup
        return new List<Book>();
    }

    public List<Book> GetTrending()
    {
        _logger.LogDebug("GetTrending called");
        // TODO: Implement OpenLibrary trending books
        return new List<Book>();
    }

    public List<Book> GetPopular()
    {
        _logger.LogDebug("GetPopular called");
        // TODO: Implement OpenLibrary popular books
        return new List<Book>();
    }
}
