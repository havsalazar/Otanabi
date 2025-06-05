using System.Net.Http.Headers;
using HtmlAgilityPack;
using JsUnpacker;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

public class StreamwishExtractor : IVideoExtractor
{
    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        var streamUrl = "";
        try
        {
            HtmlWeb oWeb = new();
            var doc = await oWeb.LoadFromWebAsync(url);
            var packedScript = doc.DocumentNode.Descendants()
                .FirstOrDefault(x => x.Name == "script" && x.InnerText?.Contains("eval") == true);

            if (packedScript != null && Unpacker.IsPacked(packedScript.InnerText))
            {
                var unpacked = Unpacker.UnpackAndCombine(packedScript.InnerText);
                if (unpacked != null && unpacked.Contains("var links=", StringComparison.OrdinalIgnoreCase))
                {
                    streamUrl = unpacked.SubstringAfter("hls2\":\"").Split(["\"}"], StringSplitOptions.None)[0];
                }
                else
                {
                    streamUrl = unpacked.SubstringAfter("sources:[{file:\"").Split(["\"}"], StringSplitOptions.None)[0];
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting video stream: {ex.Message}");
        }
        return (streamUrl, null);
    }
}
