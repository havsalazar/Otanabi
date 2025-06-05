using System.Net.Http.Headers;
using HtmlAgilityPack;
using JsUnpacker;
using Otanabi.Extensions.Contracts.VideoExtractors;
using ScrapySharp.Extensions;

namespace Otanabi.Extensions.VideoExtractors;

public class FilemoonExtractor : IVideoExtractor
{
    private readonly HttpClient _client = new();

    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Referer", url);
            request.Headers.Add("Origin", $"https://{host}");

            var response = await _client.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var needsRedirect = doc.DocumentNode.CssSelect("#iframe-holder iframe");
            var redirectUrl = string.Empty;
            if (needsRedirect.Any())
            {
                redirectUrl = needsRedirect.FirstOrDefault().GetAttributeValue("src")
                                .SubstringAfter("window.location.href = '").SubstringBefore("';");
                html = await _client.GetStringAsync(redirectUrl);
                doc.LoadHtml(html);
            }

            var scriptNode = doc.DocumentNode
                .SelectSingleNode("//script[contains(text(),'eval') and contains(text(),'m3u8')]");

            if (scriptNode == null)
            {
                return ("", null);
            }

            var unpacked = Unpacker.UnpackAndCombine(scriptNode.InnerText) ?? string.Empty;

            var masterUrl = unpacked
                .SubstringAfter("{file:\"")
                .SubstringBefore("\"}")
                .Trim();

            return (masterUrl, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FilemoonExtractor] Error extracting stream: {ex.Message}");
        }

        return ("", null);
    }
}