using Dalamud.Game;

namespace HuntHelper;

public static class Constants
{
    public static bool NEW_EXPANSION = true;
    public static ushort NEW_EXPANSION_MIN_MAP_ID = 961; //just set this as highest mapid for prev expansion - 1192 - Living Memory - Dawntrail

    public const string BaseImageUrl = "https://raw.githubusercontent.com/img02/HuntHelper-Resources/main/Images/Maps/";
#if DEBUG
    public const string RepoUrl = "https://github.com/img02/HuntHelper-Resources/tree/test";
#else
    public const string RepoUrl = "https://github.com/img02/HuntHelper-Resources/";
#endif

    public const string translateUrl = "https://github.com/img02/HuntHelper/tree/main/Translate";
    public const string translateUrlCrowdIn = "https://crowdin.com/project/hunthelper";
    public const string kofiUrl = "https://ko-fi.com/img123";

    #region English
    //en
    private static readonly string[] Minhocao_en = { "Earth Sprite" };
    private static readonly string[] Squonk_en = { "Chirp" };
    private static readonly string[] Leucrotta_en = { "Allagan Chimera", "Lesser Hydra", "Meracydian Vouivre" };
    private static readonly string[] Gandawera_en = { "Aurum Regis Ore", "Seventh Heaven" };
    private static readonly string[] Okina_en = { "Naked Yumemi", "Yumemi" }; //naked goes first here, otherwise minor issue w/ matching 'naked yumemi' to 'yumemi'
    private static readonly string[] Udumbara_en = { "Leshy", "Diakka" };
    private static readonly string[] SaltAndLight_en = { "Throw" };
    private static readonly string[] ForgivenPedantry_en = { "Dwarven Cotton Boll" };
    private static readonly string[] Ixtab_en = { "Cracked Ronkan Doll", "Cracked Ronkan Thorn", "Cracked Ronkan Vessel" };
    private static readonly string[] Sphatika_en = { "Asvattha", "Pisaca", "Vajralangula" };
    private static readonly string[] Ruinator_en = { "Thinker", "Wanderer", "Weeper" };

    private const string MinhocaoRegex_en = $"{BattleRegexBase_en}earth sprite.";
    private const string SquonkRegex_en = $"Squonk uses Chirp";
    private const string LeucrottaRegex_en = $"{BattleRegexBase_en}(Allagan chimera|lesser hydra|Meracydian vouivre).";
    private const string GandaweraRegex_en = $"{GatheringRegexBase_en}(aurum regis ore|seventh heaven)";
    private const string OkinaRegex_en = $"{BattleRegexBase_en}(Yumemi|Naked Yumemi).";
    private const string UdumbaraRegex_en = $"{BattleRegexBase_en}(Leshy|Diakka).";
    private const string SaltAndLightRegex_en = $"You throw away.*";
    private const string ForgivenPedantryRegex_en = $"{GatheringRegexBase_en}dwarven cotton (boll|bolls)";
    private const string IxtabRegex_en = $"{BattleRegexBase_en}Cracked (Ronkan Doll|Ronkan Thorn|Ronkan Vessel).";
    private const string SphatikaRegex_en = $"{BattleRegexBase_en}(Asvattha|Pisaca|Vajralangula).";
    private const string RuinatorRegex_en = $"{BattleRegexBase_en}(Thinker|Wanderer|Weeper).";
    private const string BattleRegexBase_en = "(?i)(defeat|defeats) the ";
    private const string GatheringRegexBase_en = "(?i)You obtain.*";
    #endregion

    #region Japanese
    //jp
    private static readonly string[] Minhocao_ja = { "アーススプライト" };
    private static readonly string[] Squonk_ja = { "チャープ" };
    private static readonly string[] Leucrotta_ja = { "アラガン・キマイラ", "レッサーハイドラ", "メラシディアン・ヴィーヴル" };
    private static readonly string[] Gandawera_ja = { "皇金鉱", "アストラルフラワー" };
    private static readonly string[] Okina_ja = { "カラナシ・ユメミ", "ユメミガイ" };
    private static readonly string[] Udumbara_ja = { "レーシー", "ディアッカ" };
    private static readonly string[] SaltAndLight_ja = { "捨てた。" };
    private static readonly string[] ForgivenPedantry_ja = { "ドワーフ綿花" };
    private static readonly string[] Ixtab_ja = { "クラックド・ロンカドール", "クラックド・ロンカソーン", "クラックド・ロンカヴェッセル" };
    private static readonly string[] Sphatika_ja = { "アシュヴァッタ", "ピシャーチャ", "ヴァジュララングラ" };
    private static readonly string[] Ruinator_ja = { "シンカー", "ワンダラー", "ウィーパー" };


