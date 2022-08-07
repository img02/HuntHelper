using System.Net.Mime;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;

namespace HuntHelper.Chat;

public class ChatMessage
{
    private SeString _message;
    public SeString Message => _message;

    public string Text => _message.TextValue;
    public ChatMessage()
    {
        _message = new SeString();
    }

    public ChatMessage(string msg)
    {
        _message = new SeString(new TextPayload(msg));
    }

    public void Prepend(string text)
    { //not tested
        _message = new SeString(new TextPayload(text)).Append(_message);
    }

    public void Append(string text)
    {
        _message.Append(new SeString(new TextPayload(text)));
    }

    //why didn't I use SeStringBuilder? because I'm stupid
    public void AddFlag(string placeName, float xCoord, float yCoord)
    {
        _message.Append(new IconPayload(BitmapFontIcon.GoldStar));
        var mapFlag = SeString.CreateMapLink(placeName, xCoord, yCoord);
        if (mapFlag != null) _message.Append(mapFlag);
        
        else _message.Append(new SeString(
            new TextPayload(
                $"|Error: Could not find {placeName} - Please submit an issue with the intended map location.|")))
            .Append(new IconPayload(BitmapFontIcon.NoCircle));

        /*var builder = new SeStringBuilder();
        builder.AddUiForeground("fdsf", 22);
        _message.Append(builder.BuiltString);*/
    }
}