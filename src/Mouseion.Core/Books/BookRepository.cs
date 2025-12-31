// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Dapper;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.Books;

public interface IBookRepository : IBasicRepository<Book>
{
    Book? FindByTitle(string title, int year);
    List<Book> GetByAuthorId(int authorId);
    List<Book> GetBySeriesId(int seriesId);
    List<Book> GetMonitored();
    bool BookExists(int authorId, string title, int year);
}

public class BookRepository : BasicRepository<Book>, IBookRepository
{
    public BookRepository(IDatabase database)
        : base(database)
    {
    }

    public Book? FindByTitle(string title, int year)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 4",
            new { Title = title, Year = year });
    }

    public List<Book> GetByAuthorId(int authorId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"MediaType\" = 4",
            new { AuthorId = authorId }).ToList();
    }

    public List<Book> GetBySeriesId(int seriesId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"BookSeriesId\" = @SeriesId AND \"MediaType\" = 4",
            new { SeriesId = seriesId }).ToList();
    }

    public List<Book> GetMonitored()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Monitored\" = @Monitored AND \"MediaType\" = 4",
            new { Monitored = true }).ToList();
    }

    public bool BookExists(int authorId, string title, int year)
    {
        using var conn = _database.OpenConnection();
        var count = conn.QuerySingle<int>(
            "SELECT COUNT(*) FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 4",
            new { AuthorId = authorId, Title = title, Year = year });
        return count > 0;
    }
}
