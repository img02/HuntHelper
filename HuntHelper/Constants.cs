namespace HuntHelper;

public static class Constants
{
    public const string BaseImageUrl = "https://raw.githubusercontent.com/imaginary-png/HuntHelper-Resources/main/Images/Maps/";
    public const string RepoUrl = "https://github.com/imaginary-png/HuntHelper-Resources/";

    //arr
    public static readonly string[] Minhocao = { "Earth Sprite" };
    //hw
    public static readonly string[] Leucrotta = { "Allagan Chimera", "Lesser Hydra", "Meracydian Vouivre" };
    public static readonly string[] Gandawera = { "Aurum Regis Ore", "Seventh Heaven" };
    //sb
    public static readonly string[] Okina = { "Naked Yumemi", "Yumemi" }; //naked goes first here, otherwise minor issue w/ matching 'naked yumemi' to 'yumemi'
    public static readonly string[] Udumbara = { "Leshy", "Diakka" };
    public static readonly string[] SaltAndLight = { "Throw" };
    //shb
    public static readonly string[] ForgivenPedantry = { "Dwarven Cotton Boll" };
    public static readonly string[] Ixtab = { "Cracked Ronkan Doll", "Cracked Ronkan Thorn", "Cracked Ronkan Vessel" };
    //ew
    public static readonly string[] Sphatika = { "Asvattha", "Pisaca", "Vajralangula" };
    public static readonly string[] Ruinator = { "Thinker", "Wanderer", "Weeper" };

    //arr
    public const string MinhocaoRegex = $"{BattleRegexBase}earth sprite.";
    //hw
    public const string LeucrottaRegex = $"{BattleRegexBase}(Allagan chimera|lesser hydra|Meracydian vouivre).";
    public const string GandaweraRegex = $"{GatheringRegexBase}(aurum regis ore|seventh heaven)";
    //sb
    public const string OkinaRegex = $"{BattleRegexBase}(Yumemi|Naked Yumemi).";
    public const string UdumbaraRegex = $"{BattleRegexBase}(Leshy|Diakka).";
    public const string SaltAndLightRegex = $"You throw away.*";
    //sbh
    public const string ForgivenPedantryRegex = $"{GatheringRegexBase}dwarven cotton (boll|bolls)";
    public const string IxtabRegex = $"{BattleRegexBase}Cracked (Ronkan Doll|Ronkan Thorn|Ronkan Vessel).";
    //ew
    public const string SphatikaRegex = $"{BattleRegexBase}(Asvattha|Pisaca|Vajralangula).";
    public const string RuinatorRegex = $"{BattleRegexBase}(Thinker|Wanderer|Weeper).";

    private const string BattleRegexBase = "(?i)(defeat|defeats) the ";
    private const string GatheringRegexBase = "(?i)You obtain.*";

    //ss ids
    public const uint SS_Ker = 10615;
    public const uint SS_Forgiven_Rebellion = 8915;

    //minion ids
    public const uint WeeEa = 423;
}