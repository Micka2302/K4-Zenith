using System.Reflection;

public static class CallerIdentifier
{
	private static readonly string CurrentPluginName = Assembly.GetExecutingAssembly().GetName().Name!;
	private static readonly string[] BlockAssemblies = ["System.", "K4-ZenithAPI", "KitsuneMenu"];
	public static readonly List<string> ModuleList = [];
	private static readonly ThreadLocal<string> _cachedCallerName = new ThreadLocal<string>(() => CurrentPluginName);
	private static readonly HashSet<string> _moduleSet = [];

	public static string GetCallingPluginName()
	{
		if (_cachedCallerName.IsValueCreated)
		{
			return _cachedCallerName.Value ?? CurrentPluginName;
		}

		var stackTrace = new System.Diagnostics.StackTrace(true);

		for (int i = 1; i < stackTrace.FrameCount; i++)
		{
			var assembly = stackTrace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
			var assemblyName = assembly?.GetName().Name;

			if (assemblyName == "CounterStrikeSharp.API")
				break;

			if (assemblyName != CurrentPluginName && assemblyName != null && !BlockAssemblies.Any(assemblyName.StartsWith))
			{
				lock (_moduleSet)
				{
					if (_moduleSet.Add(assemblyName))
					{
						ModuleList.Add(assemblyName);
					}
				}
				_cachedCallerName.Value = assemblyName;
				return assemblyName;
			}
		}
		_cachedCallerName.Value = CurrentPluginName;
		return CurrentPluginName;
	}
}
