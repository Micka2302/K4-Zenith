using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202508051)]
	public class Default_IncreaseNameFieldSize : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_player_settings").Exists())
			{
				Alter.Table("zenith_player_settings")
					.AlterColumn("name").AsString(255).Nullable();
			}

			if (Schema.Table("zenith_player_storage").Exists())
			{
				Alter.Table("zenith_player_storage")
					.AlterColumn("name").AsString(255).Nullable();
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_player_settings").Exists())
			{
				Alter.Table("zenith_player_settings")
					.AlterColumn("name").AsString(64).Nullable();
			}

			if (Schema.Table("zenith_player_storage").Exists())
			{
				Alter.Table("zenith_player_storage")
					.AlterColumn("name").AsString(64).Nullable();
			}
		}
	}
}