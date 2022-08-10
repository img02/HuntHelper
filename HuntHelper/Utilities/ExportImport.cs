using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Unicode;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HuntHelper.Utilities;

public class ExportImport
{
    public ExportImport()
    {

    }


    public static string Export<T>(T objectToSerialise)
    {
        var byteArr = Compress(JsonConvert.SerializeObject(objectToSerialise));
        return Convert.ToBase64String(byteArr);
    }

    public static void Import<T>(string importCode, List<T> listToDeserialiseTo)
    {
        try
        {
            var decompressed = Decompress(importCode);
            PluginLog.Warning("imoprt:|"+decompressed+"|");
            var result = JsonConvert.DeserializeObject<List<T>>(decompressed);
            if (result == null) return;
            listToDeserialiseTo.AddRange(result);
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);
            return;
        }
    }

    //shamelessly copied from https://github.com/goatcorp/Dalamud/blob/2085cb03cad9554f82cb520d6216ba09fc550266/Dalamud/Utility/Util.cs#L442
    private static byte[] Compress(string toCompress)
    {
        var byteArr = Encoding.UTF8.GetBytes(toCompress);
        using var input = new MemoryStream(byteArr);
        using var output = new MemoryStream();
        using (var compressor = new GZipStream(output, CompressionMode.Compress))
        {
            input.CopyTo(compressor);
        }
        return output.ToArray();
    }

    private static string Decompress(string toDecompress)
    {
        var byteArr = Convert.FromBase64String(toDecompress);
        using var input = new MemoryStream(byteArr);
        using var output = new MemoryStream();
        using (var decompresser = new GZipStream(input, CompressionMode.Decompress))
        {
            decompresser.CopyTo(output);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }
}