    private const string MinhocaoRegex_ja = $"アーススプライト{BattleRegexBase_ja}";
    private const string SquonkRegex_ja = $"スクオンクの「チャープ」";
    private const string LeucrottaRegex_ja = $"(アラガン・キマイラ|レッサーハイドラ|メラシディアン・ヴィーヴル){BattleRegexBase_ja}";
    private const string GandaweraRegex_ja = $"(?i)(皇金鉱|アストラルフラワー){GatheringRegexBase_ja}";
    private const string OkinaRegex_ja = $"(カラナシ・ユメミ|ユメミガイ){BattleRegexBase_ja}";
    private const string UdumbaraRegex_ja = $"(レーシー|ディアッカ){BattleRegexBase_ja}";
    private const string SaltAndLightRegex_ja = $".*を捨てた。";
    private const string ForgivenPedantryRegex_ja = $"ドワーフ綿花{GatheringRegexBase_ja}";
    private const string IxtabRegex_ja = $"(クラックド・ロンカドール|クラックド・ロンカソーン|クラックド・ロンカヴェッセル){BattleRegexBase_ja}";
    private const string SphatikaRegex_ja = $"(アシュヴァッタ|ピシャーチャ|ヴァジュララングラ){BattleRegexBase_ja}";
    private const string RuinatorRegex_ja = $"(シンカー|ワンダラー|ウィーパー){BattleRegexBase_ja}";
    private const string BattleRegexBase_ja = "を倒した。"; //XXXは、シンカーを倒した。
    private const string GatheringRegexBase_ja = "を入手した。";
    #endregion


    #region German
    //de
    private static readonly string[] Minhocao_de = { "Erd-Exergon" };
    private static readonly string[] Squonk_de = { "Fiepsen" };
    private static readonly string[] Leucrotta_de = { "allagisch[a] Chimära", "klein[a] Hydra", "meracydisch[a] Vivel" };
    private static readonly string[] Gandawera_de = { "Königsgold-Erz", "Siebter Himmel-Blume" };
    private static readonly string[] Okina_de = { "Nackt-Yumemi", "Yumemi" };
    private static readonly string[] Udumbara_de = { "Leschij", "Diakka" };
    private static readonly string[] SaltAndLight_de = { "wirfst" };
    private static readonly string[] ForgivenPedantry_de = { "Zwergenwolle" };
    private static readonly string[] Ixtab_de = { "kaputt[a] Ronka-Totem", "kaputt[a] Ruinenquadroquader", "kaputt[a] Ruinenquader" };
    private static readonly string[] Sphatika_de = { "Asvattha", "Pisaca", "Vajralangula" };
    private static readonly string[] Ruinator_de = { "Denker", "Streuner", "Schluchzer" };

    private const string MinhocaoRegex_de = $"{BattleRegexBase_de}Erd-Exergon besiegt.";
    private const string SquonkRegex_de = $"Squonk setzt Fiepsen ein.";
    private const string LeucrottaRegex_de = $"{BattleRegexBase_de}(allagisch[a] Chimära|klein[a] Hydra|meracydisch[a] Vivel) besiegt.";
    private const string GandaweraRegex_de = $"(?i)(Königsgold-Erz|Siebter Himmel-Blume){GatheringRegexBase_de}";
    private const string OkinaRegex_de = $"{BattleRegexBase_de}(Yumemi|Nackt-Yumemi) besiegt.";
    private const string UdumbaraRegex_de = $"{BattleRegexBase_de}(Leschij|Diakka) besiegt.";
    private const string SaltAndLightRegex_de = $"Du wirfst.*"; //Du wirfst einen  Königsgold-Erzklumpen weg.
    private const string ForgivenPedantryRegex_de = $".*Zwergenwolle{GatheringRegexBase_de}";
    private const string IxtabRegex_de = $"{BattleRegexBase_de}(kaputt[a] Ronka-Totem|kaputt[a] Ruinenquadroquader|kaputt[a] Ruinenquader) besiegt.";
    private const string SphatikaRegex_de = $"{BattleRegexBase_de}(Asvattha|Pisaca|Vajralangula) besiegt.";
    private const string RuinatorRegex_de = $"{BattleRegexBase_de}(Denker|Streuner|Schluchzer) besiegt.";
    private const string BattleRegexBase_de = "(?i)(hast|hat) .*"; //Du hast den Denker besiegt. - den das ?
    private const string GatheringRegexBase_de = ".* erhalten.";
    #endregion

