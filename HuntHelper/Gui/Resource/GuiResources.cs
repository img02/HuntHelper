using Dalamud.Logging;
using Dalamud;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;

namespace HuntHelper.Gui.Resource;

public class GuiResources
{
    public static readonly Dictionary<string, string> PluginText = new();
    public static readonly Dictionary<string, string> MapGuiText = new();
    public static readonly Dictionary<string, string> HuntTrainGuiText = new();
    public static readonly Dictionary<string, string> SpawnPointerFinderGuiText = new();
    public static readonly Dictionary<string, string> CounterGuiText = new();






    public static bool LoadGuiText(ClientLanguage lang)
    {
        if (Plugin.PluginDir == string.Empty) return false;

        var dict = new Dictionary<string, Dictionary<string, string>>();
        var path = Path.Combine(Plugin.PluginDir,
            lang == ClientLanguage.English ? @"Data\Localisation\en.json" :
            lang == ClientLanguage.French ? @"Data\Localisation\fr.json" :
            lang == ClientLanguage.German ? @"Data\Localisation\de.json" :
            @"Data\Localisation\ja.json");
        try
        {
            var text = File.ReadAllText(path);
            var d = JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string, string>>>(text);
            if (d == null)
            {
                PluginLog.Error("failed to deserialise: " + lang);
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
            return true;
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);
        }
        return false;
    }

    private static void AddToDictionary(Dictionary<string, string> dict, Dictionary<string, string> toAdd)
    {
        foreach (var kvp in toAdd)
        {
            dict.TryAdd(kvp.Key, kvp.Value);
        }
    }
}