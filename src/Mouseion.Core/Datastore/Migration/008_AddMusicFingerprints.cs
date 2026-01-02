// Copyright (c) 2025 Mouseion Project
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentMigrator;

namespace Mouseion.Core.Datastore.Migration;

[Migration(8, "Add Fingerprint columns to MusicFiles for AcoustID duplicate detection")]
public class Migration_008_AddMusicFingerprints : FluentMigrator.Migration
{
    public override void Up()
    {
        Alter.Table("MusicFiles")
            .AddColumn("Fingerprint").AsString().Nullable()
            .AddColumn("FingerprintDuration").AsInt32().Nullable();

        Create.Index("IX_MusicFiles_Fingerprint")
            .OnTable("MusicFiles")
            .OnColumn("Fingerprint");
    }

    public override void Down()
    {
        Delete.Index("IX_MusicFiles_Fingerprint").OnTable("MusicFiles");
        Delete.Column("Fingerprint").FromTable("MusicFiles");
        Delete.Column("FingerprintDuration").FromTable("MusicFiles");
    }
}
