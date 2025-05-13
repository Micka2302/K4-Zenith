using CounterStrikeSharp.API.Core;

namespace ZenithAPI
{
	public class ZenithPlayerPlaceholder
	{
		public required BasePlugin Module;
		public required string Placeholder;
		public required Func<CCSPlayerController, string> Callback;
	}

	public class ZenithServerPlaceholder
	{
		public required BasePlugin Module;
		public required string Placeholder;
		public required Func<string> Callback;
	}
}