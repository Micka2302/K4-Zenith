using System.Reflection;
using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Zenith.Models
{
    public partial class Database
    {
        public static void RunAutoMigrations(Plugin plugin, string backupPath, bool force = false)
        {
            var localService = new ServiceCollection()
                .AddFluentMigratorCore()
                .ConfigureRunner(rb =>
                {
                    rb.AddMySql5() // MySQL or MariaDB
                        .WithGlobalConnectionString(plugin.Database.GetConnectionString())
                        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations();
                })
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .BuildServiceProvider(false);

            var runner = localService.GetRequiredService<IMigrationRunner>();
            var migrations = runner.MigrationLoader.LoadMigrations();

            // Filter migrations based on modules
            if (!Directory.Exists(Path.Combine(plugin.ModuleDirectory, "..", "K4-Zenith-Bans")))
            {
                migrations = new SortedList<long, FluentMigrator.Infrastructure.IMigrationInfo>(
                    migrations
                    .Where(m => !m.Value.Migration.GetType().Name.StartsWith("bans", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(m => m.Key, m => m.Value));
            }

            if (!Directory.Exists(Path.Combine(plugin.ModuleDirectory, "..", "K4-Zenith-Stats")))
            {
                migrations = new SortedList<long, FluentMigrator.Infrastructure.IMigrationInfo>(
                    migrations
                    .Where(m => !m.Value.Migration.GetType().Name.StartsWith("stats", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(m => m.Key, m => m.Value));
            }

            // Only run migrations that haven't been applied yet
            var pendingMigrations = force ? [.. migrations] : migrations
                .Where(m => runner.HasMigrationsToApplyUp(m.Key))
                .ToList();

            if (pendingMigrations.Count != 0)
            {
                Task.Run(async () =>
                {
                    plugin.Logger.LogWarning("Creating a backup of the database before running migrations.");
                    await BackupDatabase(plugin, backupPath);
                    plugin.Logger.LogInformation($"Database backup completed to {backupPath}. Starting migrations.");

                    foreach (var migration in pendingMigrations)
                    {
                        plugin.Logger.LogInformation($"Running migration: {migration.Value.Migration.GetType().Name}");
                        runner.MigrateUp(migration.Key);  // Run migrations that are not in the VersionInfo table
                    }

                    plugin.Logger.LogInformation("Database migrations completed successfully.");
                }).Wait();
            }
            else
            {
                plugin.Logger.LogInformation("No migrations to apply. Database is up to date.");
            }
        }

        public static async Task BackupDatabase(Plugin plugin, string outputPath)
        {
            try
            {
                using var connection = new MySqlConnection(plugin.Database.GetConnectionString());

                await connection.OpenAsync();

                var databaseName = await connection.ExecuteScalarAsync<string>("SELECT DATABASE();");

                using var writer = new StreamWriter(outputPath);

                var tables = await connection.QueryAsync<string>("SHOW TABLES;");
                foreach (var table in tables)
                {
                    var createTableQuery = await connection.ExecuteScalarAsync<string>($"SHOW CREATE TABLE `{table}`;");
                    await writer.WriteLineAsync($"-- Schema for table `{table}`");
                    await writer.WriteLineAsync(createTableQuery + ";");
                    await writer.WriteLineAsync();

                    var rows = await connection.QueryAsync($"SELECT * FROM `{table}`;");
                    foreach (var row in rows)
                    {
                        var insertQuery = $"INSERT INTO `{table}` VALUES (";
                        foreach (var prop in row)
                        {
                            var value = prop.Value == null ? "NULL" : $"'{MySqlHelper.EscapeString(prop.Value.ToString())}'";
                            insertQuery += $"{value},";
                        }
                        insertQuery = insertQuery.TrimEnd(',') + ");";
                        await writer.WriteLineAsync(insertQuery);
                    }
                    await writer.WriteLineAsync();
                }

                await writer.WriteLineAsync($"-- Backup completed for `{databaseName}`.");
            }
            catch (Exception ex)
            {
                plugin.Logger.LogError($"Error during database backup: {ex.Message}");
            }
        }
    }
}