using System;
using System.Numerics;
using HuntHelper.Managers.Hunts;
using ImGuiNET;

namespace HuntHelper;

public class HuntTrainUI : IDisposable
{
    private readonly HuntManager _huntManager;
    private readonly Configuration _config;


    private Vector2 _huntTrainWindowSize = new Vector2(250, 400);
    private Vector2 _huntTrainWindowPos = new Vector2(150, 150);

    private bool _huntTrainWindowVisible = false;
    public bool HuntTrainWindowVisible
    {
        get => _huntTrainWindowVisible;
        set => _huntTrainWindowVisible = value;
    }


    public HuntTrainUI(HuntManager huntManager, Configuration config)
    {
        _huntManager = huntManager;
        _config = config;
        LoadSettings();
    }

    public void Draw()
    {
        DrawHuntTrainWindow();
    }

    public void Dispose()
    {
        SaveSettings();
    }

    public void LoadSettings()
    {
        //HuntTrainWindowVisible = _config.HuntTrainWindowVisible;
        _huntTrainWindowSize = _config.HuntTrainWindowSize;
        _huntTrainWindowPos = _config.HuntTrainWindowPos;
    }

    public void SaveSettings()
    {
        //_config.HuntTrainWindowVisible = HuntTrainWindowVisible;
        _config.HuntTrainWindowSize = _huntTrainWindowSize;
        _config.HuntTrainWindowPos = _huntTrainWindowPos;
    }
    public void DrawHuntTrainWindow()
    {
        if (!HuntTrainWindowVisible) return;

        ImGui.SetNextWindowSize(_huntTrainWindowSize);
        ImGui.SetWindowPos(_huntTrainWindowPos);
        if (ImGui.Begin("Hunt Train##Window", ref _huntTrainWindowVisible))
        {
            ImGui.Text("IMMA WINDOW");
        }
    }

}