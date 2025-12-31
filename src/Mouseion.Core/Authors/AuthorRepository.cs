// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using Dapper;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.Authors;

public interface IAuthorRepository : IBasicRepository<Author>
{
    Author? FindByName(string name);
    Author? FindByForeignId(string foreignAuthorId);
    List<Author> GetMonitored();
    bool AuthorPathExists(string path);
}

public class AuthorRepository : BasicRepository<Author>, IAuthorRepository
{
    public AuthorRepository(IDatabase database)
        : base(database)
    {
    }

    public Author? FindByName(string name)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<Author>(
            $"SELECT * FROM \"{_table}\" WHERE \"Name\" = @Name",
            new { Name = name });
    }

    public Author? FindByForeignId(string foreignAuthorId)
    {
        using var conn = _database.OpenConnection();
        return conn.QuerySingleOrDefault<Author>(
            $"SELECT * FROM \"{_table}\" WHERE \"ForeignAuthorId\" = @ForeignAuthorId",
            new { ForeignAuthorId = foreignAuthorId });
    }

    public List<Author> GetMonitored()
    {
        using var conn = _database.OpenConnection();
        return conn.Query<Author>(
            $"SELECT * FROM \"{_table}\" WHERE \"Monitored\" = @Monitored",
            new { Monitored = true }).ToList();
    }

    public bool AuthorPathExists(string path)
    {
        using var conn = _database.OpenConnection();
        var count = conn.QuerySingle<int>(
            $"SELECT COUNT(*) FROM \"{_table}\" WHERE \"Path\" = @Path",
            new { Path = path });
        return count > 0;
    }
}
