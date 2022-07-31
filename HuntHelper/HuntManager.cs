using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using HuntHelper.HuntInfo;
using ImGuiNET;
using Newtonsoft.Json;

namespace HuntHelper;

public class HuntManager
{
    private Dictionary<HuntRank, List<Mob>> ARRDict;
    private Dictionary<HuntRank, List<Mob>> HWDict;
    private Dictionary<HuntRank, List<Mob>> ShBDict;
    private Dictionary<HuntRank, List<Mob>> SBDict;
    private Dictionary<HuntRank, List<Mob>> EWDict;
    private DalamudPluginInterface pluginInterface;

    public bool ErrorPopUpVisible = false;
    public string ErrorMessage = string.Empty;

    //-contains current status, MobA1,MobB2,MobS,MobSS, MobA2,MobB2 - maybe player,playerpos,map.etc.

    public HuntManager(DalamudPluginInterface pluginInterface)
    {
        ARRDict = new Dictionary<HuntRank, List<Mob>>();
        HWDict = new Dictionary<HuntRank, List<Mob>>();
        ShBDict = new Dictionary<HuntRank, List<Mob>>();
        SBDict = new Dictionary<HuntRank, List<Mob>>();
        EWDict = new Dictionary<HuntRank, List<Mob>>();

        this.pluginInterface = pluginInterface;

        LoadHuntData();

    }


    public void GetPriorityMob()
    {

    }


    public void LoadHuntData()
    {
        var ARRJsonFiles = new List<string>
        {
            "./data/ARR-A.json",
            "./data/ARR-B.json",
            "./data/ARR-S.json",
        };

        var HWJsonFiles = new List<string>
        {
            "./data/HW-A.json",
            "./data/HW-B.json",
            "./data/HW-S.json",
        };
        var SBJsonFiles = new List<string>
        {
            "./data/SB-A.json",
            "./data/SB-B.json",
            "./data/SB-S.json",

        };
        var ShBJsonFiles = new List<string>
        {

            "./data/ShB-A.json",
            "./data/ShB-B.json",
            "./data/ShB-S.json",

        };
        var EWJsonFiles = new List<string>
        {
            "./data/EW-A.json",
            "./data/EW-B.json",
            "./data/EW-S.json"
        };

        //messy... prob cleaner way
        LoadFilesIntoDic(ARRDict, ARRJsonFiles);
        LoadFilesIntoDic(HWDict, HWJsonFiles);
        LoadFilesIntoDic(SBDict, SBJsonFiles);
        LoadFilesIntoDic(ShBDict, ShBJsonFiles);
        LoadFilesIntoDic(EWDict, EWJsonFiles);

    }

    public void SaveHuntData()
    {

    }

    //override tostring?
    public string GetDatabaseAsString()
    {
        var text = String.Format("{0,-4} | {1,-26} | {2,-5} | {3,5}\n" +
                   "--------------------------------------------------\n", "Rank","Name"," ID","Enabled");
        text += DictToString(ARRDict);
        text += DictToString(HWDict);
        text += DictToString(SBDict);
        text += DictToString(ShBDict);
        text += DictToString(EWDict);
        return text;
    }
    public bool IsHunt(uint modelID)
    {
        var exists = false;
        exists = ARRDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = HWDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = SBDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = ShBDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        if (!exists) exists = EWDict.Any(kvp => kvp.Value.Any(m => m.ModelID == modelID));
        return exists;
    }


    private string DictToString(Dictionary<HuntRank, List<Mob>> dic)
    {
        var text = string.Empty;

        foreach (var kvp in dic)
        {
            foreach (var mob in kvp.Value)
            {
                text += String.Format($"{mob.Rank,-4} | {mob.Name,-26} | {mob.ModelID,5} | {mob.IsEnabled,5}\n");
            }

            text += "\n";
        }

        return text += "\n--------------------------------------------------\n";
    }

    private void LoadFilesIntoDic(Dictionary<HuntRank, List<Mob>> dict, List<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            if (!File.Exists(Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, path)))
            {
                ErrorPopUpVisible = true;
                ErrorMessage += $"File {path} missing... Please replace missing file(s).\n";
                return;
            }
        }
        var A = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[0])));
        var B = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[1])));
        var S = JsonConvert.DeserializeObject<List<Mob>>(File.ReadAllText(Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, filePaths[2])));

        if (A != null) dict.Add(HuntRank.A, A);
        if (B != null) dict.Add(HuntRank.B, B);
        if (S != null) dict.Add(HuntRank.S, S);
    }

   







}