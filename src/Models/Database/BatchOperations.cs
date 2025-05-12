using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Data;
using System.Text;

namespace Zenith.Models
{
    public static class DatabaseBatchOperations
    {
        public static async Task<bool> SaveAllOnlinePlayerDataWithOptimizedBatching(Plugin plugin)
        {
            try
            {
                var playerDataToSave = new List<(string SteamId, string Name, Dictionary<string, string> Settings, Dictionary<string, string> Storage)>();

                foreach (var player in Player.List.Values)
                {
                    if (!player.Loaded)
                        continue;

                    var settingsData = new Dictionary<string, string>();
                    var storageData = new Dictionary<string, string>();

                    ProcessPlayerData(player.Settings, settingsData, "settings");
                    ProcessPlayerData(player.Storage, storageData, "storage");

                    playerDataToSave.Add((player.SteamID.ToString(), player.Name, settingsData, storageData));
                }

                if (playerDataToSave.Count == 0)
                    return true;

                using var connection = plugin.Database.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    await BatchSaveDataType(connection, Player.TABLE_PLAYER_SETTINGS, playerDataToSave, p => p.Item3, transaction);
                    await BatchSaveDataType(connection, Player.TABLE_PLAYER_STORAGE, playerDataToSave, p => p.Item4, transaction);
                    await transaction.CommitAsync();
                    plugin._lastStorageSave = DateTime.Now;
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    plugin.Logger.LogError($"Error during batch save operation: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                plugin.Logger.LogError($"Failed to save player data in batch mode: {ex.Message}");
                return false;
            }
        }

        private static void ProcessPlayerData(Dictionary<string, Dictionary<string, object?>> sourceDict, Dictionary<string, string> targetDict, string dataType)
        {
            foreach (var moduleGroup in sourceDict)
            {
                var moduleID = moduleGroup.Key;
                var moduleData = moduleGroup.Value;
                targetDict[$"{moduleID}.{dataType}"] = System.Text.Json.JsonSerializer.Serialize(moduleData);
            }
        }

        private static async Task<bool> BatchSaveDataType(
            MySqlConnection connection,
            string tableName,
            List<(string SteamId, string Name, Dictionary<string, string> Settings, Dictionary<string, string> Storage)> playerData,
            Func<(string, string, Dictionary<string, string>, Dictionary<string, string>), Dictionary<string, string>> dataSelector,
            MySqlTransaction transaction)
        {
            var allColumns = new HashSet<string>();
            foreach (var player in playerData)
            {
                var data = dataSelector(player);
                foreach (var column in data.Keys)
                {
                    allColumns.Add(column);
                }
            }

            if (allColumns.Count == 0)
                return false;

            var columnList = new List<string> { "steam_id", "name", "last_online" };
            columnList.AddRange(allColumns);

            var batchSize = 32;
            var totalBatches = (int)Math.Ceiling((double)playerData.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batchPlayers = playerData.Skip(batchIndex * batchSize).Take(batchSize).ToList();

                var queryBuilder = new StringBuilder();
                queryBuilder.AppendLine($"INSERT INTO `{tableName}` (");
                queryBuilder.AppendLine(string.Join(", ", columnList.Select(c => $"`{MySqlHelper.EscapeString(c)}`")));
                queryBuilder.AppendLine(") VALUES ");

                var valueParameters = new DynamicParameters();
                var valueRows = new List<string>();
                var updateClauses = new List<string>();

                foreach (var col in columnList.Where(c => c != "steam_id" && c != "name"))
                {
                    updateClauses.Add($"`{MySqlHelper.EscapeString(col)}` = VALUES(`{MySqlHelper.EscapeString(col)}`)");
                }

                int playerIndex = 0;
                foreach (var player in batchPlayers)
                {
                    var data = dataSelector(player);

                    var values = new List<string>
                    {
                        $"@steam_id_{playerIndex}",
                        $"@name_{playerIndex}",
                        "NOW()" // last_online
					};

                    valueParameters.Add($"@steam_id_{playerIndex}", player.SteamId);
                    valueParameters.Add($"@name_{playerIndex}", player.Name);

                    foreach (var column in allColumns)
                    {
                        var paramName = $"@{column.Replace(".", "_")}_{playerIndex}";
                        if (data.TryGetValue(column, out var value))
                        {
                            values.Add(paramName);
                            valueParameters.Add(paramName, value);
                        }
                        else
                        {
                            values.Add("NULL");
                        }
                    }

                    valueRows.Add($"({string.Join(", ", values)})");
                    playerIndex++;
                }

                queryBuilder.AppendLine(string.Join(",\n", valueRows));
                queryBuilder.AppendLine("ON DUPLICATE KEY UPDATE");
                queryBuilder.AppendLine(string.Join(",\n", updateClauses));

                await connection.ExecuteAsync(queryBuilder.ToString(), valueParameters, transaction);
            }

            return true;
        }

        public static async Task<bool> LoadAllOnlinePlayerDataWithOptimizedBatching(Plugin plugin)
        {
            try
            {
                var steamIds = new List<string>();
                var playerControls = new Dictionary<string, CCSPlayerController>();

                foreach (var playerController in Utilities.GetPlayers())
                {
                    if (playerController == null || !playerController.IsValid || playerController.IsBot || playerController.IsHLTV)
                        continue;

                    var steamId = playerController.SteamID.ToString();
                    var existingPlayer = Player.List.Values.FirstOrDefault(p => p.SteamID.ToString() == steamId);

                    if (existingPlayer == null || !existingPlayer.Loaded)
                    {
                        steamIds.Add(steamId);
                        playerControls[steamId] = playerController;
                    }
                }

                if (steamIds.Count == 0)
                    return true;

                using var connection = plugin.Database.CreateConnection();
                await connection.OpenAsync();

                var tablesExist = await connection.ExecuteScalarAsync<bool>(@"
                    SELECT COUNT(*) > 0
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                    AND table_name IN (@Table1, @Table2)",
                    new { Table1 = Player.TABLE_PLAYER_SETTINGS, Table2 = Player.TABLE_PLAYER_STORAGE });

                if (!tablesExist)
                {
                    plugin.Logger.LogInformation("Player tables don't exist yet, skipping batch load");
                    return false;
                }

                var settingsColumns = string.Join(", ",
                    Player.moduleDefaultSettings.Keys.Select(k =>
                        $"s.`{MySqlHelper.EscapeString(k)}.settings` AS `{k}_settings`"));

                var storageColumns = string.Join(", ",
                    Player.moduleDefaultStorage.Keys.Select(k =>
                        $"st.`{MySqlHelper.EscapeString(k)}.storage` AS `{k}_storage`"));

                var query = $@"
                    SELECT
                        s.steam_id,
                        s.name,
                        s.last_online AS settings_last_online,
                        st.last_online AS storage_last_online
                        {(string.IsNullOrEmpty(settingsColumns) ? "" : ", " + settingsColumns)}
                        {(string.IsNullOrEmpty(storageColumns) ? "" : ", " + storageColumns)}
                    FROM
                        `{Player.TABLE_PLAYER_SETTINGS}` s
                    LEFT JOIN
                        `{Player.TABLE_PLAYER_STORAGE}` st ON s.steam_id = st.steam_id
                    WHERE
                        s.steam_id IN @SteamIDs";

                var results = await connection.QueryAsync<dynamic>(query, new { SteamIDs = steamIds });

                Server.NextWorldUpdate(() =>
                {
                    foreach (var result in results)
                    {
                        string steamId = result.steam_id.ToString();

                        var player = Player.List.Values.FirstOrDefault(p => p.SteamID.ToString() == steamId);
                        if (player == null)
                        {
                            if (playerControls.TryGetValue(steamId, out var controller) && controller != null && controller.IsValid)
                            {
                                player = new Player(plugin, controller);
                            }
                            else
                            {
                                continue;
                            }
                        }

                        Dictionary<string, object> resultDict = new Dictionary<string, object>();
                        if (result is IDictionary<string, object> resultDictionary)
                        {
                            foreach (var prop in resultDictionary)
                            {
                                resultDict[prop.Key] = prop.Value;
                            }
                        }

                        LoadPlayerDataFromResult(player, resultDict, plugin);
                        player.Loaded = true;

                        plugin._moduleServices?.InvokeZenithPlayerLoaded(player.Controller!);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                plugin.Logger.LogError($"Error during batch load operation: {ex.Message}");
                return false;
            }
        }

        private static void LoadPlayerDataFromResult(Player player, Dictionary<string, object> result, Plugin plugin)
        {
            foreach (var module in Player.moduleDefaultSettings.Keys)
            {
                var settingsKey = $"{module}_settings";
                if (result.TryGetValue(settingsKey, out object? value) && value != null)
                {
                    try
                    {
                        string? jsonString = value.ToString();
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            var moduleData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString);
                            if (moduleData != null)
                            {
                                if (!player.Settings.TryGetValue(module, out var moduleDict))
                                {
                                    moduleDict = new Dictionary<string, object?>();
                                    player.Settings[module] = moduleDict;
                                }

                                foreach (var item in moduleData)
                                {
                                    moduleDict[item.Key] = item.Value;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        plugin.Logger.LogError($"Error deserializing settings data for module {module}: {ex.Message}");
                    }
                }
            }

            foreach (var module in Player.moduleDefaultStorage.Keys)
            {
                var storageKey = $"{module}_storage";
                if (result.TryGetValue(storageKey, out object? value) && value != null)
                {
                    try
                    {
                        string? jsonString = value.ToString();
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            var moduleData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonString);
                            if (moduleData != null)
                            {
                                if (!player.Storage.TryGetValue(module, out var moduleDict))
                                {
                                    moduleDict = new Dictionary<string, object?>();
                                    player.Storage[module] = moduleDict;
                                }

                                foreach (var item in moduleData)
                                {
                                    moduleDict[item.Key] = item.Value;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        plugin.Logger.LogError($"Error deserializing storage data for module {module}: {ex.Message}");
                    }
                }
            }

            Player.ApplyDefaultValues(Player.moduleDefaultSettings, player.Settings);
            Player.ApplyDefaultValues(Player.moduleDefaultStorage, player.Storage);
        }
    }
}