using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;

namespace HuntHelper.Utilities;

public class ImGuiUtil
{
    public static void ImGui_CentreText(string text, Vector4 colour, float offset = 1f)
    {
        ImGui_CenterCursor(text, offset);
        ImGui.TextColored(colour, text);
    }
    public static void ImGui_CentreText(string text, float offset = 1f)
    {
        ImGui_CenterCursor(text, offset);
        ImGui.TextUnformatted(text);
    }

    public static void ImGui_RightAlignText(string text)
    {
        ImGui_CenterCursor(text, 1.9f);
        ImGui.TextUnformatted(text);
    }

    private static void ImGui_CenterCursor(string text, float offset)
    {
        var windowWidth = ImGui.GetWindowSize().X;
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f * offset);
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
    public static void ImGui_Separator(float offsetY)
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
        ImGui.Separator();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
    }
}