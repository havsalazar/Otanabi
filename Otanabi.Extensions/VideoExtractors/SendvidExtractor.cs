using System.Net.Http.Headers;
using HtmlAgilityPack;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

public class SendvidExtractor : IVideoExtractor
{
    private readonly HttpClient _client = new();

    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        var masterUrl = "";
        try
        {
            var response = await _client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var sourceNode = doc.DocumentNode.SelectSingleNode("//source[@id='video_source']");
            masterUrl = sourceNode?.GetAttributeValue("src", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendvidExtractor] Error extracting stream: {ex.Message}");
        }
        return (masterUrl, null);
    }
}