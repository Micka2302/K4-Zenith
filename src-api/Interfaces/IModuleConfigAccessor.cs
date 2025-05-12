namespace ZenithAPI
{
	public interface IModuleConfigAccessor
	{
		/// <summary>
		///  Retrieves a configuration value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="groupName"></param>
		/// <param name="configName"></param>
		/// <returns></returns>
		T GetValue<T>(string groupName, string configName) where T : notnull;

		/// <summary>
		/// Sets a configuration value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="groupName"></param>
		/// <param name="configName"></param>
		/// <param name="value"></param>
		void SetValue<T>(string groupName, string configName, T value) where T : notnull;

		/// <summary>
		/// Checks if a configuration value exists.
		/// </summary>
		/// <param name="groupName"></param>
		/// <param name="configName"></param>
		bool HasValue(string groupName, string configName);
	}
}