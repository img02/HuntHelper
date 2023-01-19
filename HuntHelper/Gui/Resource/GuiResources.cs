using Dalamud.Logging;
using Dalamud;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace HuntHelper.Gui.Resource;

public class GuiResources
{
    public static readonly Dictionary<string, string> PluginText = new();
    public static readonly Dictionary<string, string> MapGuiText = new();
    public static readonly Dictionary<string, string> HuntTrainGuiText = new();
    public static readonly Dictionary<string, string> SpawnPointerFinderGuiText = new();
    public static readonly Dictionary<string, string> CounterGuiText = new();
    public static string Language = string.Empty;
    
    
    public static bool LoadGuiText(ClientLanguage lang)
    {
        if (Plugin.PluginDir == string.Empty) return false;

        var dict = new Dictionary<string, Dictionary<string, string>>();
        var language =
            lang == ClientLanguage.English ? "english" :
            lang == ClientLanguage.French ? "french" :
            lang == ClientLanguage.German ? "german" :
            "japanese";

        return LoadGuiText(language);
    }

    private static void AddToDictionary(Dictionary<string, string> dict, Dictionary<string, string> toAdd)
    {
        foreach (var kvp in toAdd)
        {
            if (!dict.TryAdd(kvp.Key, kvp.Value))
            {
                dict[kvp.Key] = kvp.Value;
            }
#if DEBUG
            PluginLog.Debug(dict[kvp.Key]);
#endif
        }
    }
    
    public static string[] GetAvailableLanguages()
    {
        var localisationFolder = Path.Combine(Plugin.PluginDir, @"Data\Localisation\");
        var paths = Directory.GetFileSystemEntries(localisationFolder, "*.json");
        var files = paths.Select(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant()).ToArray();
        return files;
    }
    
    public static bool LoadGuiText(string language)
    {
        var path = Path.Combine(Plugin.PluginDir, @"Data\Localisation\", $"{language}.json");
        try
        {
            var text = File.ReadAllText(path);
            var d = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(text);
            if (d == null)
            {
                PluginLog.Error("failed to deserialise: " + $"{language}.json");
                return false;
            }
            foreach (var kvp in d)
            {
                switch (kvp.Key)
                {
                    case "Plugin":
                        AddToDictionary(PluginText, kvp.Value);
                        break;
                    case "Map":
                        AddToDictionary(MapGuiText, kvp.Value);
                        break;
                    case "HuntTrain":
                        AddToDictionary(HuntTrainGuiText, kvp.Value);
                        break;
                    case "SpawnPointFinder":
                        AddToDictionary(SpawnPointerFinderGuiText, kvp.Value);
                        break;
                    case "Counter":
                        AddToDictionary(CounterGuiText, kvp.Value);
                        break;
                }
            }
            PluginLog.Debug($"we gucci");
            PluginLog.Log($"Loaded language file: {path}");
            Language = language;
            return true;
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);
        }
        return false;
    }
}