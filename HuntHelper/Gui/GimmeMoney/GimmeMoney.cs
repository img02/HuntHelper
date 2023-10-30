using Dalamud.Interface.Utility;
using HuntHelper.Utilities;
using ImGuiNET;
using System.Diagnostics;

namespace HuntHelper.Gui.GimmeMoney
{
    internal class GimmeMoney
    {
        /// <summary>
        /// begging for money, got no shame
        /// </summary>
        /// <param name="_bottomPanelHeight"></param>
        public static void drawDonoTab(ref float _bottomPanelHeight)
        {
            //took this from simpletweaks ;d
            ImGui.PushStyleColor(ImGuiCol.Tab, 0xFF000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.TabActive, 0xFF000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.TabHovered, 0xAA000000 | 0x005E5BFF);

            if (ImGui.BeginTabItem("ko-fi"))
            {
                _bottomPanelHeight = 120f * ImGuiHelpers.GlobalScale;
                ImGui.TextWrapped("If you've found this plugin useful and want to show some support.");
                var url = Constants.kofiUrl;
                ImGui.PushItemWidth(150);
                ImGui.InputText("", ref url, 40, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();
                if (ImGui.IsItemClicked())
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }

                ImGui.EndTabItem();
            }
            ImGui.PopStyleColor(3);
        }

        /// <summary>
        /// begging for money, got no shame
        /// </summary>
        /// <param name="_bottomPanelHeight"></param>
        public static void drawDonoButton()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

            if (ImGui.Button("ko-fi"))
            {
                var url = Constants.kofiUrl;
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            ImGuiUtil.ImGui_HoveredToolTip("If you've found this plugin useful and want to show some support.");

            ImGui.PopStyleColor(2);
        }
    }
}
