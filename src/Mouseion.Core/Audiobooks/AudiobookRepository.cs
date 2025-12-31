// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Dapper;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.Audiobooks;

public interface IAudiobookRepository : IBasicRepository<Audiobook>
{
    Audiobook? FindByTitle(string title, int year);
    List<Audiobook> GetByAuthorId(int authorId);
    List<Audiobook> GetBySeriesId(int seriesId);
    List<Audiobook> GetMonitored();
    bool AudiobookExists(int authorId, string title, int year);
}

public class AudiobookRepository : BasicRepository<Audiobook>, IAudiobookRepository
{
    public AudiobookRepository(IDatabase database)
        : base(database, "MediaItems")
    {
    }

    public override IEnumerable<Audiobook> All()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Audiobook>("SELECT * FROM \"MediaItems\" WHERE \"MediaType\" = 5");
    }

    public override Audiobook? Find(int id)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<Audiobook>(
            "SELECT * FROM \"MediaItems\" WHERE \"Id\" = @Id AND \"MediaType\" = 5",
            new { Id = id });
    }

    public Audiobook? FindByTitle(string title, int year)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<Audiobook>(
            "SELECT * FROM \"MediaItems\" WHERE \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 5",
            new { Title = title, Year = year });
    }

    public List<Audiobook> GetByAuthorId(int authorId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Audiobook>(
            "SELECT * FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"MediaType\" = 5",
            new { AuthorId = authorId }).ToList();
    }

    public List<Audiobook> GetBySeriesId(int seriesId)
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Audiobook>(
            "SELECT * FROM \"MediaItems\" WHERE \"BookSeriesId\" = @SeriesId AND \"MediaType\" = 5",
            new { SeriesId = seriesId }).ToList();
    }

    public List<Audiobook> GetMonitored()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Audiobook>(
            "SELECT * FROM \"MediaItems\" WHERE \"Monitored\" = @Monitored AND \"MediaType\" = 5",
            new { Monitored = true }).ToList();
    }

    public bool AudiobookExists(int authorId, string title, int year)
    {
        using var conn = _database.OpenConnection();
        var count = conn.QuerySingle<int>(
            "SELECT COUNT(*) FROM \"MediaItems\" WHERE \"AuthorId\" = @AuthorId AND \"Title\" = @Title AND \"Year\" = @Year AND \"MediaType\" = 5",
            new { AuthorId = authorId, Title = title, Year = year });
        return count > 0;
    }
}
