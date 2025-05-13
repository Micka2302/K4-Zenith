using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Commands;

namespace ZenithAPI
{
	public class ZenithCommand
	{
		public required BasePlugin Module;
		public required string Command;
		public required string Description;
		public required CommandInfo.CommandCallback Callback;
		public CommandUsage Usage = CommandUsage.CLIENT_AND_SERVER;
		public int ArgCount = 0;
		public string? HelpText = null;
		public string? Permission = null;
		public CommandDefinition? CommandDefinition { get; set; }
	}
}