    #region French
    //fr
    private static readonly string[] Minhocao_fr = { "élémentaire de terre" };
    private static readonly string[] Squonk_fr = { "Gazouillement" };
    private static readonly string[] Leucrotta_fr = { "chimère allagoise", "hydre mineure", "vouivre méracydienne" };
    private static readonly string[] Gandawera_fr = { "Minerai d'aurum regis", "Fleur astrale | fleurs astrales" }; //sb
    private static readonly string[] Okina_fr = { "yumemi nu", "Yumemi" };
    private static readonly string[] Udumbara_fr = { "liéchi", "Diakka" };
    private static readonly string[] SaltAndLight_fr = { "jetez" }; //shb
    private static readonly string[] ForgivenPedantry_fr = { "Fleur de coton nain" };
    private static readonly string[] Ixtab_fr = { "poupée ronka fissurée", "épine ronka fissurée", "réceptacle ronka fissuré" };//lol//ew
    private static readonly string[] Sphatika_fr = { "Asvattha", "pishacha", "Vajralangula" };
    private static readonly string[] Ruinator_fr = { "penseur", "vagabond", "lamenteur" };

    private const string MinhocaoRegex_fr = $"{BattleRegexBase_fr}élémentaire de terre.";
    private const string SquonkRegex_fr = $"Squonk utilise Gazouillement.";
    private const string LeucrottaRegex_fr = $"{BattleRegexBase_fr}(chimère allagoise|hydre mineure|vouivre méracydienne).";
    private const string GandaweraRegex_fr = $"{GatheringRegexBase_fr}(Minerai d'aurum regis|Fleur astrale|fleurs astrales).";
    private const string OkinaRegex_fr = $"{BattleRegexBase_fr}(yumemi nu|Yumemi).";
    private const string UdumbaraRegex_fr = $"{BattleRegexBase_fr}(liéchi|Diakka).";
    private const string SaltAndLightRegex_fr = $"Vous jetez.*"; //Vous jetez un  morceau de minerai d'aurum regis
    private const string ForgivenPedantryRegex_fr = $"{GatheringRegexBase_fr}Fleur de coton nain";
    private const string IxtabRegex_fr = $"{BattleRegexBase_fr}(poupée ronka fissurée|épine ronka fissurée|réceptacle ronka fissuré).";
    private const string SphatikaRegex_fr = $"{BattleRegexBase_fr}(Asvattha|pishacha|Vajralangula).";
    private const string RuinatorRegex_fr = $"{BattleRegexBase_fr}(penseur|vagabond|lamenteur).";
    private const string BattleRegexBase_fr = "(?i)(a|avez) vaincu .*"; //Vous avez vaincu l'élémentaire de terre.
    private const string GatheringRegexBase_fr = "(?i)Vous obtenez.*";
    #endregion

    #region ChineseSimplified
    //zh
    private static readonly string[] Minhocao_chs = { "土元精" };
    private static readonly string[] Squonk_chs = { "唧唧咋咋" };
    private static readonly string[] Leucrotta_chs = { "亚拉戈奇美拉", "小海德拉", "美拉西迪亚薇薇尔飞龙" };
    private static readonly string[] Gandawera_chs = { "皇金矿", "星极花" };
    private static readonly string[] Okina_chs = { "无壳观梦螺", "观梦螺" };
    private static readonly string[] Udumbara_chs = { "莱西", "狄亚卡" };
    private static readonly string[] SaltAndLight_chs = { "舍弃" };
    private static readonly string[] ForgivenPedantry_chs = { "矮人棉" };
    private static readonly string[] Ixtab_chs = { "破裂的隆卡人偶", "破裂的隆卡石蒺藜", "破裂的隆卡器皿" };
    private static readonly string[] Sphatika_chs = { "阿输陀花", "毕舍遮", "金刚尾" };
    private static readonly string[] Ruinator_chs = { "思考之物", "彷徨之物", "叹息之物" };

