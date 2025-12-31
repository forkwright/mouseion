// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Dapper;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.BookSeries;

public interface IBookSeriesRepository : IBasicRepository<BookSeries>
{
    BookSeries? FindByTitle(string title);
    BookSeries? FindByForeignId(string foreignSeriesId);
    List<BookSeries> GetByAuthorId(int authorId);
    List<BookSeries> GetMonitored();
}

public class BookSeriesRepository : BasicRepository<BookSeries>, IBookSeriesRepository
{
    public BookSeriesRepository(IDatabase database)
        : base(database)
    {
    }

    public BookSeries? FindByTitle(string title)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<BookSeries>(
            $"SELECT * FROM \"{_table}\" WHERE \"Title\" = @Title",
            new { Title = title });
    }

    public BookSeries? FindByForeignId(string foreignSeriesId)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<BookSeries>(
            $"SELECT * FROM \"{_table}\" WHERE \"ForeignSeriesId\" = @ForeignSeriesId",
            new { ForeignSeriesId = foreignSeriesId });
    }

    public List<BookSeries> GetByAuthorId(int authorId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<BookSeries>(
            $"SELECT * FROM \"{_table}\" WHERE \"AuthorId\" = @AuthorId",
            new { AuthorId = authorId }).ToList();
    }

    public List<BookSeries> GetMonitored()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<BookSeries>(
            $"SELECT * FROM \"{_table}\" WHERE \"Monitored\" = @Monitored",
            new { Monitored = true }).ToList();
    }
}
