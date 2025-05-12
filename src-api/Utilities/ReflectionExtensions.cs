using System.Reflection;

namespace ZenithAPI
{
	public static class ReflectionExtensions
	{
		public static bool IsObsolete(this FieldInfo field)
		{
			return field.GetCustomAttribute<ObsoleteAttribute>() != null;
		}
	}
}