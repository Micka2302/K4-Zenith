using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using ZenithAPI;

namespace Zenith_Ranks;

public class EventManager
{
	private readonly Plugin _plugin;
	private readonly Dictionary<int, int> _killStreakPoints;

	public EventManager(Plugin plugin)
	{
		_plugin = plugin;
		_killStreakPoints = InitializeKillStreakPoints();
	}

	private Dictionary<int, int> InitializeKillStreakPoints()
	{
		return new Dictionary<int, int>
		{
			{ 2, _plugin._configAccessor.GetValue<int>("Points", "DoubleKill") },
			{ 3, _plugin._configAccessor.GetValue<int>("Points", "TripleKill") },
			{ 4, _plugin._configAccessor.GetValue<int>("Points", "Domination") },
			{ 5, _plugin._configAccessor.GetValue<int>("Points", "Rampage") },
			{ 6, _plugin._configAccessor.GetValue<int>("Points", "MegaKill") },
			{ 7, _plugin._configAccessor.GetValue<int>("Points", "Ownage") },
			{ 8, _plugin._configAccessor.GetValue<int>("Points", "UltraKill") },
			{ 9, _plugin._configAccessor.GetValue<int>("Points", "KillingSpree") },
			{ 10, _plugin._configAccessor.GetValue<int>("Points", "MonsterKill") },
			{ 11, _plugin._configAccessor.GetValue<int>("Points", "Unstoppable") },
			{ 12, _plugin._configAccessor.GetValue<int>("Points", "GodLike") }
		};
	}

	public void HandlePlayerDeathEvent(EventPlayerDeath? deathEvent)
	{
		if (deathEvent?.Userid == null) return;

		var victim = deathEvent.Userid != null ? _plugin._playerServicesCapability.GetZenithPlayer(deathEvent.Userid) : null;
		var attacker = deathEvent.Attacker != null ? _plugin._playerServicesCapability.GetZenithPlayer(deathEvent.Attacker) : null;
		var assister = deathEvent.Assister != null ? _plugin._playerServicesCapability.GetZenithPlayer(deathEvent.Assister) : null;

		if (victim != null)
		{
			HandleVictimDeath(victim, attacker, deathEvent);
		}

		if (attacker != null && attacker.Controller.SteamID != victim?.Controller.SteamID)
		{
			HandleAttackerKill(attacker, victim, deathEvent);
		}

		if (assister != null && assister.Controller.SteamID != deathEvent.Userid?.SteamID)
		{
			HandleAssisterEvent(assister, attacker, victim, deathEvent);
		}
	}

	private void HandleVictimDeath(IPlayerServices victim, IPlayerServices? attacker, EventPlayerDeath deathEvent)
	{
		if (deathEvent.Attacker == null || deathEvent.Attacker.SteamID == victim.Controller.SteamID)
		{
			if (!_plugin._isGameEnd)
				_plugin.ModifyPlayerPoints(victim, _plugin._configAccessor.GetValue<int>("Points", "Suicide"), "k4.events.suicide");
		}
		else
		{
			if (!_plugin._configAccessor.GetValue<bool>("Settings", "PointsForBots") && deathEvent.Attacker?.IsBot == true)
				return;

			string? eventInfo = attacker != null && _plugin._configAccessor.GetValue<bool>("Settings", "ExtendedDeathMessages")
				? (_plugin.Localizer.ForPlayer(victim.Controller, "k4.phrases.death-extended", attacker.Name, $"{attacker.GetStorage<long>("Points"):N0}") ?? string.Empty)
				: null;

			int points = attacker != null && _plugin._configAccessor.GetValue<bool>("Settings", "DynamicDeathPoints")
				? _plugin.CalculateDynamicPoints(attacker, victim, _plugin._configAccessor.GetValue<int>("Points", "Death"))
				: _plugin._configAccessor.GetValue<int>("Points", "Death");

			_plugin.ModifyPlayerPoints(victim, points, "k4.events.playerdeath", eventInfo);
		}

		ResetKillStreak(victim);
	}

