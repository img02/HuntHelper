using Dalamud.Interface.Utility;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Numerics;

namespace HuntHelper.Managers.NewExpansion
{
    //todo make new config bool next expansion
    internal class SubmitDataPrompt 
    {

        private Configuration _config;
        private bool _visible = false;
        public SubmitDataPrompt(Configuration config)
        {
            _config = config;
            if (!_config.DawntrailAlreadyPrompted) _visible = true;
        }

        public void Draw()
        {
            if (!Constants.NEW_EXPANSION) return;
            if (!_visible) return;

            ImGui.SetNextWindowSize(new Vector2(630 * ImGuiHelpers.GlobalScale, 220 * ImGuiHelpers.GlobalScale));
            if (ImGui.Begin("new expansion, who dis?"))
            {
                ImGuiUtil.DoStuffWithMonoFont(() =>
                {
                    ImGui.TextWrapped("submit found spawn points to help make **INSERT EXPANSION NAME HERE** hunt map images?\n(this is from hunthelper btw)");

                    ImGui.NewLine();

                    if (ImGui.Button("yes, never show me this again", new Vector2(333 * ImGuiHelpers.GlobalScale, 25 * ImGuiHelpers.GlobalScale)))
                    {
                        _config.DawntrailSubmitPositionsData = true;
                        _config.DawntrailAlreadyPrompted = true;
                        _visible = false;
                    }
                    if (ImGui.Button("no :(, never show me this again", new Vector2(333 * ImGuiHelpers.GlobalScale, 25 * ImGuiHelpers.GlobalScale)))
                    {
                        _config.DawntrailAlreadyPrompted = true;
                        _visible = false;
                    }

                    ImGui.NewLine();
                    ImGui.Text("You can opt in / out from the settings on the main map ui.");
                });
            }
            ImGui.End();
        }
    }
}
