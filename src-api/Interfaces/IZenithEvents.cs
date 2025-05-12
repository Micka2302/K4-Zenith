using CounterStrikeSharp.API.Core;

namespace ZenithAPI
{
	public interface IZenithEvents
	{
		/// <summary>
		/// Occurs when a player is loaded and their data is downloaded.
		/// </summary>
		/// <param name="player">The player who was loaded.</param>
		event Action<CCSPlayerController> OnZenithPlayerLoaded;

		/// <summary>
		/// Occurs when a player is unloaded and their data is saved.
		/// </summary>
		/// <param name="player">The player who was unloaded.</param>
		event Action<CCSPlayerController> OnZenithPlayerUnloaded;

		/// <summary>
		/// Occurs when the Zenith storage is reset.
		/// </summary>
		/// <param name="moduleID">The ID of the module that reset the storage.</param>
		event Action<string> OnZenithStorageReset;

		/// <summary>
		/// Invokes the OnZenithCoreUnload event.
		/// </summary>
		/// <param name="hotReload">True if the core is being reloaded.</param>
		event Action<bool> OnZenithCoreUnload;

		/// <summary>
		/// Occurs when a player sends a chat message.
		/// </summary>
		/// <param name="player">The player who sent the message.</param>
		/// <param name="message">The message itself that was sent.</param>
		/// <param name="formattedMessage">The formatted message that was sent. (full line with prefix, colors, etc)</param>
		event Action<CCSPlayerController, string, string> OnZenithChatMessage;

		/// <summary>
		/// Occurs when a configuration value is changed.
		/// </summary>
		/// <param name="moduleName">The module that owns the configuration.</param>
		/// <param name="groupName">The group of the configuration.</param>
		/// <param name="configName">The name of the configuration.</param>
		/// <param name="newValue">The new value of the configuration.</param>
		event Action<string, string, string, object> OnZenithConfigChanged;
	}
}