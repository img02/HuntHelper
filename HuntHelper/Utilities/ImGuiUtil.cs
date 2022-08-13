using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;

namespace HuntHelper.Utilities;

public class ImGuiUtil
{
    public static void ImGui_CentreText(string text, Vector4 colour, float offset = 1f)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f * offset);
        ImGui.TextColored(colour, text);
    }
    public static void ImGui_CentreText(string text, float offset = 1f)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f * offset);
        ImGui.TextUnformatted(text);
    }

    public static void ImGui_RightAlignText(string text)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * .95f);
        ImGui.TextUnformatted(text);
    }

    public static void ImGui_HelpMarker(string text)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }

    public static void DoStuffWithMonoFont(Action function)
    {
        ImGui.PushFont(UiBuilder.MonoFont);
        function();
        ImGui.PopFont();
    }

    public static void ImGui_HoveredToolTip(string msg)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(msg);
            ImGui.EndTooltip();
        }
    }
}