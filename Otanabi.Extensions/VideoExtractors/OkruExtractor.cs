using System.Net.Http.Headers;
using AngleSharp;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;
public class OkruExtractor : IVideoExtractor
{

    private readonly IBrowsingContext _client;

    public OkruExtractor()
    {
        var config = Configuration.Default.WithDefaultLoader();
        _client = BrowsingContext.New(config);
    }

    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        try
        {
            var doc = await _client.OpenAsync(url);

            var videoString = doc.QuerySelector("div[data-options]").GetAttribute("data-options");
            if (videoString == null)
            {
                return new();
            }

            var playlistUrl = "";
            if (videoString.Contains("ondemandHls"))
            {
                playlistUrl = ExtractLink(videoString, "ondemandHls");
            }
            else if (videoString.Contains("ondemandDash"))
            {
                playlistUrl = ExtractLink(videoString, "ondemandDash");
            }
            else
            {
                playlistUrl = VideosFromJson(videoString).OrderByDescending(x => x.Quality).FirstOrDefault().videoUrl;
            }

            return (playlistUrl, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting master URL: {ex.Message}");
        }
        return new();
    }

    public static string ExtractLink(string input, string attr)
    {
        var startToken = $"{attr}\\\":\\\"";
        var startIndex = input.IndexOf(startToken);
        if (startIndex == -1)
            return string.Empty;

        startIndex += startToken.Length;
        var endIndex = input.IndexOf("\\\"", startIndex);
        if (endIndex == -1)
            return string.Empty;

        var extracted = input.Substring(startIndex, endIndex - startIndex);
        return extracted.Replace("\\\\u0026", "&");
    }

    public static List<(string videoUrl, string Quality)> VideosFromJson(string videoString, bool fixQualities = true)
    {
        var arrayData = videoString
            .SubstringAfter("\\\"videos\\\":[{\\\"name\\\":\\\"")
            .SubstringBefore("]");

        return arrayData
            .Split(["{\\\"name\\\":\\\""], StringSplitOptions.RemoveEmptyEntries)
            .Reverse()
            .Where(it => ExtractLink(it, "url").StartsWith("https://"))
            .Select(it =>
            {
                var videoUrl = ExtractLink(it, "url");
                var qualityRaw = it.SubstringBefore("\\\"");
                var quality = fixQualities ? FixQuality(qualityRaw) : qualityRaw;
                return (videoUrl, $"Okru:{quality}");
            })
            .ToList();
    }

    public static string FixQuality(string quality)
    {
        var qualities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ultra", "2160p" },
            { "quad", "1440p" },
            { "full", "1080p" },
            { "hd", "720p" },
            { "sd", "480p" },
            { "low", "360p" },
            { "lowest", "240p" },
            { "mobile", "144p" },
        };

        return qualities.TryGetValue(quality, out var result) ? result : quality;
    }
}
