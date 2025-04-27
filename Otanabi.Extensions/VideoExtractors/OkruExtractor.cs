using System.Net.Http.Headers;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Otanabi.Extensions.Contracts.VideoExtractors;
using ScrapySharp.Extensions;

namespace Otanabi.Extensions.VideoExtractors;

public class OkruExtractor : IVideoExtractor
{
    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        // url = "https://ok.ru/videoembed/947875089023";
        var streaminUrl = "";
        try
        {
            HtmlWeb oWeb = new HtmlWeb();
            HtmlDocument doc = await oWeb.LoadFromWebAsync(url);
            var values = doc
                .DocumentNode.SelectSingleNode("/html/body/div[2]/div")
                .GetAttributeValue("data-options")
                .Replace("&quot;", "\"");
            dynamic contourManifest = JObject.Parse(values);
            var metadata = (string)contourManifest.flashvars["metadata"];
            var meta2 = JObject.Parse(metadata);
            var videos = meta2["videos"];
            foreach (var video in videos)
            {
                if ((string)video["name"] == "hd")
                {
                    streaminUrl = (string)video["url"];
                }
            }
        }
        catch (Exception) { }

        return (streaminUrl, null);
    }
}
