using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;

namespace HuntHelper
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        #region Main Map Window
        
        //initial window position
        public Vector2 MapWindowPos { get; set; } = new Vector2(25, 25);
        //window sizes
        public int CurrentWindowSize { get; set; } = 512;
        public int PresetOneWindowSize { get; set; } = 512;
        public int PresetTwoWindowSize { get; set; } = 1024;
        public float MapImageOpacityAsPercentage { get; set; } = 100f;

        // icon colours
        public Vector4 SpawnPointColour { get; set; } = new Vector4(0.24706f, 0.309804f, 0.741176f, 1); //purty blue
        public Vector4 MobColour { get; set; } = new Vector4(0.4f, 1f, 0.567f, 1f); //green
        public Vector4 PlayerIconColour { get; set; } = new Vector4(0f, 0f, 0f, 1f); //black
        public Vector4 PlayerIconBackgroundColour { get; set; } = new Vector4(0.117647f, 0.5647f, 1f, 0.7f); //blue
        public Vector4 DirectionLineColour { get; set; } = new Vector4(1f, 0.3f, 0.3f, 1f); //redish
        public Vector4 DetectionCircleColour { get; set; } = new Vector4(1f, 1f, 0f, 1f); //goldish
        // icon radius sizes
        public float AllRadiusModifier { get; set; } = 1.0f;
        public float MobIconRadiusModifier { get; set; } = 2f;
        public float SpawnPointRadiusModifier { get; set; } = 1.0f;
        public float PlayerIconRadiusModifier { get; set; } = 1f;
        public float DetectionCircleModifier { get; set; } = 1.0f;
        // mouseover distance modifier for mob icon tooltips
        public float MouseOverDistanceModifier { get; set; } = 2.5f;
        // icon-related thickness
        public float DetectionCircleThickness { get; set; } = 3f;
        public float DirectionLineThickness { get; set; } = 3f;

        // on-screen text positions and colours
        public float ZoneInfoPosXPercentage { get; set; } = 3.5f;
        public float ZoneInfoPosYPercentage { get; set; } = 11.1f;
        public float WorldInfoPosXPercentage { get; set; } = 0.45f;
        public float WorldInfoPosYPercentage { get; set; } = 8.3f;
        public float PriorityMobInfoPosXPercentage { get; set; } = 35f;
        public float PriorityMobInfoPosYPercentage { get; set; } = 2.5f;
        public float NearbyMobListPosXPercentage { get; set; } = .42f;
        public float NearbyMobListPosYPercentage { get; set; } = 69f;
        // text colour
        public Vector4 ZoneTextColour { get; set; } = new Vector4(0.282353f, 0.76863f, 0.69412f, 1f); //blue-greenish
        public Vector4 ZoneTextColourAlt { get; set; } = new Vector4(0, 0.4353f, 0.36863f, 1f); //same but darker
        public Vector4 WorldTextColour { get; set; } = new Vector4(0.77255f, 0.3412f, 0.612f, 1f); //purply
        public Vector4 WorldTextColourAlt = new Vector4(0.51765f, 0, 0.3294f, 1f); //same but darker
        public Vector4 PriorityMobTextColour { get; set; } = Vector4.One;
        public Vector4 PriorityMobTextColourAlt { get; set; } = Vector4.One;
        public Vector4 NearbyMobListColour { get; set; } = Vector4.One;
        public Vector4 NearbyMobListColourAlt { get; set; } = Vector4.One;
        public Vector4 PriorityMobColourBackground { get; set; } = new Vector4(1f, 0.43529411764705883f, 0.5137254901960784f, 0f); //nicely pink :D
        public Vector4 NearbyMobListColourBackground { get; set; } = new Vector4(1f, 0.43529411764705883f, 0.5137254901960784f, 0f); //nicely pink :D

        public Vector4 MapWindowColour { get; set; } = new Vector4(0f, 0f, 0f, .2f); //alpha / w value isn't used
        public float MapWindowOpacityAsPercentage { get; set; } = 20f;

        //checkbox bools
        public bool PriorityMobEnabled { get; set; } = true;
        public bool NearbyMobListEnabled { get; set; } = true;
        public bool ShowZoneName { get; set; } = true;
        public bool ShowWorldName { get; set; } = true;
        public bool SaveSpawnData { get; set; } = true;
        public bool UseMapImages { get; set; } = false;
        //tts
        public string TTSVoiceName { get; set; } = string.Empty;
        public string TTSAMessage { get; set; } = "<rank> Nearby";
        public string TTSBMessage { get; set; } = "<rank> Nearby";
        public string TTSSMessage { get; set; } = "<rank> in zone";
        public string ChatAMessage { get; set; } = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        public string ChatBMessage { get; set; } = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        public string ChatSMessage { get; set; } = "FOUND: <name> @ <flag> ---  <rank>  --  <hpp>";
        public bool TTSAEnabled { get; set; } = false;
        public bool TTSBEnabled { get; set; } = false;
        public bool TTSSEnabled { get; set; } = true;
        public bool ChatAEnabled { get; set; } = false;
        public bool ChatBEnabled { get; set; } = false;
        public bool ChatSEnabled { get; set; } = true;
        public bool EnableBackgroundScan { get; set; } = false;
        public bool ShowOptionsWindow { get; set; } = false;
        public bool FlyTextAEnabled { get; set; } = false;
        public bool FlyTextBEnabled { get; set; } = false;
        public bool FlyTextSEnabled { get; set; } = true;

        #endregion

        #region Hunt Train Window
        //public bool HuntTrainWindowVisible { get; set; } = false;
        public Vector2 HuntTrainWindowSize { get; set; } = new Vector2(250, 400);
        public Vector2 HuntTrainWindowPos { get; set; } = new Vector2(150, 150);
        public bool HuntTrainShowPos { get; set; } = false;
        public bool HuntTrainShowLastSeen { get; set; } = false;
        public bool HuntTrainUseBorder { get; set; } = false;

        #endregion


        #region Counter Window

        public Vector2 CounterWindowPos { get; set; } = new Vector2(50, 50);
        public Vector2 CounterWindowSize { get; set; } = new Vector2(250, 50);

        public bool CountInBackground { get; set; } = false;

        #endregion


        //Hunt Window Flag - used for toggling title bar
        public int HuntWindowFlag { get; set; } = (int)(ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar);


        [NonSerialized]
        private DalamudPluginInterface? _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this._pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this._pluginInterface!.SavePluginConfig(this);
        }
    }
}
