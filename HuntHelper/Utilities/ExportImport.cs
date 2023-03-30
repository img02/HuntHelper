using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HuntHelper.Utilities;

public static class ExportImport
{
    public static string Export<T>(T objectToSerialise)
    {
        var byteArr = Compress(JsonConvert.SerializeObject(objectToSerialise));
        return Convert.ToBase64String(byteArr);
    }


    //if failed, returns original object. 
    public static T Import<T>(string importCode, T objectToDeserialiseTo)
    {
        try
        {
            var decompressed = Decompress(importCode);
            var result = JsonConvert.DeserializeObject<T>(decompressed);
            return result ?? objectToDeserialiseTo;
        }
        catch (Exception e)
        {
            /*PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace);*/
            return objectToDeserialiseTo;
        }
    }

    //copied from https://github.com/goatcorp/Dalamud/blob/2085cb03cad9554f82cb520d6216ba09fc550266/Dalamud/Utility/Util.cs#L442
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