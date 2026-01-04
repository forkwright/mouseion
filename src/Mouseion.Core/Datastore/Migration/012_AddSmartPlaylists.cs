// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentMigrator;

namespace Mouseion.Core.Datastore.Migration;

[Migration(12)]
public class Migration_012_AddSmartPlaylists : FluentMigrator.Migration
{
    public override void Up()
    {
        Create.Table("SmartPlaylists")
            .WithColumn("Id").AsString().PrimaryKey()
            .WithColumn("Name").AsString().NotNullable()
            .WithColumn("FilterRequestJson").AsString().NotNullable()
            .WithColumn("TrackCount").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("LastRefreshed").AsString().NotNullable()
            .WithColumn("CreatedAt").AsString().NotNullable()
            .WithColumn("UpdatedAt").AsString().NotNullable();

        Create.Table("SmartPlaylistTracks")
            .WithColumn("SmartPlaylistId").AsString().NotNullable()
            .WithColumn("TrackId").AsString().NotNullable()
            .WithColumn("Position").AsInt32().NotNullable();

        Create.PrimaryKey("PK_SmartPlaylistTracks")
            .OnTable("SmartPlaylistTracks")
            .Columns("SmartPlaylistId", "TrackId");

        Create.Index("idx_smart_playlist_tracks_playlist")
            .OnTable("SmartPlaylistTracks")
            .OnColumn("SmartPlaylistId");

        Create.Index("idx_smart_playlist_tracks_position")
            .OnTable("SmartPlaylistTracks")
            .OnColumn("SmartPlaylistId")
            .Ascending()
            .OnColumn("Position")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Table("SmartPlaylistTracks");
        Delete.Table("SmartPlaylists");
    }
}
