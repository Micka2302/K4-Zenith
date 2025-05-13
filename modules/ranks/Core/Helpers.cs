using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using ZenithAPI;

namespace Zenith_Ranks;

public sealed partial class Plugin : BasePlugin
{
	public void ModifyPlayerPoints(IPlayerServices player, int points, string eventKey, string? extraInfo = null)
	{
		if (points == 0) return;

		var vipFlags = GetCachedConfigValue<List<string>>("Settings", "VIPFlags");
		var vipMultiplier = (decimal)GetCachedConfigValue<double>("Settings", "VipMultiplier");
		bool scoreboardSync = GetCachedConfigValue<bool>("Settings", "ScoreboardScoreSync");
		bool showSummaries = GetCachedConfigValue<bool>("Settings", "PointSummaries");

		var playerData = GetOrUpdatePlayerRankInfo(player);

		if (points > 0 && vipFlags.Any(f => AdminManager.PlayerHasPermissions(player.Controller, f)))
		{
			points = (int)(points * vipMultiplier);
		}

		long currentPoints = player.GetStorage<long>("Points");
		long maxPoints = GetCachedConfigValue<long>("Settings", "MaxPoints");

		long newPoints = Math.Clamp(currentPoints + points, 0, maxPoints > 0 ? maxPoints : long.MaxValue);

		if (currentPoints == newPoints)
			return;

		// Update storage and cache in one call
		SetPlayerPointsWithCache(player, newPoints);

		if (scoreboardSync && player.Controller.Score != (int)newPoints)
		{
			player.Controller.Score = (int)newPoints;
		}

		if (showSummaries || !player.GetSetting<bool>("ShowRankChanges"))
		{
			// Get the player's extended data to store round points
			var extendedData = PlayerCacheManager.GetOrAddPlayer(_moduleName, player.SteamID, _ => new PlayerExtendedData());

			// Update round points for the player
			extendedData.RoundPoints += points;

			// Save back to the cache
			PlayerCacheManager.SetPlayer(_moduleName, player.SteamID, extendedData);
		}
		else
		{
			string message = Localizer.ForPlayer(player.Controller, points >= 0 ? "k4.phrases.gain" : "k4.phrases.loss",
				$"{newPoints:N0}", Math.Abs(points), extraInfo ?? Localizer.ForPlayer(player.Controller, eventKey));

			Server.NextFrame(() => player.Print(message));
		}
	}

	private void UpdatePlayerRank(IPlayerServices player, PlayerRankInfo playerData, long points)
	{
		var (determinedRank, nextRank) = DetermineRanks(points);

		if (determinedRank?.Id != playerData.Rank?.Id)
		{
			string newRankName = determinedRank?.Name ?? Localizer.ForPlayer(player.Controller, "k4.phrases.rank.none");
			player.SetStorage("Rank", newRankName);

			bool isRankUp = playerData.Rank is null || CompareRanks(determinedRank, playerData.Rank) > 0;

			playerData.Rank = determinedRank;
			playerData.NextRank = nextRank;
			playerData.LastUpdate = DateTime.Now;

			if (!GetCachedConfigValue<bool>("Settings", "ShowRankChanges"))
				return;

			string htmlMessage = $@"
            <font color='{(isRankUp ? "#00FF00" : "#FF0000")}' class='fontSize-m'>{Localizer.ForPlayer(player.Controller, isRankUp ? "k4.phrases.rankup" : "k4.phrases.rankdown")}</font><br>
            <font color='{determinedRank?.HexColor}' class='fontSize-m'>{Localizer.ForPlayer(player.Controller, "k4.phrases.newrank", newRankName)}</font>";

			player.PrintToCenter(htmlMessage, _configAccessor.GetValue<int>("Core", "CenterAlertTime"), ActionPriority.Normal);
		}
	}

	private static int CompareRanks(Rank? rank1, Rank? rank2)
	{
		if (rank1 == rank2) return 0;

		if (rank1 == null) return rank2 == null ? 0 : -1;
		if (rank2 == null) return 1;

		return rank1.Point.CompareTo(rank2.Point);
	}

	internal PlayerRankInfo GetOrUpdatePlayerRankInfo(IPlayerServices player)
	{
		return PlayerCacheManager.GetOrAddPlayer(_moduleName, player.SteamID, (steamId) =>
		{
			var currentPoints = player.GetStorage<long>("Points");
			var (determinedRank, nextRank) = DetermineRanks(currentPoints);

			return new PlayerRankInfo
			{
				Rank = determinedRank,
				NextRank = nextRank,
				LastUpdate = DateTime.Now,
				KillStreak = new KillStreakInfo()
			};
		});
	}

	internal (Rank? CurrentRank, Rank? NextRank) DetermineRanks(long points)
	{
		if (Ranks.Count == 0)
			return (null, null);

		int low = 0;
		int high = Ranks.Count - 1;
		int bestRankIndex = -1;

		// Binary search to find the highest rank that the player qualifies for
		while (low <= high)
		{
			int mid = low + (high - low) / 2;

			if (Ranks[mid].Point <= points)
			{
				bestRankIndex = mid;
				low = mid + 1;  // Look for a higher rank
			}
			else
			{
				high = mid - 1;  // Look for a lower rank
			}
		}

		// Determine current and next rank
		Rank? currentRank = bestRankIndex >= 0 ? Ranks[bestRankIndex] : null;
		Rank? nextRank = bestRankIndex + 1 < Ranks.Count ? Ranks[bestRankIndex + 1] : null;

		return (currentRank, nextRank);
	}

	public int CalculateDynamicPoints(IPlayerServices attacker, IPlayerServices victim, int basePoints)
	{
		// Fast path return if basePoints is 0
		if (basePoints == 0) return 0;

		// Check if dynamic points are enabled directly from config cache
		bool dynamicPointsEnabled = GetCachedConfigValue<bool>("Settings", "DynamicDeathPoints");

		// Return base points if dynamic points are disabled
		if (!dynamicPointsEnabled)
			return basePoints;

		// Get points from players
		long attackerPoints = attacker.GetStorage<long>("Points");
		long victimPoints = victim.GetStorage<long>("Points");

		// Another fast path for simple cases
		if (attackerPoints <= 0 || victimPoints <= 0)
			return basePoints;

		// Calculate the result directly - no need to cache since player points change frequently
		double minMultiplier = GetCachedConfigValue<double>("Settings", "DynamicDeathPointsMinMultiplier");
		double maxMultiplier = GetCachedConfigValue<double>("Settings", "DynamicDeathPointsMaxMultiplier");

		double pointsRatio = Math.Clamp(victimPoints / (double)attackerPoints, minMultiplier, maxMultiplier);
		int result = (int)Math.Round(pointsRatio * basePoints);

		return result;
	}

	private void UpdateScoreboards()
	{
		if (!GetCachedConfigValue<bool>("Settings", "UseScoreboardRanks"))
			return;

		int mode = GetCachedConfigValue<int>("Settings", "ScoreboardMode");
		int rankMax = GetCachedConfigValue<int>("Settings", "RankMax");
		int rankBase = GetCachedConfigValue<int>("Settings", "RankBase");
		int rankMargin = GetCachedConfigValue<int>("Settings", "RankMargin");

		foreach (var player in ZenithPlayer.GetValidPlayers())
		{
			long currentPoints = Math.Max(1, player.GetStorage<long>("Points"));

			var playerData = GetOrUpdatePlayerRankInfo(player);
			SetCompetitiveRank(player, mode, playerData.Rank?.Id ?? 0, currentPoints, rankMax, rankBase, rankMargin);
		}
	}
}