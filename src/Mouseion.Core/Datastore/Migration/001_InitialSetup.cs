using FluentMigrator;

namespace Mouseion.Core.Datastore.Migration;

[Migration(1, "Initial Setup")]
public class Migration_001_InitialSetup : FluentMigrator.Migration
{
    public override void Up()
    {
        Create.Table("VersionInfo")
            .WithColumn("Version").AsInt64().NotNullable()
            .WithColumn("AppliedOn").AsDateTime().Nullable()
            .WithColumn("Description").AsString().Nullable();
    }

    public override void Down()
    {
        Delete.Table("VersionInfo");
    }
}