	private void HandleAttackerKill(IPlayerServices attacker, IPlayerServices? victim, EventPlayerDeath deathEvent)
	{
		if (deathEvent.Userid == null || (!_plugin._configAccessor.GetValue<bool>("Settings", "PointsForBots") && deathEvent.Userid.IsBot))
			return;

		if (!_plugin._configAccessor.GetValue<bool>("Settings", "FFAMode") && attacker.Controller.Team == deathEvent.Userid?.Team)
		{
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "TeamKill"), "k4.events.teamkill");
		}
		else
		{
			HandleKillEvent(attacker, victim, deathEvent);
		}
	}

	private void HandleKillEvent(IPlayerServices attacker, IPlayerServices? victim, EventPlayerDeath deathEvent)
	{
		string? eventInfo = victim != null && _plugin._configAccessor.GetValue<bool>("Settings", "ExtendedDeathMessages")
			? (_plugin.Localizer.ForPlayer(attacker.Controller, "k4.phrases.kill-extended", victim.Name, $"{victim.GetStorage<long>("Points"):N0}") ?? string.Empty)
			: null;

		int points = _plugin._configAccessor.GetValue<bool>("Settings", "DynamicDeathPoints") && victim != null
			? _plugin.CalculateDynamicPoints(attacker, victim, _plugin._configAccessor.GetValue<int>("Points", "Kill"))
			: _plugin._configAccessor.GetValue<int>("Points", "Kill");

		_plugin.ModifyPlayerPoints(attacker, points, "k4.events.kill", eventInfo);

		HandleSpecialKillEvents(attacker, deathEvent);
		HandleKillStreak(attacker);
	}

	private void HandleSpecialKillEvents(IPlayerServices attacker, EventPlayerDeath deathEvent)
	{
		if (deathEvent.Headshot)
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "Headshot"), "k4.events.headshot");

		if (deathEvent.Penetrated > 0)
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "Penetrated") * deathEvent.Penetrated, "k4.events.penetrated");

		if (deathEvent.Noscope)
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "NoScope"), "k4.events.noscope");

		if (deathEvent.Thrusmoke)
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "Thrusmoke"), "k4.events.thrusmoke");

		if (deathEvent.Attackerblind)
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "BlindKill"), "k4.events.blindkill");

		if (deathEvent.Distance >= _plugin._configAccessor.GetValue<int>("Points", "LongDistance"))
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "LongDistanceKill"), "k4.events.longdistance");

		HandleSpecialWeaponKills(attacker, deathEvent.Weapon);
	}

	private void HandleSpecialWeaponKills(IPlayerServices attacker, string weapon)
	{
		string lowerCaseWeaponName = weapon.ToLower();

		if (lowerCaseWeaponName.Contains("hegrenade"))
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "GrenadeKill"), "k4.events.grenadekill");
		else if (lowerCaseWeaponName.Contains("inferno"))
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "InfernoKill"), "k4.events.infernokill");
		else if (lowerCaseWeaponName.Contains("grenade") || lowerCaseWeaponName.Contains("molotov") || lowerCaseWeaponName.Contains("flashbang") || lowerCaseWeaponName.Contains("bumpmine"))
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "ImpactKill"), "k4.events.impactkill");
		else if (lowerCaseWeaponName.Contains("knife") || lowerCaseWeaponName.Contains("bayonet"))
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "KnifeKill"), "k4.events.knifekill");
		else if (lowerCaseWeaponName == "taser")
			_plugin.ModifyPlayerPoints(attacker, _plugin._configAccessor.GetValue<int>("Points", "TaserKill"), "k4.events.taserkill");
	}

	public void HandleKillStreak(IPlayerServices attacker)
	{
		var playerData = _plugin.GetOrUpdatePlayerRankInfo(attacker);
		long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

		int timeBetweenKills = _plugin.GetCachedConfigValue<int>("Points", "SecondsBetweenKills");
		bool isValidStreak = timeBetweenKills <= 0 || (currentTime - playerData.KillStreak.LastKillTime <= timeBetweenKills);

		if (isValidStreak)
		{
			playerData.KillStreak.KillCount++;
			playerData.KillStreak.LastKillTime = currentTime;

			if (_killStreakPoints.TryGetValue(playerData.KillStreak.KillCount, out var streakPoints) && streakPoints != 0)
			{
				_plugin.ModifyPlayerPoints(attacker, streakPoints, $"k4.events.killstreak{playerData.KillStreak.KillCount}");
			}
		}
		else
		{
			playerData.KillStreak.KillCount = 1;
			playerData.KillStreak.LastKillTime = currentTime;
		}
	}

	public void ResetKillStreak(IPlayerServices player)
	{
		if (PlayerCacheManager.TryGetPlayer<PlayerRankInfo>(_plugin._moduleName, player.SteamID, out var playerData))
		{
			playerData.KillStreak = new KillStreakInfo();
			PlayerCacheManager.SetPlayer(_plugin._moduleName, player.SteamID, playerData);
		}
	}

	private void HandleAssisterEvent(IPlayerServices assister, IPlayerServices? attacker, IPlayerServices? victim, EventPlayerDeath deathEvent)
	{
		if (!_plugin._configAccessor.GetValue<bool>("Settings", "PointsForBots") && deathEvent.Userid?.IsBot == true)
			return;

		if (!_plugin._configAccessor.GetValue<bool>("Settings", "FFAMode") && attacker?.Controller.Team == deathEvent.Userid?.Team && assister.Controller.Team == deathEvent.Userid?.Team)
		{
			_plugin.ModifyPlayerPoints(assister, _plugin._configAccessor.GetValue<int>("Points", "TeamKillAssist"), "k4.events.teamkillassist");
			if (deathEvent.Assistedflash)
			{
				_plugin.ModifyPlayerPoints(assister, _plugin._configAccessor.GetValue<int>("Points", "TeamKillAssistFlash"), "k4.events.teamkillassistflash");
			}
		}
		else
		{
			string? eventInfo = victim != null && _plugin._configAccessor.GetValue<bool>("Settings", "ExtendedDeathMessages")
				? (_plugin.Localizer.ForPlayer(assister.Controller, "k4.phrases.assist-extended", victim.Name, $"{victim.GetStorage<long>("Points"):N0}") ?? string.Empty)
				: null;

			_plugin.ModifyPlayerPoints(assister, _plugin._configAccessor.GetValue<int>("Points", "Assist"), "k4.events.assist", eventInfo);
			if (deathEvent.Assistedflash)
			{
				_plugin.ModifyPlayerPoints(assister, _plugin._configAccessor.GetValue<int>("Points", "AssistFlash"), "k4.events.assistflash");
			}
		}
	}
}