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
    Task<Book?> FindByTitleAsync(string title, int year, CancellationToken ct = default);
    Task<List<Book>> GetByAuthorIdAsync(int authorId, CancellationToken ct = default);
    Task<List<Book>> GetBySeriesIdAsync(int seriesId, CancellationToken ct = default);
    Task<List<Book>> GetMonitoredAsync(CancellationToken ct = default);
    Task<bool> BookExistsAsync(int authorId, string title, int year, CancellationToken ct = default);

    Book? FindByTitle(string title, int year);
    List<Book> GetByAuthorId(int authorId);
    List<Book> GetBySeriesId(int seriesId);
    List<Book> GetMonitored();
    bool BookExists(int authorId, string title, int year);
}

public class BookRepository : BasicRepository<Book>, IBookRepository
{
    public BookRepository(IDatabase database)
        : base(database, "MediaItems")
    {
    }

    public override async Task<IEnumerable<Book>> AllAsync(CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        return await conn.QueryAsync<Book>("SELECT * FROM \"MediaItems\" WHERE \"MediaType\" = 4").ConfigureAwait(false);
    }

    public override IEnumerable<Book> All()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>("SELECT * FROM \"MediaItems\" WHERE \"MediaType\" = 4");
    }

    public override async Task<Book?> FindAsync(int id, CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Id\" = @Id AND \"MediaType\" = 4",
            new { Id = id }).ConfigureAwait(false);
    }

    public override Book? Find(int id)
    {
        using var conn = _database.OpenConnection();
        return conn.QueryFirstOrDefault<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Id\" = @Id AND \"MediaType\" = 4",
            new { Id = id });
    }

    public async Task<Book?> FindByTitleAsync(string title, int year, CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 4",
            new { Title = title, Year = year }).ConfigureAwait(false);
    }

    public Book? FindByTitle(string title, int year)
    {
        using var conn = _database.OpenConnection();
        return conn.QueryFirstOrDefault<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 4",
            new { Title = title, Year = year });
    }

    public async Task<List<Book>> GetByAuthorIdAsync(int authorId, CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        var result = await conn.QueryAsync<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"MediaType\" = 4",
            new { AuthorId = authorId }).ConfigureAwait(false);
        return result.ToList();
    }

    public List<Book> GetByAuthorId(int authorId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"MediaType\" = 4",
            new { AuthorId = authorId }).ToList();
    }

    public async Task<List<Book>> GetBySeriesIdAsync(int seriesId, CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        var result = await conn.QueryAsync<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"BookSeriesId\" = @SeriesId AND \"MediaType\" = 4",
            new { SeriesId = seriesId }).ConfigureAwait(false);
        return result.ToList();
    }

    public List<Book> GetBySeriesId(int seriesId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"BookSeriesId\" = @SeriesId AND \"MediaType\" = 4",
            new { SeriesId = seriesId }).ToList();
    }

    public async Task<List<Book>> GetMonitoredAsync(CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        var result = await conn.QueryAsync<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Monitored\" = @Monitored AND \"MediaType\" = 4",
            new { Monitored = true }).ConfigureAwait(false);
        return result.ToList();
    }

    public List<Book> GetMonitored()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Book>(
            "SELECT * FROM \"MediaItems\" WHERE \"Monitored\" = @Monitored AND \"MediaType\" = 4",
            new { Monitored = true }).ToList();
    }

    public async Task<bool> BookExistsAsync(int authorId, string title, int year, CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        var count = await conn.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 4",
            new { AuthorId = authorId, Title = title, Year = year }).ConfigureAwait(false);
        return count > 0;
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
