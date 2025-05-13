using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using ZenithAPI;

namespace Zenith_Ranks
{
	public sealed partial class Plugin : BasePlugin
	{
		private EventManager? _eventManager;
		private readonly Dictionary<string, (string targetProperty, int points)> _experienceEvents = new(StringComparer.OrdinalIgnoreCase);

		private void Initialize_Events()
		{
			_eventManager = new EventManager(this);

			RegisterListener<Listeners.OnMapStart>(OnMapStart);

			if (CoreConfig.FollowCS2ServerGuidelines)
			{
				Logger.LogWarning("CS2 server guidelines are enabled, disabling fake ranks functionality.");
				Logger.LogInformation("To enable fake ranks, set 'FollowCS2ServerGuidelines' to false in the Core configuration (configs/core.cfg).");
			}
			else
			{
				RegisterListener<Listeners.OnTick>(UpdateScoreboards);

				if (ConVar.Find("mp_halftime")?.GetPrimitiveValue<bool>() == false)
					Logger.LogWarning("Halftime is disabled, this may lead to scoreboard render issues.");
			}

			RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
			RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart, HookMode.Post);
			RegisterEventHandler<EventCsWinPanelMatch>(OnCsWinPanelMatch, HookMode.Post);
			RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);

			InitializeExperienceEvents();
		}

		private void OnMapStart(string mapName)
		{
			_isGameEnd = false;
			AddTimer(1.0f, () =>
			{
				GameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
			});
		}

		private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;

			bool resetKillStreaks = GetCachedConfigValue<bool>("Points", "RoundEndKillStreakReset");
			bool pointSummary = GetCachedConfigValue<bool>("Settings", "PointSummaries");

			foreach (var player in ZenithPlayer.GetValidPlayers())
			{
				if (resetKillStreaks)
					_eventManager?.ResetKillStreak(player);

				if (player.Controller.IsValid && player.Controller.Team > CsTeam.Spectator)
				{
					if (PlayerCacheManager.TryGetPlayer<PlayerExtendedData>(_moduleName, player.SteamID, out var playerData) && playerData.HasSpawned)
					{
						if (player.Controller.TeamNum == @event.Winner)
						{
							ModifyPlayerPoints(player, _configAccessor.GetValue<int>("Points", "RoundWin"), "k4.events.roundwin");
						}
						else
						{
							ModifyPlayerPoints(player, _configAccessor.GetValue<int>("Points", "RoundLose"), "k4.events.roundlose");
						}


						if (pointSummary)
						{
							if (player.GetSetting<bool>("ShowRankChanges"))
							{
								// Try to get the player's data from cache
								if (PlayerCacheManager.TryGetPlayer<PlayerExtendedData>(_moduleName, player.SteamID, out var extendedData)
									&& extendedData.RoundPoints != 0)
								{
									long currentPoints = player.GetStorage<long>("Points");
									int roundPoints = extendedData.RoundPoints;
									string message = roundPoints > 0 ?
										Localizer.ForPlayer(player.Controller, "k4.phrases.round-summary-earn", roundPoints, currentPoints) :
										Localizer.ForPlayer(player.Controller, "k4.phrases.round-summary-lose", Math.Abs(roundPoints), currentPoints);
									player.Print(message);
								}
							}
						}

						playerData.RoundPoints = 0;
						playerData.HasSpawned = false;
						PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, playerData);
					}
				}
			}

			return HookResult.Continue;
		}
		private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
		{
			_isGameEnd = false;
			return HookResult.Continue;
		}

		private HookResult OnCsWinPanelMatch(EventCsWinPanelMatch @event, GameEventInfo info)
		{
			_isGameEnd = true;
			return HookResult.Continue;
		}

		private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
		{
			HandlePlayerSpawn(@event.Userid);
			return HookResult.Continue;
		}

		private void InitializeExperienceEvents()
		{
			_experienceEvents.Clear();

			RegisterEventHandler<EventRoundMvp>(OnRoundMvp, HookMode.Post);
			RegisterEventHandler<EventHostageRescued>(OnHostageRescued, HookMode.Post);
			RegisterEventHandler<EventBombDefused>(OnBombDefused, HookMode.Post);
			RegisterEventHandler<EventBombPlanted>(OnBombPlanted, HookMode.Post);
			RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
			RegisterEventHandler<EventHostageKilled>(OnHostageKilled, HookMode.Post);
			RegisterEventHandler<EventHostageHurt>(OnHostageHurt, HookMode.Post);
			RegisterEventHandler<EventBombPickup>(OnBombPickup, HookMode.Post);
			RegisterEventHandler<EventBombDropped>(OnBombDropped, HookMode.Post);
			RegisterEventHandler<EventBombExploded>(OnBombExploded, HookMode.Post);
			RegisterEventHandler<EventHostageRescuedAll>(OnHostageRescuedAll, HookMode.Post);
		}

		private HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "MVP", "k4.events.roundmvp");
			return HookResult.Continue;
		}

		private HookResult OnHostageRescued(EventHostageRescued @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "HostageRescue", "k4.events.hostagerescued");
			return HookResult.Continue;
		}

		private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "BombDefused", "k4.events.bombdefused");
			return HookResult.Continue;
		}

		private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "BombPlant", "k4.events.bombplanted");
			return HookResult.Continue;
		}

		private HookResult OnHostageKilled(EventHostageKilled @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "HostageKill", "k4.events.hostagekilled");
			return HookResult.Continue;
		}

		private HookResult OnHostageHurt(EventHostageHurt @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "HostageHurt", "k4.events.hostagehurt");
			return HookResult.Continue;
		}

		private HookResult OnBombPickup(EventBombPickup @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "BombPickup", "k4.events.bombpickup");
			return HookResult.Continue;
		}

		private HookResult OnBombDropped(EventBombDropped @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyPlayerPointsForEvent(@event.Userid, "BombDrop", "k4.events.bombdropped");
			return HookResult.Continue;
		}

		private HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyTeamPointsForEvent(CsTeam.Terrorist, "BombExploded", "k4.events.bombexploded");
			return HookResult.Continue;
		}

		private HookResult OnHostageRescuedAll(EventHostageRescuedAll @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			ModifyTeamPointsForEvent(CsTeam.CounterTerrorist, "HostageRescueAll", "k4.events.hostagerescuedall");
			return HookResult.Continue;
		}

		private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
		{
			if (!ShouldProcessEvent()) return HookResult.Continue;
			_eventManager?.HandlePlayerDeathEvent(@event);
			return HookResult.Continue;
		}

		private DateTime _lastCheckTime = DateTime.MinValue;
		private bool _lastProcessResult;
		private const int CACHE_UPDATE_INTERVAL_SECONDS = 5;

		private bool ShouldProcessEvent()
		{
			var currentTime = DateTime.UtcNow;
			if ((currentTime - _lastCheckTime).TotalSeconds < CACHE_UPDATE_INTERVAL_SECONDS)
			{
				return _lastProcessResult;
			}

			_lastProcessResult = GetCachedConfigValue<int>("Settings", "MinPlayers") <= Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && !p.IsHLTV) &&
								(GetCachedConfigValue<bool>("Settings", "WarmupPoints") || GameRules?.WarmupPeriod != true);
			_lastCheckTime = currentTime;

			return _lastProcessResult;
		}
		private void ModifyPlayerPointsForEvent(CCSPlayerController? player, string pointsKey, string eventKey)
		{
			if (player == null)
				return;

			var playerServices = _playerServicesCapability.GetZenithPlayer(player);
			if (playerServices != null)
			{
				// Check if the player has spawned this round
				if (PlayerCacheManager.TryGetPlayer<PlayerExtendedData>(_moduleName, player.SteamID, out var extendedData) && extendedData.HasSpawned)
				{
					int points = _configAccessor.GetValue<int>("Points", pointsKey);
					ModifyPlayerPoints(playerServices, points, eventKey);
				}
			}
		}
		private void ModifyTeamPointsForEvent(CsTeam team, string pointsKey, string eventKey)
		{
			int points = _configAccessor.GetValue<int>("Points", pointsKey);
			foreach (var player in GetValidPlayers())
			{
				if (player.Controller.Team == team)
				{
					// Check if the player has spawned this round
					if (PlayerCacheManager.TryGetPlayer<PlayerExtendedData>(_moduleName, player.SteamID, out var extendedData) && extendedData.HasSpawned)
					{
						ModifyPlayerPoints(player, points, eventKey);
					}
				}
			}
		}

		internal static void SetCompetitiveRank(IPlayerServices player, int mode, int rankId, long currentPoints, int rankMax, int rankBase, int rankMargin)
		{
			player.Controller.CompetitiveWins = 10;

			switch (mode)
			{
				case 1:
					player.Controller.CompetitiveRanking = (int)currentPoints;
					player.Controller.CompetitiveRankType = 11;
					break;
				case 2:
				case 3:
					player.Controller.CompetitiveRanking = Math.Min(rankId, 18);
					player.Controller.CompetitiveRankType = (sbyte)(mode == 2 ? 12 : 7);
					break;
				case 4:
					player.Controller.CompetitiveRanking = Math.Min(rankId, 15);
					player.Controller.CompetitiveRankType = 10;
					break;
				default:
					int rank = rankId > rankMax ? rankBase + rankMax - rankMargin : rankBase + (rankId - rankMargin - 1);
					player.Controller.CompetitiveRanking = rank;
					player.Controller.CompetitiveRankType = 12;
					break;
			}
		}
		private void HandlePlayerSpawn(CCSPlayerController? player)
		{
			if (player == null || player.IsBot || player.IsHLTV)
				return;

			if (_configAccessor.GetValue<bool>("Settings", "EnableRequirementMessages"))
			{
				int requiredPlayers = _configAccessor.GetValue<int>("Settings", "MinPlayers");
				if (requiredPlayers > Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && !p.IsHLTV))
				{
					_moduleServices?.PrintForPlayer(player, Localizer.ForPlayer(player, "k4.phrases.points_disabled", requiredPlayers));
				}
			}

			// Set HasSpawned to true for this player
			var extendedData = PlayerCacheManager.GetOrAddPlayer<PlayerExtendedData>(_moduleName, player.SteamID, _ => new PlayerExtendedData());
			extendedData.HasSpawned = true;
			PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, extendedData);
		}
	}
}