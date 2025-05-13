using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Translations;

namespace ZenithAPI
{
	/// <summary>
	/// Handles command registration and execution for Zenith plugins
	/// </summary>
	/// <remarks>
	/// Creates a new command handler for the specified plugin
	/// </remarks>
	/// <param name="plugin">The plugin context</param>
	public sealed partial class CommandHandler(BasePlugin plugin)
	{
		private static readonly ConcurrentDictionary<BasePlugin, List<ZenithCommand>> _commands = new();

		private readonly BasePlugin _plugin = plugin;
		private readonly IStringLocalizer _localizer = plugin.Localizer;
		private readonly ILogger _logger = plugin.Logger;

		public void RegisterCommand(string command, string description, CommandInfo.CommandCallback handler, CommandUsage usage = CommandUsage.CLIENT_AND_SERVER, int argCount = 0, string? helpText = null, string? permission = null)
		{
			command = EnsurePrefix(command);

			var existingCommand = FindCommand(command);

			if (existingCommand != null)
			{
				if (existingCommand.Module != _plugin)
				{
					_logger.LogError($"Command '{command}' is already registered by plugin '{existingCommand.Module.ModuleName}'. The command cannot be registered by '{_plugin.ModuleName}'.");
					return;
				}

				RemoveExistingCommand(existingCommand);
				_logger.LogWarning($"Command '{command}' already exists for plugin '{_plugin.ModuleName}', overwriting.");
			}

			RegisterNewCommand(command, description, handler, usage, argCount, helpText, permission);
		}

		public void RegisterCommand(List<string> commands, string description, CommandInfo.CommandCallback handler, CommandUsage usage = CommandUsage.CLIENT_AND_SERVER, int argCount = 0, string? helpText = null, string? permission = null)
		{
			foreach (var command in commands)
			{
				RegisterCommand(command, description, handler, usage, argCount, helpText, permission);
			}
		}

		private static string EnsurePrefix(string command)
			=> command.StartsWith("css_") ? command : "css_" + command;

		private static ZenithCommand? FindCommand(string command)
			=> _commands.SelectMany(kvp => kvp.Value.Where(cmd => cmd.Command == command)).FirstOrDefault();

		private void RemoveExistingCommand(ZenithCommand existingCommand)
		{
			if (existingCommand.CommandDefinition != null)
				_plugin.CommandManager.RemoveCommand(existingCommand.CommandDefinition);

			_commands[existingCommand.Module].Remove(existingCommand);
		}

		private void RegisterNewCommand(string command, string description, CommandInfo.CommandCallback handler, CommandUsage usage, int argCount, string? helpText, string? permission)
		{
			var newCommand = new CommandDefinition(command, description, (controller, info) =>
			{
				if (!CommandHelper(controller, info, usage, argCount, helpText, permission))
					return;

				handler(controller, info);
			});

			_plugin.CommandManager.RegisterCommand(newCommand);

			var zenithCommand = new ZenithCommand
			{
				Module = _plugin,
				Command = command,
				Description = description,
				Callback = handler,
				Usage = usage,
				ArgCount = argCount,
				HelpText = helpText,
				Permission = permission,
				CommandDefinition = newCommand
			};

			_commands.GetOrAdd(_plugin, _ => []).Add(zenithCommand);
		}

		public void RemoveCommands()
		{
			if (_commands.TryGetValue(_plugin, out var pluginCommands))
			{
				foreach (var command in pluginCommands)
				{
					if (command.CommandDefinition == null)
						continue;

					_plugin.CommandManager.RemoveCommand(command.CommandDefinition);
				}

				_commands.TryRemove(_plugin, out _);
			}
		}

		public static void RemoveAllCommands()
		{
			foreach (var pluginEntry in _commands)
			{
				foreach (var command in pluginEntry.Value)
				{
					if (command.CommandDefinition == null)
						continue;

					command.Module.CommandManager.RemoveCommand(command.CommandDefinition);
				}

				_commands.TryRemove(pluginEntry.Key, out _);
			}
		}

		public IReadOnlyList<ZenithCommand> GetCommands()
		{
			if (_commands.TryGetValue(_plugin, out var pluginCommands))
				return pluginCommands;

			return [];
		}

		public static IReadOnlyList<ZenithCommand> GetAllCommands()
		{
			var allCommands = new List<ZenithCommand>();

			foreach (var pluginEntry in _commands)
			{
				allCommands.AddRange(pluginEntry.Value);
			}

			return allCommands;
		}

		private bool CommandHelper(CCSPlayerController? player, CommandInfo info, CommandUsage usage, int argCount = 0, string? helpText = null, string? permission = null)
		{
			if (!IsCommandUsageValid(player, info, usage))
				return false;

			if (!HasPermission(player, info, permission))
				return false;

			if (IsArgumentCountInvalid(player, info, argCount, helpText))
				return false;

			return true;
		}

		private bool IsCommandUsageValid(CCSPlayerController? player, CommandInfo info, CommandUsage usage)
		{
			switch (usage)
			{
				case CommandUsage.CLIENT_ONLY when player == null || !player.IsValid:
					info.ReplyToCommand($" {_localizer.ForPlayer(player, "k4.general.prefix")} {_localizer.ForPlayer(player, "k4.command.client-only")}");
					return false;
				case CommandUsage.SERVER_ONLY when player != null:
					info.ReplyToCommand($" {_localizer.ForPlayer(player, "k4.general.prefix")} {_localizer.ForPlayer(player, "k4.command.server-only")}");
					return false;
				default:
					return true;
			}
		}

		private bool HasPermission(CCSPlayerController? controller, CommandInfo info, string? permission)
		{
			if (string.IsNullOrEmpty(permission))
				return true;

			if (!AdminManager.PlayerHasPermissions(controller, permission) &&
				!AdminManager.PlayerHasPermissions(controller, "@zenith/root") &&
				!AdminManager.PlayerHasPermissions(controller, "@css/root") &&
				(!AdminManager.PlayerHasCommandOverride(controller, info.GetArg(0)) || AdminManager.GetPlayerCommandOverrideState(controller, info.GetArg(0)) == false))
			{
				info.ReplyToCommand($" {_localizer.ForPlayer(controller, "k4.general.prefix")} {_localizer.ForPlayer(controller, "k4.command.no-permission")}");
				return false;
			}

			return true;
		}

		private bool IsArgumentCountInvalid(CCSPlayerController? controller, CommandInfo info, int argCount, string? helpText)
		{
			if (argCount > 0 && info.ArgCount < argCount + 1 && helpText != null)
			{
				info.ReplyToCommand($" {_localizer.ForPlayer(controller, "k4.general.prefix")} {_localizer.ForPlayer(controller, "k4.command.help", info.ArgByIndex(0), helpText)}");
				return true;
			}

			return false;
		}
	}
}
