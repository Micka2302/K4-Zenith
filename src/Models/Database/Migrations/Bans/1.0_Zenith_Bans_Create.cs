using FluentMigrator;

namespace Zenith.Migrations
{
	[Migration(202410193)] // Új migráció verziója
	public class Bans_CreateZenithBansTables : Migration
	{
		public override void Up()
		{
			// zenith_bans_players table creation
			if (!Schema.Table("zenith_bans_players").Exists())
			{
				Create.Table("zenith_bans_players")
					.WithColumn("id").AsInt32().PrimaryKey().Identity()
					.WithColumn("steam_id").AsInt64().Unique()
					.WithColumn("name").AsString(64).Nullable()
					.WithColumn("ip_addresses").AsCustom("JSON").Nullable()
					.WithColumn("last_online").AsDateTime().Nullable();

				Execute.Sql("ALTER TABLE zenith_bans_players CHARACTER SET = utf8mb4, COLLATE = utf8mb4_unicode_ci;");
			}

			// zenith_bans_player_ranks table creation
			if (!Schema.Table("zenith_bans_player_ranks").Exists())
			{
				Create.Table("zenith_bans_player_ranks")
					.WithColumn("id").AsInt32().PrimaryKey().Identity()
					.WithColumn("steam_id").AsInt64().ForeignKey("FK_player_ranks_steam_id", "zenith_bans_players", "steam_id")
					.WithColumn("server_ip").AsString(50).NotNullable()
					.WithColumn("groups").AsCustom("JSON").Nullable()
					.WithColumn("permissions").AsCustom("JSON").Nullable()
					.WithColumn("immunity").AsInt32().Nullable()
					.WithColumn("rank_expiry").AsDateTime().Nullable();

				Create.UniqueConstraint("unique_player_server").OnTable("zenith_bans_player_ranks").Columns("steam_id", "server_ip");
				Execute.Sql("ALTER TABLE zenith_bans_player_ranks CHARACTER SET = utf8mb4, COLLATE = utf8mb4_unicode_ci;");
			}

			// zenith_bans_admin_groups table creation
			if (!Schema.Table("zenith_bans_admin_groups").Exists())
			{
				Create.Table("zenith_bans_admin_groups")
					.WithColumn("id").AsInt32().PrimaryKey().Identity()
					.WithColumn("name").AsString(50).Unique()
					.WithColumn("permissions").AsCustom("JSON").Nullable()
					.WithColumn("immunity").AsInt32().Nullable();

				Execute.Sql("ALTER TABLE zenith_bans_admin_groups CHARACTER SET = utf8mb4, COLLATE = utf8mb4_unicode_ci;");
			}

			// zenith_bans_punishments table creation
			if (!Schema.Table("zenith_bans_punishments").Exists())
			{
				Create.Table("zenith_bans_punishments")
					.WithColumn("id").AsInt32().PrimaryKey().Identity()
					.WithColumn("steam_id").AsInt64().ForeignKey("FK_punishments_steam_id", "zenith_bans_players", "steam_id")
					.WithColumn("type").AsCustom("ENUM('mute', 'gag', 'silence', 'ban', 'warn', 'kick')").Nullable()
					.WithColumn("duration").AsInt32().Nullable()
					.WithColumn("created_at").AsDateTime().Nullable()
					.WithColumn("expires_at").AsDateTime().Nullable()
					.WithColumn("admin_steam_id").AsInt64().Nullable().ForeignKey("FK_punishments_admin_steam_id", "zenith_bans_players", "steam_id")
					.WithColumn("removed_at").AsDateTime().Nullable()
					.WithColumn("remove_admin_steam_id").AsInt64().Nullable().ForeignKey("FK_punishments_remove_admin_steam_id", "zenith_bans_players", "steam_id")
					.WithColumn("server_ip").AsString(50).NotNullable().WithDefaultValue("all")
					.WithColumn("reason").AsCustom("TEXT").Nullable();

				Execute.Sql("ALTER TABLE zenith_bans_punishments CHARACTER SET = utf8mb4, COLLATE = utf8mb4_unicode_ci;");
			}
		}

		public override void Down()
		{
			// Táblák törlése, ha a migráció visszaáll
			if (Schema.Table("zenith_bans_punishments").Exists())
				Delete.Table("zenith_bans_punishments");

			if (Schema.Table("zenith_bans_admin_groups").Exists())
				Delete.Table("zenith_bans_admin_groups");

			if (Schema.Table("zenith_bans_player_ranks").Exists())
				Delete.Table("zenith_bans_player_ranks");

			if (Schema.Table("zenith_bans_players").Exists())
				Delete.Table("zenith_bans_players");
		}
	}
}
