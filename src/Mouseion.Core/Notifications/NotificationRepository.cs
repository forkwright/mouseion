// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using Dapper;
using Mouseion.Core.Datastore;

namespace Mouseion.Core.Notifications;

public class NotificationRepository : BasicRepository<NotificationDefinition>, INotificationRepository
{
    public NotificationRepository(IDatabase database)
        : base(database, "Notifications")
    {
    }

    public async Task<IEnumerable<NotificationDefinition>> GetEnabledAsync(CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        return await conn.QueryAsync<NotificationDefinition>(
            "SELECT * FROM \"Notifications\" WHERE \"Enabled\" = true").ConfigureAwait(false);
    }

    public async Task<IEnumerable<NotificationDefinition>> GetByImplementationAsync(
        string implementation,
        CancellationToken ct = default)
    {
        using var conn = _database.OpenConnection();
        return await conn.QueryAsync<NotificationDefinition>(
            "SELECT * FROM \"Notifications\" WHERE \"Implementation\" = @Implementation",
            new { Implementation = implementation }).ConfigureAwait(false);
    }

    public async Task<IEnumerable<NotificationDefinition>> GetByTriggerAsync(
        NotificationTrigger trigger,
        CancellationToken ct = default)
    {
        var column = trigger switch
        {
            NotificationTrigger.OnGrab => "OnGrab",
            NotificationTrigger.OnDownload => "OnDownload",
            NotificationTrigger.OnRename => "OnRename",
            NotificationTrigger.OnMediaAdded => "OnMediaAdded",
            NotificationTrigger.OnMediaDeleted => "OnMediaDeleted",
            NotificationTrigger.OnHealthIssue => "OnHealthIssue",
            NotificationTrigger.OnHealthRestored => "OnHealthRestored",
            NotificationTrigger.OnApplicationUpdate => "OnApplicationUpdate",
            _ => throw new ArgumentOutOfRangeException(nameof(trigger))
        };

        using var conn = _database.OpenConnection();
        return await conn.QueryAsync<NotificationDefinition>(
            $"SELECT * FROM \"Notifications\" WHERE \"Enabled\" = true AND \"{column}\" = true")
            .ConfigureAwait(false);
    }
}
