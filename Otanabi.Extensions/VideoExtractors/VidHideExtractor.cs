using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JsUnpacker;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

public class VidHideExtractor : IVideoExtractor
{
    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        var videoUrl = "";
        try
        {
            using var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var scriptNode = doc.DocumentNode
                .SelectNodes("//script")
                ?.FirstOrDefault(n => n.InnerText.Contains("eval(function(p,a,c,k,e,d)"));

            if (scriptNode != null)
            {
                var unpacked = Unpacker.UnpackAndCombine(scriptNode.InnerText);

                if (!string.IsNullOrWhiteSpace(unpacked))
                {
                    var match = Regex.Match(unpacked, @"sources:\[\{file:""(.*?)""");
                    if (match.Success)
                    {
                        videoUrl = match.Groups[1].Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting video URL: {ex.Message}");
        }

        return (videoUrl, null);
    }
}