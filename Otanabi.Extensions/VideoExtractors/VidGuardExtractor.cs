using System.Net.Http.Headers;
using HtmlAgilityPack;
using Jint;
using Newtonsoft.Json.Linq;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

public class VidGuardExtractor : IVideoExtractor
{
    public async Task<(string, HttpHeaders)> GetStreamAsync(string url)
    {
        var playlistUrl = "";
        try
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);
            var script = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'eval')]")?.InnerText;

            if (string.IsNullOrEmpty(script)) return new();

            // Ejecutar el JS y obtener la variable svg
            var svgJson = RunJsAndExtractSvg(script);
            if (string.IsNullOrEmpty(svgJson)) return new();

            var parsed = JObject.Parse(svgJson);
            var streamUrl = parsed["stream"]?.ToString();
            if (string.IsNullOrEmpty(streamUrl)) return new();

            playlistUrl = DecodeSig(streamUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VidGuardExtractor] Error: {ex.Message}");
        }
        return (playlistUrl, null);
    }

    private static string RunJsAndExtractSvg(string script)
    {
        var engine = new Engine();
        engine.SetValue("window", new { });
        engine.Execute(script);
        var svg = engine.GetValue("svg").ToObject();
        return JObject.FromObject(svg).ToString();
    }

    private static string DecodeSig(string url)
    {
        var sig = url.Split("sig=")[1].Split('&')[0];
        var decoded = string.Join("", Enumerable.Range(0, sig.Length / 2)
            .Select(i => (char)(Convert.ToInt32(sig.Substring(i * 2, 2), 16) ^ 2)));

        var padded = decoded + (decoded.Length % 4 == 2 ? "==" : decoded.Length % 4 == 3 ? "=" : "");
        var base64Decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));

        var step1 = base64Decoded.Substring(0, base64Decoded.Length - 5);
        var reversed = new string(step1.Reverse().ToArray());

        var charArray = reversed.ToCharArray();
        for (int i = 0; i + 1 < charArray.Length; i += 2)
        {
            (charArray[i], charArray[i + 1]) = (charArray[i + 1], charArray[i]);
        }

        var finalSig = new string(charArray).Substring(0, charArray.Length - 5);
        return url.Replace(sig, finalSig);
    }
}