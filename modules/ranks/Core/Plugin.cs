using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Menu;
using Microsoft.Extensions.Logging;
using ZenithAPI;

namespace Zenith_Ranks;

[MinimumApiVersion(300)]
public sealed partial class Plugin : BasePlugin
{
	public override string ModuleName => $"K4-Zenith | Ranks";
	public override string ModuleAuthor => "K4ryuu @ KitsuneLab";
	public override string ModuleVersion => "2.0.0";

	internal string _moduleName = Assembly.GetExecutingAssembly().GetName().Name!;

	internal PlayerCapability<IPlayerServices>? _playerServicesCapability;
	private PluginCapability<IModuleServices>? _moduleServicesCapability;
	private DateTime _lastPlaytimeCheck = DateTime.Now;
	public KitsuneMenu Menu { get; private set; } = null!;

	public CCSGameRules? GameRules { get; private set; }
	private IZenithEvents? _zenithEvents;
	private IModuleServices? _moduleServices;
	internal bool _isGameEnd;
	public IModuleConfigAccessor _coreAccessor = null!;

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		if (!InitializeZenithAPI())
			return;

		RegisterConfigs();
		RegisterModuleSettings();
		RegisterModuleStorage();
		RegisterPlaceholders();
		RegisterCommands();

		Initialize_Ranks();
		Initialize_Events();

		SetupZenithEvents();
		SetupGameRules(hotReload);

		Menu = new KitsuneMenu(this);
		_coreAccessor = _moduleServices!.GetModuleConfigAccessor();

		if (hotReload)
		{
			_moduleServices!.LoadAllOnlinePlayerData();

			var players = Utilities.GetPlayers();
			foreach (var player in players)
			{
				if (player != null && player.IsValid && !player.IsBot && !player.IsHLTV)
					OnZenithPlayerLoaded(player);
			}
		}

		AddTimer(5.0f, () =>
		{
			UserMessage message = UserMessage.FromId(350);
			message.Recipients.AddAllPlayers();
			message.Send();
		}, TimerFlags.REPEAT);

		AddTimer(5.0f, () =>
		{
			try
			{
				// Clean up expired entries in the central cache
				// Use the stored module name
				PlayerCacheManager.CleanupExpiredPlayers(_moduleName);

				// Handle playtime rewards
				int interval = GetCachedConfigValue<int>("Points", "PlaytimeInterval");
				if (interval <= 0) return;

				if ((DateTime.Now - _lastPlaytimeCheck).TotalMinutes >= interval)
				{
					int playtimePoints = GetCachedConfigValue<int>("Points", "PlaytimePoints");
					foreach (var player in GetValidPlayers())
					{
						ModifyPlayerPoints(player, playtimePoints, "k4.events.playtime");
					}
					_lastPlaytimeCheck = DateTime.Now;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"Error occurred during background tasks: {ex.Message}");
			}
		}, TimerFlags.REPEAT);

