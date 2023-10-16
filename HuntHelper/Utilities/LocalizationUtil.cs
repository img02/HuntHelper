using Dalamud.Game.Text;

namespace HuntHelper.Utilities;

public static class LocalizationUtil
{
    public static string GetInstanceGlyph(this uint instance)
    {
        if (instance is < 1 or > 9) return "";
        return (SeIconChar.Instance1 + (int)instance - 1).ToIconString();
    }
}
