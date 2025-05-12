using CounterStrikeSharp.API.Core;

namespace ZenithAPI
{
	public class SettingChangedEventArgs(CCSPlayerController controller, string key, object? oldValue, object? newValue) : EventArgs
	{
		public CCSPlayerController Controller { get; } = controller;
		public string Key { get; } = key;
		public object? OldValue { get; } = oldValue;
		public object? NewValue { get; } = newValue;
	}
}