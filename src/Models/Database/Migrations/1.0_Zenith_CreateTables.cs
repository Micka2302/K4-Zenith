using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202410191)]
	public class Default_CreatePlayerDataTables : Migration
	{
		public override void Up()
		{
			if (!Schema.Table("zenith_player_settings").Exists())
			{
				Create.Table("zenith_player_settings")
					.WithColumn("steam_id").AsString(32).PrimaryKey()
					.WithColumn("name").AsString(64).Nullable()
					.WithColumn("last_online").AsCustom("TIMESTAMP").WithDefault(SystemMethods.CurrentDateTime).Nullable();
			}

			if (!Schema.Table("zenith_player_storage").Exists())
			{
				Create.Table("zenith_player_storage")
					.WithColumn("steam_id").AsString(32).PrimaryKey()
					.WithColumn("name").AsString(64).Nullable()
					.WithColumn("last_online").AsCustom("TIMESTAMP").WithDefault(SystemMethods.CurrentDateTime).Nullable();
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_player_settings").Exists())
				Delete.Table("zenith_player_settings");

			if (Schema.Table("zenith_player_storage").Exists())
				Delete.Table("zenith_player_storage");
		}
	}
}