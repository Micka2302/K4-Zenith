using CounterStrikeSharp.API.Core;
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

		void ResetModuleStorage(ulong steamId);

		void ResetModuleSettings(ulong steamId);

		void ResetModuleSettings(CCSPlayerController player);

		void ResetModuleStorage(CCSPlayerController player);

		Task<T?> GetOfflineData<T>(ulong steamId, string tableName, string key);

		Task SetOfflineData(ulong steamId, string tableName, Dictionary<string, object?> data);
	}
}