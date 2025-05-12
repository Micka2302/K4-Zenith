using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace ZenithAPI
{
	/// <summary>
	/// Provides services for managing module-specific data.
	/// </summary>
	public interface IModuleServices : IZenithEvents // ! zenith:module-services
	{
		/// <summary>
		/// Prints a message to all players' chat.
		/// </summary>
		void PrintForAll(string message, bool showPrefix = true);

		/// <summary>
		/// Prints a message to all players on a specific team.
		/// </summary>
		void PrintForTeam(CsTeam team, string message, bool showPrefix = true);

		/// <summary>
		/// Prints a message to all players on a specific team.
		/// </summary>
		void PrintForPlayer(CCSPlayerController? player, string message, bool showPrefix = true);

		/// <summary>
		/// Retrieves the connection string for the database.
		/// </summary>
		string GetConnectionString();

		/// <summary>
		/// Registers default settings for a module.
		/// </summary>
		/// <param name="defaultSettings">A dictionary of default settings.</param>
		void RegisterModuleSettings(Dictionary<string, object?> defaultSettings, IStringLocalizer? localizer = null);

		/// <summary>
		/// Registers default storage items for a module.
		/// </summary>
		/// <param name="defaultStorage">A dictionary of default storage items.</param>
		void RegisterModuleStorage(Dictionary<string, object?> defaultStorage);

		/// <summary>
		/// Registers a command for a module.
		/// </summary>
		/// <param name="command">The command to register.</param>
		/// <param name="description">The description of the command.</param>
		/// <param name="handler">The callback function to execute when the command is invoked.</param>
		/// <param name="usage">The usage type of the command.</param>
		/// <param name="argCount">The number of arguments required for the command.</param>
		/// <param name="helpText">The help text to display when the command is used incorrectly.</param>
		/// <param name="permission">The permission required to use the command.</param>
		void RegisterModuleCommand(string command, string description, CommandInfo.CommandCallback handler, CommandUsage usage = CommandUsage.CLIENT_AND_SERVER, int argCount = 0, string? helpText = null, string? permission = null);

		/// <summary>
		/// Registers multiple commands for a module.
		/// </summary>
		/// <param name="commands">The commands to register.</param>
		/// <param name="description">The description of the commands.</param>
		/// <param name="handler">The callback function to execute when the commands are invoked.</param>
		/// <param name="usage">The usage type of the commands.</param>
		/// <param name="argCount">The number of arguments required for the commands.</param>
		/// <param name="helpText">The help text to display when the commands are used incorrectly.</param>
		/// <param name="permission">The permission required to use the commands.</param>
		void RegisterModuleCommands(List<string> commands, string description, CommandInfo.CommandCallback handler, CommandUsage usage = CommandUsage.CLIENT_AND_SERVER, int argCount = 0, string? helpText = null, string? permission = null);

		/// <summary>
		/// Registers a placeholder for a player-specific value.
		/// </summary>
		/// <param name="key">The key of the placeholder.</param>
		/// <param name="valueFunc">The function to retrieve the value.</param>
		void RegisterModulePlayerPlaceholder(string key, Func<CCSPlayerController, string> valueFunc);

		/// <summary>
		/// Registers a placeholder for a server-specific value.
		/// </summary>
		/// <param name="key">The key of the placeholder.</param>
		/// <param name="valueFunc">The function to retrieve the value.</param>
		void RegisterModuleServerPlaceholder(string key, Func<string> valueFunc);

		/// <summary>
		/// Registers a module configuration setting.
		/// </summary>
		/// <typeparam name="T">The type of the setting.</typeparam>
		/// <param name="groupName">The group name of the setting.</param>
		/// <param name="configName">The name of the setting.</param>
		/// <param name="description">The description of the setting.</param>
		/// <param name="defaultValue">The default value of the setting.</param>
		/// <param name="flags">The flags of the setting.</param>
		void RegisterModuleConfig<T>(string groupName, string configName, string description, T defaultValue, ConfigFlag flags = ConfigFlag.None) where T : notnull;

		/// <summary>
		/// Checks if a module configuration setting exists.
		/// </summary>
		/// <param name="groupName">The group name of the setting.</param>
		/// <param name="configName">The name of the setting.</param>
		bool HasModuleConfigValue(string groupName, string configName);

		/// <summary>
		/// Retrieves a module configuration setting.
		/// </summary>
		/// <typeparam name="T">The type of the setting.</typeparam>
		/// <param name="groupName">The group name of the setting.</param>
		/// <param name="configName">The name of the setting.</param>
		T GetModuleConfigValue<T>(string groupName, string configName) where T : notnull;

		/// <summary>
		/// Sets a module configuration setting.
		/// </summary>
		/// <typeparam name="T">The type of the setting.</typeparam>
		/// <param name="groupName">The group name of the setting.</param>
		/// <param name="configName">The name of the setting.</param>
		/// <param name="value">The value to set.</param>
		void SetModuleConfigValue<T>(string groupName, string configName, T value) where T : notnull;

		/// <summary>
		/// Retrieves a module configuration setting.
		/// </summary>
		IModuleConfigAccessor GetModuleConfigAccessor();

		/// <summary>
		/// Retrieves the event handler for the module.
		/// </summary>
		IZenithEvents GetEventHandler();

		/// <summary>
		/// Loads all player data from the database.
		/// </summary>
		void LoadAllOnlinePlayerData();

		/// <summary>
		/// Saves all player data to the database.
		/// </summary>
		void SaveAllOnlinePlayerData();

		/// <summary>
		/// Dispose the module's Zenith based resources such as commands, configs, and player datas.
		/// </summary>
		void DisposeModule(Assembly assembly);

		void ResetModuleStorage(ulong steamId);

		void ResetModuleSettings(ulong steamId);

		void ResetModuleSettings(CCSPlayerController player);

		void ResetModuleStorage(CCSPlayerController player);

		Task<T?> GetOfflineData<T>(ulong steamId, string tableName, string key);

		Task SetOfflineData(ulong steamId, string tableName, Dictionary<string, object?> data);
	}
}