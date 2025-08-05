using FluentMigrator;

namespace Zenith.Migrations.Bans
{
	[Migration(202501052)]
	public class Bans_IncreaseNameFieldSize : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_bans_players").Exists())
			{
				Alter.Table("zenith_bans_players")
					.AlterColumn("name").AsString(255).Nullable();
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_bans_players").Exists())
			{
				Alter.Table("zenith_bans_players")
					.AlterColumn("name").AsString(64).Nullable();
			}
		}
	}
}