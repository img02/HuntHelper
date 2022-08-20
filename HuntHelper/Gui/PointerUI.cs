using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using HuntHelper.Managers.Hunts;
using ImGuiNET;

namespace HuntHelper.Gui;

public class PointerUI
{
    private readonly HuntManager _huntManager;
    private readonly Configuration _config;
    private readonly GameGui _gameGui;
    private const float DiamondBaseWidth = 30f; //not customisable
    public PointerUI(HuntManager huntManager, Configuration config, GameGui gameGui)
    {
        _huntManager = huntManager;
        _config = config;
        _gameGui = gameGui;
    }

    public void Draw()
    {
        if (!_config.EnableBackgroundScan && !_config.MapWindowVisible) return;

        var currentMobs = _huntManager.GetAllCurrentMobsWithRank();
        if (currentMobs.Count == 0) return;
        try
        {
            currentMobs.ForEach((item) =>
                PointToMobs(item.Rank, item.Mob));
        }
        catch (InvalidOperationException e)
        {
        }
    }

    private void PointToMobs(HuntRank rank, BattleNpc mob)
    {
        var floatingPointingIconThingyColour = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0.9f, 1, 1)); // blueish
        switch (rank)
        {
            case HuntRank.A:
                if (!_config.PointToARank) return;
                floatingPointingIconThingyColour = ImGui.ColorConvertFloat4ToU32(new Vector4(.9f, .24f, .24f, 1)); //redish-pink
                break;
            case HuntRank.B:
                if (!_config.PointToBRank) return;
                break;
            case HuntRank.S:
            case HuntRank.SS:
                if (!_config.PointToSRank) return;
                floatingPointingIconThingyColour = ImGui.ColorConvertFloat4ToU32(new Vector4(1, .93f, .12f, 1)); //yellowish-gold
                break;
        }

        _gameGui.WorldToScreen(mob.Position, out var pointofFocusPosition);
        var windowOffsetY = -100;
        //actual position
        //works but a bit buggy, if using when camera not facing, worldtoscreen sets pos to opposite-ish direction once camera turned far enough.
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(DiamondBaseWidth) * _config.PointerDiamondSizeModifier);
        ImGui.SetNextWindowPos(new Vector2(pointofFocusPosition.X, pointofFocusPosition.Y + windowOffsetY));
        if (ImGui.Begin($"POINTER##{mob.NameId}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground ))
        {
            DrawDiamond(floatingPointingIconThingyColour);
            ImGui.End();
        }
        ImGui.PopStyleVar(); // window padding
        //pointer when off screen
        var screenSize = ImGuiHelpers.MainViewport.Size;
        if (!(pointofFocusPosition.X < 0) && !(pointofFocusPosition.X + DiamondBaseWidth * _config.PointerDiamondSizeModifier > screenSize.X) &&
            !(pointofFocusPosition.Y + windowOffsetY < 0) && !(pointofFocusPosition.Y + DiamondBaseWidth * _config.PointerDiamondSizeModifier + windowOffsetY > screenSize.Y)) return;

        var helperArrowPosition = Vector2.Zero;
        var helperArrowSize = new Vector2(DiamondBaseWidth);

        var xMin = 0f;
        var xMax = screenSize.X - DiamondBaseWidth * _config.PointerDiamondSizeModifier;
        var yMin = 0f - windowOffsetY;
        var yMax = screenSize.Y - DiamondBaseWidth * _config.PointerDiamondSizeModifier - windowOffsetY;

        var xPos = pointofFocusPosition.X;
        var yPos = pointofFocusPosition.Y;

        if (pointofFocusPosition.X < 0) xPos = xMin;
        if (pointofFocusPosition.Y + windowOffsetY < 0) yPos = yMin;

        if (pointofFocusPosition.X + DiamondBaseWidth * _config.PointerDiamondSizeModifier > screenSize.X) xPos = xMax;
        if (pointofFocusPosition.Y + windowOffsetY + DiamondBaseWidth * _config.PointerDiamondSizeModifier > screenSize.Y) yPos = yMax;

        helperArrowPosition.X = xPos;
        helperArrowPosition.Y = yPos;

        //pointer arrow
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowSize(helperArrowSize * _config.PointerDiamondSizeModifier);
        ImGui.SetNextWindowPos(new Vector2(helperArrowPosition.X, (helperArrowPosition.Y + windowOffsetY + 15) - helperArrowSize.Y / 2)); //not sure why 15 is needed to align
        if (ImGui.Begin($"DIRECTIONTOPOINTER##{mob.NameId}", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground))
        {
            DrawDiamond(floatingPointingIconThingyColour);
            ImGui.End();
        }
        ImGui.PopStyleVar();

    }

    private void DrawDiamond(uint colour)
    {
        var black = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1));
        var (diamond, innerDiamond) = GetConvexPolyDiamondVectors();
        var dl = ImGui.GetWindowDrawList();
        dl.AddConvexPolyFilled(ref diamond[0], 4, colour);
        dl.AddConvexPolyFilled(ref innerDiamond[0], 4, black);
    }

    private (Vector2[] diamond, Vector2[] innerDiamond) GetConvexPolyDiamondVectors()
    {//base size is 30x30
        var innerOffset = 5f;
        var pos = new Vector2(ImGui.GetWindowPos().X, ImGui.GetWindowPos().Y + DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier);
        var diamond = new Vector2[]
        {
                pos,
                new Vector2(pos.X + DiamondBaseWidth / 2*_config.PointerDiamondSizeModifier, pos.Y + DiamondBaseWidth / 2 *_config.PointerDiamondSizeModifier),
                new Vector2(pos.X + DiamondBaseWidth * _config.PointerDiamondSizeModifier, pos.Y),
                new Vector2(pos.X + DiamondBaseWidth / 2*_config.PointerDiamondSizeModifier, pos.Y - DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier)
        };
        var innerDiamond = new Vector2[]
        {
                new Vector2(pos.X + innerOffset, pos.Y),
                new Vector2(pos.X + DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier, pos.Y + DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier - innerOffset),
                new Vector2(pos.X + DiamondBaseWidth * _config.PointerDiamondSizeModifier-innerOffset, pos.Y),
                new Vector2(pos.X + DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier, pos.Y - DiamondBaseWidth / 2 * _config.PointerDiamondSizeModifier + innerOffset)
        };

        return (diamond, innerDiamond);
    }
}