    private const string MinhocaoRegex_chs = $"土元精{BattleRegexBase_chs}";
    private const string SquonkRegex_chs = $"斯奎克发动了“唧唧咋咋”";
    private const string LeucrottaRegex_chs = $"(亚拉戈奇美拉|小海德拉|美拉西迪亚薇薇尔飞龙){BattleRegexBase_chs}";
    private const string GandaweraRegex_chs = $"{GatheringRegexBase_chs}(皇金矿|星极花)";
    private const string OkinaRegex_chs = $"(无壳观梦螺|观梦螺){BattleRegexBase_chs}";
    private const string UdumbaraRegex_chs = $"(莱西|狄亚卡){BattleRegexBase_chs}";
    private const string SaltAndLightRegex_chs = $"舍弃了.*";
    private const string ForgivenPedantryRegex_chs = $"{GatheringRegexBase_chs}矮人棉";
    private const string IxtabRegex_chs = $"(破裂的隆卡人偶|破裂的隆卡石蒺藜|破裂的隆卡器皿){BattleRegexBase_chs}";
    private const string SphatikaRegex_chs = $"(阿输陀花|毕舍遮|金刚尾){BattleRegexBase_chs}";
    private const string RuinatorRegex_chs = $"(思考之物|彷徨之物|叹息之物){BattleRegexBase_chs}";
    private const string BattleRegexBase_chs = "打倒了";
    private const string GatheringRegexBase_chs = "获得了.*";
    #endregion

    //ss ids
    public const uint SS_Ker = 10615;
    public const uint SS_Forgiven_Rebellion = 8915;

    //minion ids
    public const uint WeeEa = 423;


    public static string[] Minhocao { get; private set; } = Minhocao_en;
    public static string[] Squonk { get; private set; } = Squonk_en;
    public static string[] Leucrotta { get; private set; } = Leucrotta_en;
    public static string[] Gandawera { get; private set; } = Gandawera_en;
    public static string[] Okina { get; private set; } = Okina_en;
    public static string[] Udumbara { get; private set; } = Udumbara_en;
    public static string[] SaltAndLight { get; private set; } = SaltAndLight_en;
    public static string[] ForgivenPedantry { get; private set; } = ForgivenPedantry_en;
    public static string[] Ixtab { get; private set; } = Ixtab_en;
    public static string[] Sphatika { get; private set; } = Sphatika_en;
    public static string[] Ruinator { get; private set; } = Ruinator_en;

    public static string MinhocaoRegex { get; private set; } = MinhocaoRegex_en;
    public static string SquonkRegex { get; private set; } = SquonkRegex_en;
    public static string LeucrottaRegex { get; private set; } = LeucrottaRegex_en;
    public static string GandaweraRegex { get; private set; } = GandaweraRegex_en;
    public static string OkinaRegex { get; private set; } = OkinaRegex_en;
    public static string UdumbaraRegex { get; private set; } = UdumbaraRegex_en;
    public static string SaltAndLightRegex { get; private set; } = SaltAndLightRegex_en;
    public static string ForgivenPedantryRegex { get; private set; } = ForgivenPedantryRegex_en;
    public static string IxtabRegex { get; private set; } = IxtabRegex_en;
    public static string SphatikaRegex { get; private set; } = SphatikaRegex_en;
    public static string RuinatorRegex { get; private set; } = RuinatorRegex_en;

