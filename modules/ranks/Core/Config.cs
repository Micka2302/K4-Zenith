using CounterStrikeSharp.API.Core;
using ZenithAPI;

namespace Zenith_Ranks;

public sealed partial class Plugin : BasePlugin
{
	public ConfigHandler ConfigHandler { get; private set; } = null!;

	private void RegisterConfigs()
	{
		ConfigHandler = new ConfigHandler(this);

		// Register Command Configs
		ConfigHandler.Register("zenith-rank-commands", "Commands to show rank", new List<string> { "rank", "level" }, groupName: "commands");
		ConfigHandler.Register("zenith-ranks-commands", "Commands to show available ranks", new List<string> { "ranks" }, groupName: "commands");

		// Register General Configs
		ConfigHandler.Register("zenith-warmup-points", "Allow points during warmup", false, groupName: "settings");
		ConfigHandler.Register("zenith-point-summaries", "When enabled it blocks the realtime point changes and show only one message at the end of the round", false, groupName: "settings");
		ConfigHandler.Register("zenith-enable-requirement-messages", "Enable or disable the messages for points being disabled", true, groupName: "settings");
		ConfigHandler.Register("zenith-min-players", "Minimum players for points", 4, groupName: "settings");
		ConfigHandler.Register("zenith-points-for-bots", "Allow points for bot based events", false, groupName: "settings");
		ConfigHandler.Register("zenith-ffa-mode", "Free-for-all mode", false, groupName: "settings");
		ConfigHandler.Register("zenith-scoreboard-score-sync", "Sync the points with the score on scoreboard", false, groupName: "settings");
		ConfigHandler.Register("zenith-vip-multiplier", "VIP point multiplier", 1.25, groupName: "settings");
		ConfigHandler.Register("zenith-dynamic-death-points", "Enable or disable dynamic death points. When enabled the points are calculated based on killer and victim point differences", true, groupName: "settings");
		ConfigHandler.Register("zenith-dynamic-death-points-max-multiplier", "Max multiplier for dynamic death points", 3.0, groupName: "settings");
		ConfigHandler.Register("zenith-dynamic-death-points-min-multiplier", "Min multiplier for dynamic death points", 0.5, groupName: "settings");
		ConfigHandler.Register("zenith-use-scoreboard-ranks", "Use of rank images on scoreboard", true, groupName: "settings");
		ConfigHandler.Register("zenith-show-rank-changes", "Globally enable or disable rank change center messages", true, groupName: "settings");
		ConfigHandler.Register("zenith-scoreboard-mode", "Scoreboard mode (1 - premier, 2 - competitive, 3 - wingman, 4 - danger zone, 0 - custom)", 1, groupName: "settings");
		ConfigHandler.Register("zenith-rank-base", "Base rank value for custom ranks (Only required for custom scoreboard mode)", 0, groupName: "settings");
		ConfigHandler.Register("zenith-rank-max", "Maximum rank value for custom ranks (Only required for custom scoreboard mode)", 0, groupName: "settings");
		ConfigHandler.Register("zenith-rank-margin", "Rank margin value for custom ranks (Only required for custom scoreboard mode)", 0, groupName: "settings");
		ConfigHandler.Register("zenith-extended-death-messages", "Use extended death messages including enemy name and points in the message", true, groupName: "settings");
		ConfigHandler.Register("zenith-vip-flags", "VIP flags for multipliers", new List<string> { "@zenith-ranks/vip" }, groupName: "settings");

		// Register Point Configs
		ConfigHandler.Register("zenith-start-points", "Starting points for new players", 0, groupName: "points");
		ConfigHandler.Register("zenith-max-points", "Maximum points a player can earn (0 for no limit)", 0, groupName: "points");
		ConfigHandler.Register("zenith-death-point", "Points for death", -6, groupName: "points");
		ConfigHandler.Register("zenith-kill-point", "Points for kill", 6, groupName: "points");
		ConfigHandler.Register("zenith-headshot-point", "Extra points for headshot", 4, groupName: "points");
		ConfigHandler.Register("zenith-penetrated-point", "Extra points for penetration kill", 2, groupName: "points");
		ConfigHandler.Register("zenith-noscope-point", "Extra points for no-scope kill", 10, groupName: "points");
		ConfigHandler.Register("zenith-thrusmoke-point", "Extra points for kill through smoke", 8, groupName: "points");
		ConfigHandler.Register("zenith-blindkill-point", "Extra points for blind kill", 3, groupName: "points");
		ConfigHandler.Register("zenith-teamkill-point", "Points for team kill", -12, groupName: "points");
		ConfigHandler.Register("zenith-suicide-point", "Points for suicide", -7, groupName: "points");
		ConfigHandler.Register("zenith-assist-point", "Points for assist", 3, groupName: "points");
		ConfigHandler.Register("zenith-assistflash-point", "Points for flash assist", 4, groupName: "points");
		ConfigHandler.Register("zenith-teamkillassist-point", "Points for team kill assist", -5, groupName: "points");
		ConfigHandler.Register("zenith-teamkillassistflash-point", "Points for team kill flash assist", -3, groupName: "points");
		ConfigHandler.Register("zenith-roundwin-point", "Points for round win", 3, groupName: "points");
		ConfigHandler.Register("zenith-roundlose-point", "Points for round loss", -3, groupName: "points");
		ConfigHandler.Register("zenith-mvp-point", "Points for MVP", 8, groupName: "points");
		ConfigHandler.Register("zenith-bombdrop-point", "Points for dropping the bomb", -3, groupName: "points");
		ConfigHandler.Register("zenith-bombpickup-point", "Points for picking up the bomb", 1, groupName: "points");
		ConfigHandler.Register("zenith-bombdefused-point", "Points for defusing the bomb", 8, groupName: "points");
		ConfigHandler.Register("zenith-bombdefusedothers-point", "Points for others when bomb is defused", 2, groupName: "points");
		ConfigHandler.Register("zenith-bombplant-point", "Points for planting the bomb", 7, groupName: "points");
		ConfigHandler.Register("zenith-bombexploded-point", "Points for bomb explosion", 7, groupName: "points");
		ConfigHandler.Register("zenith-hostagehurt-point", "Points for hurting a hostage", -3, groupName: "points");
		ConfigHandler.Register("zenith-hostagekill-point", "Points for killing a hostage", -25, groupName: "points");
		ConfigHandler.Register("zenith-hostagerescue-point", "Points for rescuing a hostage", 12, groupName: "points");
		ConfigHandler.Register("zenith-hostagerescueall-point", "Extra points for rescuing all hostages", 8, groupName: "points");
		ConfigHandler.Register("zenith-longdistancekill-point", "Extra points for long-distance kill", 6, groupName: "points");
		ConfigHandler.Register("zenith-longdistance", "Distance for long-distance kill (units)", 30, groupName: "points");
		ConfigHandler.Register("zenith-secondsbetweenkills", "Seconds between kills for multi-kill bonuses", 0, groupName: "points");
		ConfigHandler.Register("zenith-roundendkillstreakreset", "Reset kill streak on round end", true, groupName: "points");
		ConfigHandler.Register("zenith-doublekill-point", "Points for double kill", 4, groupName: "points");
		ConfigHandler.Register("zenith-triplekill-point", "Points for triple kill", 8, groupName: "points");
		ConfigHandler.Register("zenith-domination-point", "Points for domination (4 kills)", 12, groupName: "points");
		ConfigHandler.Register("zenith-rampage-point", "Points for rampage (5 kills)", 16, groupName: "points");
		ConfigHandler.Register("zenith-megakill-point", "Points for mega kill (6 kills)", 20, groupName: "points");
		ConfigHandler.Register("zenith-ownage-point", "Points for ownage (7 kills)", 24, groupName: "points");
		ConfigHandler.Register("zenith-ultrakill-point", "Points for ultra kill (8 kills)", 28, groupName: "points");
		ConfigHandler.Register("zenith-killingspree-point", "Points for killing spree (9 kills)", 32, groupName: "points");
		ConfigHandler.Register("zenith-monsterkill-point", "Points for monster kill (10 kills)", 36, groupName: "points");
		ConfigHandler.Register("zenith-unstoppable-point", "Points for unstoppable (11 kills)", 40, groupName: "points");
		ConfigHandler.Register("zenith-godlike-point", "Points for godlike (12+ kills)", 45, groupName: "points");
		ConfigHandler.Register("zenith-grenadekill-point", "Points for grenade kill", 20, groupName: "points");
		ConfigHandler.Register("zenith-infernokill-point", "Points for inferno (molotov/incendiary) kill", 20, groupName: "points");
		ConfigHandler.Register("zenith-impactkill-point", "Points for impact kill", 50, groupName: "points");
		ConfigHandler.Register("zenith-taserkill-point", "Points for taser kill", 15, groupName: "points");
		ConfigHandler.Register("zenith-knifekill-point", "Points for knife kill", 12, groupName: "points");
		ConfigHandler.Register("zenith-playtimeinterval", "Interval for playtime points (in minutes), or 0 to disable", 10, groupName: "points");
		ConfigHandler.Register("zenith-playtimepoints", "Points for playtime interval", 10, groupName: "points");
	}
}