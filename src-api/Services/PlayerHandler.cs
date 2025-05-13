using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;

namespace ZenithAPI
{
	public abstract class Player
	{
		public static ConcurrentDictionary<ulong, Player> List { get; } = new();

		public static Player? Find(CCSPlayerController? controller)
		{
			if (controller == null)
				return null;

			if (List.TryGetValue(controller.SteamID, out var player))
			{
				if (player.IsValid)
				{
					return player;
				}
				else
				{
					List.TryRemove(player.SteamID, out _);
				}
			}

			return null;
		}

		public static Player? Find(ulong steamid)
		{
			if (List.TryGetValue(steamid, out var player))
			{
				if (player.IsValid)
				{
					return player;
				}
				else
				{
					List.TryRemove(steamid, out _);
				}
			}
			return null;
		}

		public readonly CCSPlayerController Controller;
		public readonly ulong SteamID;
		public readonly string Name;

		public Player(CCSPlayerController controller)
		{
			Controller = controller;
			SteamID = controller?.SteamID ?? 0;
			Name = controller?.PlayerName ?? "Unknown";

			if (List.ContainsKey(SteamID))
				throw new Exception($"Player {Name} ({SteamID}) already exists in the 'Player' list.");

			this.Initialize();

			List[SteamID] = this;

			this.Synchronize();
		}

		public bool IsValid
			=> Controller.IsValid && Controller.PlayerPawn?.IsValid == true;

		public bool IsBot
			=> Controller.IsBot || Controller.IsHLTV;

		public bool IsAlive
			=> Controller.LifeState == (byte)LifeState_t.LIFE_ALIVE;

		public virtual void Initialize()
		{
			// Placeholder for initialization logic BEFORE added to list
			// Such as adding or setting up new values in the class
		}

		public virtual void Synchronize()
		{
			// Placeholder to load player data AFTER added to list
			// Such as database loads or sum
		}

		public virtual void Dispose()
		{
			List.TryRemove(SteamID, out _);
		}
	}
}