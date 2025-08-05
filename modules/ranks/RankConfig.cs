
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;
using ZenithAPI;

namespace Zenith_Ranks;

public sealed partial class Plugin : BasePlugin
{
    public List<Rank> Ranks = [];

    public void Initialize_Ranks()
    {
        string ranksFilePath = Path.Join(ModuleDirectory, "ranks.jsonc");

        string defaultRanksContent = @"[
    {
        ""Name"": ""Silver I"",
        ""Image"": """", // Image URL for the rank. This can be used for web integrations such as GameCMS
        ""Point"": 0, // From this amount of experience, the player is Silver I, if its 0, this will be the default rank
        ""ChatColor"": ""grey"", // Color code for the rank. Find color names here: https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Modules/Utils/ChatColors.cs
        ""HexColor"": ""#C0C0C0"", // Hexadecimal color code for the rank
        ""Permissions"": [ // You can add permissions to the rank. If you don't want to add any, remove this array
            {
                ""DisplayName"": ""Super Permission"", // This is the name of the permission. Will be displayed in the menu of ranks to let people know the benefits of a rank
                ""PermissionName"": ""permission1"" // This is the permission name. You can assign 3rd party permissions here
            },
            {
                ""DisplayName"": ""Legendary Permission"",
                ""PermissionName"": ""permission2""
            }
            // You can add as many as you want
        ]
    },
    {
        ""Name"": ""Silver II"",
        ""Image"": """",
        ""Point"": 5000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver III"",
        ""Image"": """",
        ""Point"": 10000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver IV"",
        ""Image"": """",
        ""Point"": 15000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver Elite"",
        ""Image"": """",
        ""Point"": 20000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver Elite Master"",
        ""Image"": """",
        ""Point"": 25000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Gold Nova I"",
        ""Image"": """",
        ""Point"": 30000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova II"",
        ""Image"": """",
        ""Point"": 40000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova III"",
        ""Image"": """",
        ""Point"": 50000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova Master"",
        ""Image"": """",
        ""Point"": 60000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Master Guardian I"",
        ""Image"": """",
        ""Point"": 75000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Master Guardian II"",
        ""Image"": """",
        ""Point"": 90000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Master Guardian Elite"",
        ""Image"": """",
        ""Point"": 110000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Distinguished Master Guardian"",
        ""Image"": """",
        ""Point"": 130000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Legendary Eagle"",
        ""Image"": """",
        ""Point"": 160000,
        ""ChatColor"": ""blue"",
        ""HexColor"": ""#0000FF""
    },
    {
        ""Name"": ""Legendary Eagle Master"",
        ""Image"": """",
        ""Point"": 190000,
        ""ChatColor"": ""blue"",
        ""HexColor"": ""#0000FF""
    },
    {
        ""Name"": ""Supreme Master First Class"",
        ""Image"": """",
        ""Point"": 230000,
        ""ChatColor"": ""purple"",
        ""HexColor"": ""#800080""
    },
    {
        ""Name"": ""Global Elite"",
        ""Image"": """",
        ""Point"": 280000,
        ""ChatColor"": ""lightred"",
        ""HexColor"": ""#FF4040""
    }
]";

        try
        {
            if (!File.Exists(ranksFilePath))
            {
                File.WriteAllText(ranksFilePath, defaultRanksContent);
                Logger.LogInformation("Default ranks file created.");
            }

            string fileContent = File.ReadAllText(ranksFilePath);

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                ResetToDefaultRanksFile(ranksFilePath, defaultRanksContent);
                fileContent = File.ReadAllText(ranksFilePath);
            }

            string jsonContent = RemoveComments(fileContent);

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                ResetToDefaultRanksFile(ranksFilePath, defaultRanksContent);
                jsonContent = RemoveComments(File.ReadAllText(ranksFilePath));
            }

            Ranks = JsonConvert.DeserializeObject<List<Rank>>(jsonContent)!;
            if (Ranks == null || Ranks.Count == 0)
            {
                ResetToDefaultRanksFile(ranksFilePath, defaultRanksContent);
                Ranks = JsonConvert.DeserializeObject<List<Rank>>(RemoveComments(File.ReadAllText(ranksFilePath)))!;
            }

            for (int i = 0; i < Ranks.Count; i++)
            {
                Ranks[i].Id = i + 1;
            }

            foreach (Rank rank in Ranks)
            {
                rank.ChatColor = ChatColorUtility.ApplyPrefixColors(rank.ChatColor);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("An error occurred: " + ex.Message);
        }
    }

    private void ResetToDefaultRanksFile(string filePath, string defaultContent)
    {
        File.WriteAllText(filePath, defaultContent);
        Logger.LogWarning("Invalid content found. Default ranks file regenerated.");
    }

    private string RemoveComments(string content)
    {
        return Regex.Replace(content, @"/\*(.*?)\*/|//(.*)", string.Empty, RegexOptions.Multiline);
    }

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
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("PermissionName")]
        public string PermissionName { get; set; } = "";
    }
}