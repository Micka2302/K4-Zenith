using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202411181)]
	public class Default_SetTablesUtf8mb4 : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_player_settings").Exists())
			{
				Execute.Sql("ALTER TABLE zenith_player_settings CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;");
			}

			if (Schema.Table("zenith_player_storage").Exists())
			{
				Execute.Sql("ALTER TABLE zenith_player_storage CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;");
			}
		}

		public override void Down()
		{
			if (Schema.Table("zenith_player_settings").Exists())
			{
				Execute.Sql("ALTER TABLE zenith_player_settings CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;");
			}

			if (Schema.Table("zenith_player_storage").Exists())
			{
				Execute.Sql("ALTER TABLE zenith_player_storage CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;");
			}
		}
	}
}
