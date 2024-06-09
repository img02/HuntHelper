using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HuntHelper.Gui.Resource;
using HuntHelper.Utilities;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HuntHelper.Managers.NewExpansion
{
    //todo disable this after hunt maps available
    internal class SubmitDataPrompt : IDisposable
    {

        private Configuration _config;
        private bool _visible = false;
        public SubmitDataPrompt(Configuration config)
        {
            _config = config;
            if (!_config.DawntrailAlreadyPrompted)  _visible = true;
        }

        public void Draw()
        {
            if (!_visible) return;

            ImGui.SetNextWindowSize(new Vector2(630* ImGuiHelpers.GlobalScale, 220* ImGuiHelpers.GlobalScale));
            if (ImGui.Begin("new expansion, who dis?"))
            {
                ImGuiUtil.DoStuffWithMonoFont(() =>
                {
                    ImGui.TextWrapped("Opt in to submit found spawn points to help make dawntrail hunt map images?\n(this is from hunthelper btw)");
               
                ImGui.NewLine();

                if (ImGui.Button("yes, never show me this again", new Vector2(333 * ImGuiHelpers.GlobalScale, 25* ImGuiHelpers.GlobalScale)))
                {
                    _config.DawntrailSubmitPositionsData = true;
                    _config.DawntrailAlreadyPrompted = true;
                    _visible = false;
                }
                if (ImGui.Button("no :(, never show me this again", new Vector2(333 * ImGuiHelpers.GlobalScale, 25* ImGuiHelpers.GlobalScale)))
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

        public void Dispose()
        {

        }
    }
}
