namespace Zenith
{
	using CounterStrikeSharp.API.Core;
	using CounterStrikeSharp.API.Modules.Commands;
	using Zenith.Models;

	public sealed partial class Plugin : BasePlugin
	{
		public void Initialize_Commands() // ? Decide whether or not its needed
		{
			CommandHandler.RegisterCommand("css_placeholderlist", "List all active placeholders in Zenith", (CCSPlayerController? player, CommandInfo command) =>
			{
				var playerPlaceholders = ZenithAPI.PlaceholderHandler.GetAllPlayerPlaceholders().GroupBy(p => p.Module);
				var serverPlaceholders = ZenithAPI.PlaceholderHandler.GetAllServerPlaceholders().GroupBy(p => p.Module);

				// log per module first it's player placeholders then server placeholders
				if (playerPlaceholders.Any())
				{
					foreach (var placeholderGroup in playerPlaceholders)
					{
						PrintToConsole($"Player placeholders for plugin '{placeholderGroup.Key.ModuleName}':", player);

						foreach (var ph in placeholderGroup)
						{
							PrintToConsole($"- {ph.Placeholder} ({ph.Callback})", player);
						}
					}
				}

				if (serverPlaceholders.Any())
				{
					foreach (var placeholderGroup in serverPlaceholders)
					{
						PrintToConsole($"Server placeholders for plugin '{placeholderGroup.Key.ModuleName}':", player);

						foreach (var ph in placeholderGroup)
						{
							PrintToConsole($"- {ph.Placeholder} ({ph.Callback})", player);
						}
					}
				}
			}, CommandUsage.CLIENT_AND_SERVER, permission: "@zenith/placeholders");

			CommandHandler.RegisterCommand("css_commandlist", "List all active commands in Zenith", (CCSPlayerController? player, CommandInfo command) =>
			{
				var commands = ZenithAPI.CommandHandler.GetAllCommands().GroupBy(c => c.Module);

				if (commands.Any())
				{
					foreach (var commandGroup in commands)
					{
						PrintToConsole($"Commands for plugin '{commandGroup.Key.ModuleName}':", player);

						foreach (var cmd in commandGroup)
						{
							PrintToConsole($"- {cmd.Command} ({cmd.Description}){(cmd.HelpText != null ? $" - Usage: {cmd.HelpText}" : "")}{(cmd.Permission != null ? $" - Permission: {cmd.Permission}" : "")}", player);
						}
					}
				}
				else
				{
					PrintToConsole($"No commands found for any Zenith plugin.", player);
				}
			}, CommandUsage.CLIENT_AND_SERVER, permission: "@zenith/commands");

			CommandHandler.RegisterCommand("css_zreload", "Reload Zenith configurations manually", (CCSPlayerController? player, CommandInfo command) =>
			{
				ConfigManager.ReloadAllConfigs();
				Player.Find(player)?.Print("Zenith configurations reloaded.");
			}, CommandUsage.CLIENT_AND_SERVER, permission: "@zenith/reload");

			CommandHandler.RegisterCommand("css_zresetall", "Reset Zenith storages for all players", (CCSPlayerController? player, CommandInfo command) =>
			{
				Player? caller = Player.Find(player);
				string argument = command.GetArg(1);

				Task.Run(async () => await Player.ResetModuleStorageAll(this, caller, argument));
			}, CommandUsage.CLIENT_AND_SERVER, 1, "[all|rank|stat|time]", permission: "@zenith/resetall");

			CommandHandler.RegisterCommand("css_zmigrate", "Migrate other supported plugins' sql data to Zenith", (CCSPlayerController? player, CommandInfo command) =>
			{
				Task.Run(async () => await TransferOldData());
			}, CommandUsage.SERVER_ONLY, permission: "@zenith/root");
		}
	}
}