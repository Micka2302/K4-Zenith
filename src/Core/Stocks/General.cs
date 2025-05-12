using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;

namespace Zenith
{
	public sealed partial class Plugin : BasePlugin
	{
		public void DisposeModule(string callingPlugin)
		{
			Logger.LogInformation($"Disposing module '{callingPlugin}' and freeing resources.");

			RemoveModuleCommands(callingPlugin);
			RemoveModulePlaceholders(callingPlugin);
		}
	}
}