    public static void SetCounterLanguage(ClientLanguage lang)
    {
        if (lang == ClientLanguage.English) return;
        if (lang == ClientLanguage.French)
        {
            Minhocao = Minhocao_fr;
            Squonk = Squonk_fr;
            Leucrotta = Leucrotta_fr;
            Gandawera = Gandawera_fr;
            Okina = Okina_fr;
            Udumbara = Udumbara_fr;
            SaltAndLight = SaltAndLight_fr;
            ForgivenPedantry = ForgivenPedantry_fr;
            Ixtab = Ixtab_fr;
            Sphatika = Sphatika_fr;
            Ruinator = Ruinator_fr;

            MinhocaoRegex = MinhocaoRegex_fr;
            SquonkRegex = SquonkRegex_fr;
            LeucrottaRegex = LeucrottaRegex_fr;
            GandaweraRegex = GandaweraRegex_fr;
            OkinaRegex = OkinaRegex_fr;
            UdumbaraRegex = UdumbaraRegex_fr;
            SaltAndLightRegex = SaltAndLightRegex_fr;
            ForgivenPedantryRegex = ForgivenPedantryRegex_fr;
            IxtabRegex = IxtabRegex_fr;
            SphatikaRegex = SphatikaRegex_fr;
            RuinatorRegex = RuinatorRegex_fr;
        }

        else if (lang == ClientLanguage.German)
        {
            Minhocao = Minhocao_de;
            Squonk = Squonk_de;
            Leucrotta = Leucrotta_de;
            Gandawera = Gandawera_de;
            Okina = Okina_de;
            Udumbara = Udumbara_de;
            SaltAndLight = SaltAndLight_de;
            ForgivenPedantry = ForgivenPedantry_de;
            Ixtab = Ixtab_de;
            Sphatika = Sphatika_de;
            Ruinator = Ruinator_de;

            MinhocaoRegex = MinhocaoRegex_de;
            SquonkRegex = SquonkRegex_de;
            LeucrottaRegex = LeucrottaRegex_de;
            GandaweraRegex = GandaweraRegex_de;
            OkinaRegex = OkinaRegex_de;
            UdumbaraRegex = UdumbaraRegex_de;
            SaltAndLightRegex = SaltAndLightRegex_de;
            ForgivenPedantryRegex = ForgivenPedantryRegex_de;
            IxtabRegex = IxtabRegex_de;
            SphatikaRegex = SphatikaRegex_de;
            RuinatorRegex = RuinatorRegex_de;
        }

        else if (lang == ClientLanguage.Japanese)
        {
            Minhocao = Minhocao_ja;
            Squonk = Squonk_ja;
            Leucrotta = Leucrotta_ja;
            Gandawera = Gandawera_ja;
            Okina = Okina_ja;
            Udumbara = Udumbara_ja;
            SaltAndLight = SaltAndLight_ja;
            ForgivenPedantry = ForgivenPedantry_ja;
            Ixtab = Ixtab_ja;
            Sphatika = Sphatika_ja;
            Ruinator = Ruinator_ja;

            MinhocaoRegex = MinhocaoRegex_ja;
            SquonkRegex = SquonkRegex_ja;
            LeucrottaRegex = LeucrottaRegex_ja;
            GandaweraRegex = GandaweraRegex_ja;
            OkinaRegex = OkinaRegex_ja;
            UdumbaraRegex = UdumbaraRegex_ja;
            SaltAndLightRegex = SaltAndLightRegex_ja;
            ForgivenPedantryRegex = ForgivenPedantryRegex_ja;
            IxtabRegex = IxtabRegex_ja;
            SphatikaRegex = SphatikaRegex_ja;
            RuinatorRegex = RuinatorRegex_ja;
        }

        else //(lang == ClientLanguage.ChineseSimplified) // ChineseSimplified does not exist in global dalamud version
        {
            Minhocao = Minhocao_chs;
            Squonk = Squonk_chs;
            Leucrotta = Leucrotta_chs;
            Gandawera = Gandawera_chs;
            Okina = Okina_chs;
            Udumbara = Udumbara_chs;
            SaltAndLight = SaltAndLight_chs;
            ForgivenPedantry = ForgivenPedantry_chs;
            Ixtab = Ixtab_chs;
            Sphatika = Sphatika_chs;
            Ruinator = Ruinator_chs;

            MinhocaoRegex = MinhocaoRegex_chs;
            SquonkRegex = SquonkRegex_chs;
            LeucrottaRegex = LeucrottaRegex_chs;
            GandaweraRegex = GandaweraRegex_chs;
            OkinaRegex = OkinaRegex_chs;
            UdumbaraRegex = UdumbaraRegex_chs;
            SaltAndLightRegex = SaltAndLightRegex_chs;
            ForgivenPedantryRegex = ForgivenPedantryRegex_chs;
            IxtabRegex = IxtabRegex_chs;
            SphatikaRegex = SphatikaRegex_chs;
            RuinatorRegex = RuinatorRegex_chs;
        }
    }
}
