using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;

namespace Zenith.Models;

public sealed partial class Player
{
	public static ConcurrentDictionary<ulong, Player> List { get; } = new ConcurrentDictionary<ulong, Player>();

	// Additional dictionary for O(1) lookup by controller
	private static readonly ConcurrentDictionary<CCSPlayerController, Player> ControllerMap = new();

	public static Player? Find(CCSPlayerController? controller)
	{
		if (controller == null)
			return null;

		if (ControllerMap.TryGetValue(controller, out var player) && player.IsValid)
			return player;

		player?.Dispose();
		return null;
	}

	public static void AddToList(Player player)
	{
		List[player.SteamID] = player;

		// Add to controller map for O(1) lookups
		if (player.Controller != null)
			ControllerMap[player.Controller] = player;
	}

	public static void RemoveFromList(ulong playerToRemove)
	{
		if (List.TryGetValue(playerToRemove, out var player) && player.Controller != null)
			ControllerMap.TryRemove(player.Controller, out _);

		List.TryRemove(playerToRemove, out _);
	}
}