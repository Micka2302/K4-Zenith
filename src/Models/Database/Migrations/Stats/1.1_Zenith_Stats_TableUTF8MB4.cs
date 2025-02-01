using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202411183)]
	public class Stats_SetTablesUtf8mb4 : Migration
	{
		public override void Up()
		{
			if (Schema.Table("zenith_weapon_stats").Exists())
				Execute.Sql("ALTER TABLE zenith_weapon_stats CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;");

			if (Schema.Table("zenith_map_stats").Exists())
				Execute.Sql("ALTER TABLE zenith_map_stats CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;");
		}

		public override void Down()
		{
			if (Schema.Table("zenith_weapon_stats").Exists())
				Execute.Sql("ALTER TABLE zenith_weapon_stats CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;");

			if (Schema.Table("zenith_map_stats").Exists())
				Execute.Sql("ALTER TABLE zenith_map_stats CONVERT TO CHARACTER SET utf8 COLLATE utf8_general_ci;");
		}
	}
}