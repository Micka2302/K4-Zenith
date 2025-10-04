using FluentMigrator;

namespace Zenith.Migrations.Bans
{
	[Migration(202501052)]
	public class Bans_IncreaseNameFieldSize : Migration
	{
		private const string BansPlayersTable = "zenith_bans_players";
		private const string Utf8Mb4Column255 = "VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
		private const string Utf8Mb4Column64 = "VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

		public override void Up()
		{
			AlterNameColumn(BansPlayersTable, Utf8Mb4Column255);
		}

		public override void Down()
		{
			AlterNameColumn(BansPlayersTable, Utf8Mb4Column64);
		}

		private void AlterNameColumn(string table, string columnDefinition)
		{
			if (!Schema.Table(table).Exists())
				return;

			Execute.Sql($"ALTER TABLE `{table}` MODIFY COLUMN `name` {columnDefinition} NULL;");
		}
	}
}
