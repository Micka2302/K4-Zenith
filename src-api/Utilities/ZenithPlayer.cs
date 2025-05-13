
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace ZenithAPI
{
    public static class ZenithPlayer
    {
        public static IEnumerable<CCSPlayerController> GetValidPlayers()
        {
            var players = Utilities.GetPlayers();

            foreach (var player in players)
            {
                if (player.IsBot || player.IsHLTV)
                    continue;

                yield return player;
            }
        }
    }
}