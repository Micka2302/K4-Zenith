using System.Text.Json.Serialization;

namespace Zenith_Ranks;

public class Rank
{
	public int Id { get; set; }

	[JsonPropertyName("Name")]
	public required string Name { get; set; }

	[JsonPropertyName("Point")]
	public int Point { get; set; }

	[JsonPropertyName("ChatColor")]
	public string ChatColor { get; set; } = "default";

	[JsonPropertyName("HexColor")]
	public string HexColor { get; set; } = "#FFFFFF";

	[JsonPropertyName("Permissions")]
	public List<Permission>? Permissions { get; set; }
}

public class Permission
{
	[JsonPropertyName("DisplayName")]
	public required string DisplayName { get; set; }

	[JsonPropertyName("PermissionName")]
	public required string PermissionName { get; set; }
}