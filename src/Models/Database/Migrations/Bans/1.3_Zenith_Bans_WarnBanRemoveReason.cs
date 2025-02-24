using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202410196)]
	public class Bans_CreateZenithBansTablesUpgradeV3 : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_bans_punishments").Exists())
			{
				if (Schema.Table("zenith_bans_punishments").Column("status").Exists())
				{
					Alter.Table("zenith_bans_punishments").AlterColumn("status").AsCustom("ENUM('active', 'warn_ban', 'expired', 'removed', 'removed_console')").NotNullable().WithDefaultValue("active");
				}
				if (!Schema.Table("zenith_bans_punishments").Column("remove_reason").Exists())
					Alter.Table("zenith_bans_punishments").AddColumn("remove_reason").AsCustom("TEXT").Nullable();
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_bans_punishments").Column("remove_reason").Exists())
				Delete.Column("remove_reason").FromTable("zenith_bans_punishments");

			if (Schema.Table("zenith_bans_punishments").Column("status").Exists())
				Alter.Table("zenith_bans_punishments").AlterColumn("status").AsCustom("ENUM('active', 'expired', 'removed', 'removed_console')").NotNullable().WithDefaultValue("active");
		}
	}
}
