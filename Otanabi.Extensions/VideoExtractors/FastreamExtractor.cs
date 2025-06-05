using System.Net.Http.Headers;
using HtmlAgilityPack;
using JsUnpacker;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

public class FastreamExtractor : IVideoExtractor
{
    private readonly HttpClient _client = new();
    private const string FastreamUrl = "https://fastream.to";
    private readonly HttpRequestHeaders _headers = new HttpClient().DefaultRequestHeaders;

    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        var videoUrl = "";
        try
        {
            var firstDoc = await _client.GetStringAsync(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(firstDoc);

            HtmlNode scriptElement = null;
            if (htmlDoc.DocumentNode.SelectNodes("//input[@name]").Any())
            {
                var formData = new FormUrlEncodedContent(htmlDoc.DocumentNode
                    .SelectNodes("//input[@name]")
                    .Select(node => new KeyValuePair<string, string>(node.GetAttributeValue("name", ""), node.GetAttributeValue("value", "")))
                );

                var response = await _client.PostAsync(url, formData);
                var postResponse = await response.Content.ReadAsStringAsync();
                var postHtmlDoc = new HtmlDocument();
                postHtmlDoc.LoadHtml(postResponse);

                scriptElement = postHtmlDoc.DocumentNode.SelectSingleNode("//script[contains(text(),'jwplayer') and contains(text(),'vplayer')]");
            }
            else
            {
                scriptElement = htmlDoc.DocumentNode.SelectSingleNode("//script[contains(text(),'jwplayer') and contains(text(),'vplayer')]");
            }

            var scriptData = scriptElement.InnerText;
            if (scriptData.Contains("eval(function("))
            {
                scriptData = Unpacker.UnpackAndCombine(scriptData) ?? string.Empty;
            }

            videoUrl = scriptData.SubstringAfter("file:\"").SubstringBefore("\"").Trim();

            _headers.Add("Referer", $"{FastreamUrl}/");
            _headers.Referrer = new Uri($"{FastreamUrl}/");
            _headers.Add("Origin", FastreamUrl);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FastreamExtractor] Error extracting video: {ex.Message}");
        }
        return (videoUrl, _headers);
    }
}