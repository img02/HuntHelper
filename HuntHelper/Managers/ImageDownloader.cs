using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace HuntHelper.Managers;

public class ImageDownloader
{
    private List<string> _urls;
    private List<string> failedFiles;
    private string _savePath;


    public ImageDownloader(List<string> urls, string savePath)
    {
        _urls = urls;
        _savePath = savePath;
    }


    public async Task<List<string>> BeginDownloadAsync()
    {
        //var results = new List<string>();
        //_urls.ForEach( async url => results.Add(await DownloadAsync(url)));

        var tasks = _urls.Select(url => DownloadAsync(url));
        var results = await Task.WhenAll(tasks);
        return results.Where(s => s != string.Empty).ToList(); //return list of strings where failed
    }

    private async Task<string> DownloadAsync(string url)
    {
        var filename = url.Replace(Constants.BaseUrl, "");

        using (HttpClient httpc = new HttpClient())
        {
            var res = await httpc.GetAsync(url);
            //res.EnsureSuccessStatusCode(); //throws if fail
            if (!res.IsSuccessStatusCode) return $"Failed to download: {filename}";

            var content = await res.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(_savePath + filename, content);
            return string.Empty;
        }
    }
}