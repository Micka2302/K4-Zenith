using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using ZenithAPI;

namespace Zenith_Ranks;

public sealed partial class Plugin : BasePlugin
{
    public List<Rank> Ranks = [];

    public void Initialize_Ranks()
    {
        string ranksFilePath = Path.Join(ModuleDirectory, "ranks.jsonc"); string defaultRanksContent = @"[
    {
        ""Name"": ""Silver I"",
        ""Image"": """", // Image URL for the rank. This can be used for web integrations such as GameCMS
        ""Point"": 0, // From this amount of experience, the player is Silver I, if its 0, this will be the default rank
        ""ChatColor"": ""grey"", // Color code for the rank. Find color names here: https://github.com/roflmuffin/CounterStrikeSharp/blob/main/managed/CounterStrikeSharp.API/Modules/Utils/ChatColors.cs
        ""HexColor"": ""#C0C0C0"", // Hexadecimal color code for the rank
        ""Permissions"": [ // You can add permissions to the rank. If you don't want to add any, remove this array
            {
                ""DisplayName"": ""Super Permission"", // This is the name of the permission. Will be displayed in the menu of ranks to let people know the benefits of a rank
                ""PermissionName"": ""@my-plugin/permission1"" // This is the permission name. You can assign 3rd party permissions here
            },
            {
                ""DisplayName"": ""Legendary Permission"",
                ""PermissionName"": ""@my-plugin/permission2""
            }
            // You can add as many as you want
        ]
    },
    {
        ""Name"": ""Silver II"",
        ""Image"": """",
        ""Point"": 1500,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver III"",
        ""Image"": """",
        ""Point"": 3000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver IV"",
        ""Image"": """",
        ""Point"": 4500,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver Elite"",
        ""Image"": """",
        ""Point"": 6000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Silver Elite Master"",
        ""Image"": """",
        ""Point"": 8000,
        ""ChatColor"": ""grey"",
        ""HexColor"": ""#C0C0C0""
    },
    {
        ""Name"": ""Gold Nova I"",
        ""Image"": """",
        ""Point"": 10000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova II"",
        ""Image"": """",
        ""Point"": 13000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova III"",
        ""Image"": """",
        ""Point"": 17000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Gold Nova Master"",
        ""Image"": """",
        ""Point"": 22000,
        ""ChatColor"": ""gold"",
        ""HexColor"": ""#FFD700""
    },
    {
        ""Name"": ""Master Guardian I"",
        ""Image"": """",
        ""Point"": 28000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Master Guardian II"",
        ""Image"": """",
        ""Point"": 35000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Master Guardian Elite"",
        ""Image"": """",
        ""Point"": 43000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Distinguished Master Guardian"",
        ""Image"": """",
        ""Point"": 52000,
        ""ChatColor"": ""green"",
        ""HexColor"": ""#00FF00""
    },
    {
        ""Name"": ""Legendary Eagle"",
        ""Image"": """",
        ""Point"": 62000,
        ""ChatColor"": ""blue"",
        ""HexColor"": ""#0000FF""
    },
    {
        ""Name"": ""Legendary Eagle Master"",
        ""Image"": """",
        ""Point"": 70000,
        ""ChatColor"": ""blue"",
        ""HexColor"": ""#0000FF""
    },
    {
        ""Name"": ""Supreme Master First Class"",
        ""Image"": """",
        ""Point"": 75000,
        ""ChatColor"": ""purple"",
        ""HexColor"": ""#800080""
    },
    {
        ""Name"": ""Global Elite"",
        ""Image"": """",
        ""Point"": 80000,
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

    private static string RemoveComments(string content)
    {
        return CommentRegex().Replace(content, string.Empty);
    }

    [GeneratedRegex(@"/\*(.*?)\*/|//(.*)", RegexOptions.Multiline)]
    private static partial Regex CommentRegex();
}