using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202410195)]
	public class Bans_CreateZenithBansTablesUpgradeV2 : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_bans_players").Exists())
			{
				if (!Schema.Table("zenith_bans_players").Column("current_server").Exists())
					Alter.Table("zenith_bans_players").AddColumn("current_server").AsString(50).Nullable();
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_bans_players").Column("current_server").Exists())
				Delete.Column("current_server").FromTable("zenith_bans_players");
		}
	}
}
