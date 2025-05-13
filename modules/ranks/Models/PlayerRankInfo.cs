namespace Zenith_Ranks;

public class PlayerRankInfo
{
	public Rank? Rank { get; set; }
	public Rank? NextRank { get; set; }
	public DateTime LastUpdate { get; set; }
	public KillStreakInfo KillStreak { get; set; } = new KillStreakInfo();
}

public class KillStreakInfo
{
	public int KillCount { get; set; }
	public long LastKillTime { get; set; }
}