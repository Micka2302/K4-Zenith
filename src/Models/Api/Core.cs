using System.Reflection;

public static class CallerIdentifier
{
	private static readonly string CurrentPluginName = Assembly.GetExecutingAssembly().GetName().Name!;
	private static readonly string[] BlockAssemblies = ["System.", "K4-ZenithAPI", "KitsuneMenu"];
	public static readonly List<string> ModuleList = [];

	public static string GetCallingPluginName()
	{
		var stackTrace = new System.Diagnostics.StackTrace(true);

		for (int i = 1; i < stackTrace.FrameCount; i++)
		{
			var assembly = stackTrace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
			var assemblyName = assembly?.GetName().Name;

			if (assemblyName == "CounterStrikeSharp.API")
				break;

			if (assemblyName != CurrentPluginName && assemblyName != null && !BlockAssemblies.Any(assemblyName.StartsWith))
			{
				if (!ModuleList.Contains(assemblyName))
					ModuleList.Add(assemblyName);

				return assemblyName;
			}
		}

		return CurrentPluginName;
	}
}