		Logger.LogInformation("{0} module successfully registered.", ModuleName);
	}

	internal T GetCachedConfigValue<T>(string section, string key) where T : notnull
	{
		try
		{
			// Use the centralized config cache with stored module name
			return ConfigCacheManager.GetOrAddValue<T>(
				_moduleName,
				section,
				key,
				() => _coreAccessor.GetValue<T>(section, key)
			);
		}
		catch (Exception ex)
		{
			Logger.LogError($"Error retrieving config value {section}.{key}: {ex.Message}");

			// Try to get the value directly from the config as fallback
			try
			{
				return _coreAccessor.GetValue<T>(section, key);
			}
			catch
			{
				// Re-throw the original exception if we can't recover
				throw;
			}
		}
	}

	private bool InitializeZenithAPI()
	{
		try
		{
			_playerServicesCapability = new("zenith:player-services");
			_moduleServicesCapability = new("zenith:module-services");
			_moduleServices = _moduleServicesCapability.Get();

			if (_moduleServices == null)
				throw new InvalidOperationException("Failed to get Module-Services API for Zenith.");

			// Register with the central player cache system
			PlayerCacheManager.RegisterModule(_moduleName, TimeSpan.FromSeconds(5));

			// Register with the central config cache system
			ConfigCacheManager.RegisterModule(_moduleName);

			return true;
		}
		catch (Exception ex)
		{
			Logger.LogError($"Failed to initialize Zenith API: {ex.Message}");
			Logger.LogInformation("Please check if Zenith is installed, configured and loaded correctly.");
			UnloadPlugin();
			return false;
		}
	}

	private void RegisterModuleSettings()
	{
		_moduleServices!.RegisterModuleSettings(new Dictionary<string, object?>
		{
			{ "ShowRankChanges", true },
		}, Localizer);
	}

	public Dictionary<string, object?> _defaultStorage = [];

	private void RegisterModuleStorage()
	{
		_defaultStorage = new Dictionary<string, object?>
		{
			{ "Points", _configAccessor.GetValue<long>("Settings", "StartPoints") },
			{ "Rank", "k4.phrases.rank.none" }
		};

		_moduleServices!.RegisterModuleStorage(_defaultStorage);
	}

	private void RegisterPlaceholders()
	{
		_moduleServices!.RegisterModulePlayerPlaceholder("rank_color", GetRankColor);
		_moduleServices.RegisterModulePlayerPlaceholder("rank", GetRankName);
		_moduleServices.RegisterModulePlayerPlaceholder("points", GetPlayerPoints);
	}

	private void RegisterCommands()
	{
		_moduleServices!.RegisterModuleCommands(_configAccessor.GetValue<List<string>>("Commands", "RankCommands"), "Show the rank informations.", OnRankCommand, CommandUsage.CLIENT_ONLY);
		_moduleServices!.RegisterModuleCommands(["zgivepoint", "zgivepoints"], "Gives Zenith Rank point to the player.", OnGivePoints, CommandUsage.CLIENT_AND_SERVER, 2, "<target> <amount>", "@zenith/point-admin");
		_moduleServices!.RegisterModuleCommands(["ztakepoint", "ztakepoints"], "Takes Zenith Rank point from the player.", OnTakePoints, CommandUsage.CLIENT_AND_SERVER, 2, "<target> <amount>", "@zenith/point-admin");
		_moduleServices!.RegisterModuleCommands(["zsetpoint", "zsetpoints"], "Sets Zenith Rank point for the player.", OnSetPoints, CommandUsage.CLIENT_AND_SERVER, 2, "<target> <amount>", "@zenith/point-admin");
		_moduleServices!.RegisterModuleCommands(["zresetpoint", "zresetpoints"], "Resets Zenith storages for the player.", OnResetPoints, CommandUsage.CLIENT_AND_SERVER, 1, "<target>", "@zenith/point-admin");
		_moduleServices!.RegisterModuleCommands(["ranks"], "Shows the rank informations.", OnRanksCommand, CommandUsage.CLIENT_ONLY);
	}

	private void SetupZenithEvents()
	{
		_zenithEvents = _moduleServices!.GetEventHandler();
		if (_zenithEvents != null)
		{
			_zenithEvents.OnZenithPlayerLoaded += OnZenithPlayerLoaded;
			_zenithEvents.OnZenithPlayerUnloaded += OnZenithPlayerUnloaded;
			_zenithEvents.OnZenithCoreUnload += OnZenithCoreUnload;
			_zenithEvents.OnZenithStorageReset += OnZenithStorageReset;
		}
		else
		{
			Logger.LogError("Failed to get Zenith event handler.");
		}
	}

	private void SetupGameRules(bool hotReload)
	{
		if (hotReload)
			GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
	}

	private void OnZenithPlayerLoaded(CCSPlayerController player)
	{
		var handler = _playerServicesCapability.GetZenithPlayer(player);
		if (handler == null)
		{
			Logger.LogError($"Failed to get player services for {player.PlayerName}");
			return;
		}

		// Initialize the player in the central cache by accessing their data
		long currentPoints = handler.GetStorage<long>("Points");
		var (determinedRank, nextRank) = DetermineRanks(currentPoints);

		// Store rank information in cache
		PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, new PlayerRankInfo
		{
			Rank = determinedRank,
			NextRank = nextRank,
			LastUpdate = DateTime.Now,
			KillStreak = new KillStreakInfo()
		});

		// Store extended player data in cache
		PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, new PlayerExtendedData
		{
			HasSpawned = false,
			RoundPoints = 0
		});
	}

	private void OnZenithPlayerUnloaded(CCSPlayerController player)
	{
		// Explicitly remove from central cache when player leaves
		PlayerCacheManager.RemovePlayer(_moduleName, player.SteamID);
	}

	private void OnZenithCoreUnload(bool hotReload)
	{
		if (hotReload)
		{
			AddTimer(3.0f, () =>
			{
				try { File.SetLastWriteTime(ModulePath, DateTime.Now); }
				catch (Exception ex) { Logger.LogError($"Failed to update file: {ex.Message}"); }
			});
		}
	}

	public override void Unload(bool hotReload)
	{
		// Unsubscribe from events when module is unloaded
		if (_zenithEvents != null)
		{
			_zenithEvents.OnZenithPlayerLoaded -= OnZenithPlayerLoaded;
			_zenithEvents.OnZenithPlayerUnloaded -= OnZenithPlayerUnloaded;
			_zenithEvents.OnZenithCoreUnload -= OnZenithCoreUnload;
			_zenithEvents.OnZenithStorageReset -= OnZenithStorageReset;
		}

		// Clean up module data in the centralized caches
		PlayerCacheManager.CleanupModule(_moduleName);
		ConfigCacheManager.InvalidateAllForModule(_moduleName);

		_moduleServicesCapability?.Get()?.DisposeModule(GetType().Assembly);
	}

	private void OnZenithStorageReset(string moduleID)
	{
		if (moduleID == _moduleName)
		{
			// Clear central cache data
			PlayerCacheManager.CleanupModule(_moduleName);
			ConfigCacheManager.InvalidateAllForModule(_moduleName);
		}
	}

	private void SetPlayerPointsWithCache(IPlayerServices player, long points)
	{
		// Update storage value
		player.SetStorage("Points", points);

		// Update rank info in cache as the points have changed
		long currentPoints = points;
		var (determinedRank, nextRank) = DetermineRanks(currentPoints);

		PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, new PlayerRankInfo
		{
			Rank = determinedRank,
			NextRank = nextRank,
			LastUpdate = DateTime.Now,
			KillStreak = GetPlayerKillStreak(player)
		});
	}

	// Get player's kill streak from cache
	private KillStreakInfo GetPlayerKillStreak(IPlayerServices player)
	{
		if (PlayerCacheManager.TryGetPlayer<PlayerRankInfo>(_moduleName, player.SteamID, out var rankInfo))
		{
			return rankInfo.KillStreak;
		}
		return new KillStreakInfo();
	}

	private void UnloadPlugin()
	{
		Server.ExecuteCommand($"css_plugins unload {Path.GetFileNameWithoutExtension(ModulePath)}");
	}

	private string GetRankColor(CCSPlayerController p)
	{
		var player = _playerServicesCapability.GetZenithPlayer(p);
		if (player != null)
		{
			var playerData = GetOrUpdatePlayerRankInfo(player);
			return playerData.Rank?.ChatColor.ToString() ?? ChatColors.Default.ToString();
		}

		return ChatColors.Default.ToString();
	}

	private string GetRankName(CCSPlayerController p)
	{
		var player = _playerServicesCapability.GetZenithPlayer(p);
		if (player != null)
		{
			var playerData = GetOrUpdatePlayerRankInfo(player);
			return Localizer.ForPlayer(p, playerData.Rank?.Name ?? "k4.phrases.rank.none") ?? Localizer.ForPlayer(p, "k4.phrases.rank.none");
		}

		return Localizer.ForPlayer(p, "k4.phrases.rank.none");
	}

	private string GetPlayerPoints(CCSPlayerController p)
	{
		var player = _playerServicesCapability.GetZenithPlayer(p);
		if (player != null)
			return player.GetStorage<long>("Points").ToString();

		return "0";
	}
}