using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202508051)]
	public class Default_IncreaseNameFieldSize : Migration
	{
		private const string PlayerSettingsTable = "zenith_player_settings";
		private const string PlayerStorageTable = "zenith_player_storage";
		private const string Utf8Mb4Column255 = "VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
		private const string Utf8Mb4Column64 = "VARCHAR(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

		public override void Up()
		{
			AlterNameColumn(PlayerSettingsTable, Utf8Mb4Column255);
			AlterNameColumn(PlayerStorageTable, Utf8Mb4Column255);
		}

		public override void Down()
		{
			AlterNameColumn(PlayerSettingsTable, Utf8Mb4Column64);
			AlterNameColumn(PlayerStorageTable, Utf8Mb4Column64);
		}

		private void AlterNameColumn(string table, string columnDefinition)
		{
			if (!Schema.Table(table).Exists())
				return;

			Execute.Sql($"ALTER TABLE `{table}` MODIFY COLUMN `name` {columnDefinition} NULL;");
		}
	}
}
