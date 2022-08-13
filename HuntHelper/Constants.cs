using System.Collections.Generic;

namespace HuntHelper;

public static class Constants
{
    public const string BaseImageUrl = "https://raw.githubusercontent.com/imaginary-png/HuntHelper-Resources/main/Images/Maps/";
    public const string RepoUrl = "https://github.com/imaginary-png/HuntHelper-Resources/";

    //arr
    public static readonly string[] Minhocao = {"Earth Sprite"};
    //hw
    public static readonly string[] Leucrotta = {"Allagan Chimera", "Lesser Hydra", "Meracydian Vouivre"};
    public static readonly string[] Gandawera = { "Aurum Regis Ore", "Seventh Heaven" };
    //sb
    public static readonly string[] Okina = { "Naked Yumemi", "Yumemi" }; //naked goes first here, otherwise minor issue w/ matching 'naked yumemi' to 'yumemi'
    public static readonly string[] Udumbara = { "Leshy", "Diakka" };
    //shb
    public static readonly string[] ForgivenPedantry = {"Dwarven Cotton Boll"};
    public static readonly string[] Ixtab = { "Cracked Ronkan Doll", "Cracked Ronkan Thorn", "Cracked Ronkan Vessel"};
    //ew
    public static readonly string[] Sphatika = { "Asvattha", "Pisaca", "Vajralangula" };
    public static readonly string[] Ruinator = { "Thinker", "Wanderer", "Weeper" };

    //arr
    public const string MinhocaoRegex = "(?i)(defeat|defeats) the earth sprite.";
    //hw
    public const string LeucrottaRegex = "(?i)(defeat|defeats) the (Allagan chimera|lesser hydra|Meracydian vouivre).";
    public const string GandaweraRegex = "(?i)You obtain .*(aurum regis ore|seventh heaven)";
    //sb
    public const string OkinaRegex = "(?i)(defeat|defeats) the (Yumemi|Naked Yumemi).";
    public const string UdumbaraRegex = "(?i)(defeat|defeats) the (Leshy|Diakka).";


}