// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

// Mouseion - Unified media manager
// Copyright (C) 2024-2025 Mouseion Contributors
// Based on Radarr (https://github.com/Radarr/Radarr)
// Copyright (C) 2010-2025 Radarr Contributors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentMigrator;

namespace Mouseion.Core.Datastore.Migration;

[Migration(11, "Enhance Tracks for Phase 2 advanced filtering")]
public class Migration_011_EnhanceTracksForPhase2 : FluentMigrator.Migration
{
    public override void Up()
    {
        // Add audio quality fields to MusicFiles table
        Alter.Table("MusicFiles")
            .AddColumn("BitDepth").AsInt32().Nullable()
            .AddColumn("DynamicRange").AsInt32().Nullable()
            .AddColumn("Lossless").AsBoolean().NotNullable().WithDefaultValue(false)
            .AddColumn("Codec").AsString().Nullable();

        // Add denormalized search fields to MediaItems table for Tracks
        Alter.Table("MediaItems")
            .AddColumn("ArtistName").AsString().Nullable()
            .AddColumn("AlbumName").AsString().Nullable()
            .AddColumn("Genre").AsString().Nullable();

        // Create indexes for audio quality filtering on MusicFiles
        Create.Index("IX_MusicFiles_Codec")
            .OnTable("MusicFiles")
            .OnColumn("Codec");

        Create.Index("IX_MusicFiles_DynamicRange")
            .OnTable("MusicFiles")
            .OnColumn("DynamicRange");

        Create.Index("IX_MusicFiles_Lossless")
            .OnTable("MusicFiles")
            .OnColumn("Lossless");

        Create.Index("IX_MusicFiles_SampleRate")
            .OnTable("MusicFiles")
            .OnColumn("SampleRate");

        Create.Index("IX_MusicFiles_BitDepth")
            .OnTable("MusicFiles")
            .OnColumn("BitDepth");

        Create.Index("IX_MusicFiles_AudioFormat")
            .OnTable("MusicFiles")
            .OnColumn("AudioFormat");

        // Create indexes for denormalized search fields on MediaItems
        Create.Index("IX_MediaItems_Genre")
            .OnTable("MediaItems")
            .OnColumn("Genre");

        Create.Index("IX_MediaItems_ArtistName")
            .OnTable("MediaItems")
            .OnColumn("ArtistName");

        Create.Index("IX_MediaItems_AlbumName")
            .OnTable("MediaItems")
            .OnColumn("AlbumName");
    }

    public override void Down()
    {
        // Remove indexes from MediaItems
        Delete.Index("IX_MediaItems_AlbumName").OnTable("MediaItems");
        Delete.Index("IX_MediaItems_ArtistName").OnTable("MediaItems");
        Delete.Index("IX_MediaItems_Genre").OnTable("MediaItems");

        // Remove indexes from MusicFiles
        Delete.Index("IX_MusicFiles_AudioFormat").OnTable("MusicFiles");
        Delete.Index("IX_MusicFiles_BitDepth").OnTable("MusicFiles");
        Delete.Index("IX_MusicFiles_SampleRate").OnTable("MusicFiles");
        Delete.Index("IX_MusicFiles_Lossless").OnTable("MusicFiles");
        Delete.Index("IX_MusicFiles_DynamicRange").OnTable("MusicFiles");
        Delete.Index("IX_MusicFiles_Codec").OnTable("MusicFiles");

        // Remove columns from MediaItems
        Delete.Column("Genre").FromTable("MediaItems");
        Delete.Column("AlbumName").FromTable("MediaItems");
        Delete.Column("ArtistName").FromTable("MediaItems");

        // Remove columns from MusicFiles
        Delete.Column("Codec").FromTable("MusicFiles");
        Delete.Column("Lossless").FromTable("MusicFiles");
        Delete.Column("DynamicRange").FromTable("MusicFiles");
        Delete.Column("BitDepth").FromTable("MusicFiles");
    }
}
