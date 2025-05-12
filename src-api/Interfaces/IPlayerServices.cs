using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace ZenithAPI
{
	/// <summary>
	/// Provides services for managing player-specific data.
	/// </summary>
	public interface IPlayerServices // ! zenith:player-services
	{
		/// <summary>
		/// The player's controller.
		/// </summary>
		CCSPlayerController Controller { get; }

		/// <summary>
		/// The SteamID of the player.
		/// </summary>
		ulong SteamID { get; }

		/// <summary>
		/// The name of the player.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Checks if the player is valid and connected.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Checks if the player is alive.
		/// </summary>
		bool IsAlive { get; }

		/// <summary>
		/// Checks if the player is muted in voice chat.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Checks if the player is gagged in chat.
		/// </summary>
		bool IsGagged { get; }

		/// <summary>
		/// Sets the player's mute status.
		/// </summary>
		void SetMute(bool mute, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Sets the player's gag status.
		/// </summary>
		void SetGag(bool gag, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Prints a message to the player's chat.
		/// </summary>
		/// <param name="message">The message to print.</param>
		void Print(string message);

		/// <summary>
		/// Prints a message to the center of the player's screen.
		/// </summary>
		/// <param name="message">The message to print.</param>
		/// <param name="duration">The duration to display the message, in seconds.</param>
		/// <remarks>Duration defaults to 3 seconds if not specified.</remarks>
		void PrintToCenter(string message, int duration = 3, ActionPriority priority = ActionPriority.Low, bool showCloseCounter = false);

		/// <summary>
		/// Sets the player's clan tag.
		/// </summary>
		/// <param name="tag">The tag to set, or null to clear the tag.</param>
		/// <param name="priority">The priority of the action.</param>
		void SetClanTag(string? tag, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Sets the player's name tag.
		/// </summary>
		/// <param name="tag">The tag to set, or null to clear the tag.</param>
		/// <param name="priority">The priority of the action.</param>
		void SetNameTag(string? tag, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Sets the player's name color.
		/// </summary>
		/// <param name="color">The color to set, or null to clear the color.</param>
		/// <param name="priority">The priority of the action.</param>
		void SetNameColor(string? color, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Sets the player's chat color.
		/// </summary>
		/// <param name="color">The color to set, or null to clear the color.</param>
		/// <param name="priority">The priority of the action.</param>
		void SetChatColor(string? color, ActionPriority priority = ActionPriority.Low);

		/// <summary>
		/// Retrieves a setting value for a specific module and key.
		/// </summary>
		/// <param name="key">The key of the setting.</param>
		/// <returns>The value of the setting, or null if not found.</returns>
		T? GetSetting<T>(string key, string? moduleID = null);

		/// <summary>
		/// Sets a setting value for a specific module and key.
		/// </summary>
		/// <param name="key">The key of the setting.</param>
		/// <param name="value">The value to set.</param>
		/// <param name="saveImmediately">If true, saves the setting to the database immediately.</param>
		void SetSetting(string key, object? value, bool saveImmediately = false, string? moduleID = null);

		/// <summary>
		/// Retrieves a storage value for a specific module and key.
		/// </summary>
		/// <param name="key">The key of the storage item.</param>
		/// <returns>The value of the storage item, or null if not found.</returns>
		T? GetStorage<T>(string key, string? moduleID = null);

		/// <summary>
		/// Sets a storage value for a specific module and key.
		/// </summary>
		/// <param name="key">The key of the storage item.</param>
		/// <param name="value">The value to set.</param>
		/// <param name="saveImmediately">If true, saves the storage item to the database immediately.</param>
		void SetStorage(string key, object? value, bool saveImmediately = false, string? moduleID = null);

		/// <summary>
		/// Saves all settings and storage items, or those for a specific module.
		/// </summary>
		void Save();

		/// <summary>
		/// Loads all player data from the database.
		/// </summary>
		void LoadPlayerData();

		/// <summary>
		/// Resets the settings for a specific module to their default values.
		/// </summary>
		void ResetModuleSettings();

		/// <summary>
		/// Resets the storage items for a specific module to their default values.
		/// </summary>
		void ResetModuleStorage();

		/// <summary>
		/// Replaces the placeholders that are registered by modules or the Zenith core such as {name}, {ip}, etc
		/// </summary>
		/// <param name="player"></param>
		/// <param name="text"></param>
		/// <returns>Returns the replaced text</returns>
		string ReplacePlaceholders(string text);
	}
}