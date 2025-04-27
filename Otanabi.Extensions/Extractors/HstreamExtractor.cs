using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace Otanabi.Extensions.Extractors;

public class HstreamExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 4;
    internal readonly string sourceName = "Hstream";
    internal readonly string originUrl = "https://hstream.moe";
    internal readonly bool Persistent = true;
    internal readonly string Type = "ANIME";
    internal readonly bool IsAdult = true;

    //internal readonly HttpService HService =  new ();
    private static readonly HttpClient client = new();

    public string GetSourceName() => sourceName;

    public string GetUrl() => originUrl;

    public IProvider GenProvider() =>
        new Provider
        {
            Id = extractorId,
            Name = sourceName,
            Url = originUrl,
            Type = Type,
            Persistent = Persistent,
            IsAdult = IsAdult,
        };

    public async Task<IAnime[]> MainPageAsync(int page = 1, Tag[]? tags = null)
    {
        return await SearchAnimeAsync("", page, tags);
    }

    public async Task<IAnime[]> SearchAnimeAsync(string searchTerm, int page, Tag[]? tags = null)
    {
        var animeList = new List<Anime>();

        var url = string.Concat(originUrl, $"/search/?page={page}&view=poster");
        if (searchTerm != "")
        {
            url = string.Concat(originUrl, $"/search/?page={page}&view=poster&s={Uri.EscapeDataString(searchTerm)}");
        }
        if (tags != null && tags.Length > 0)
        {
            url += $"&{GenerateTagString(tags)}";
        }

        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);
        oWeb.UseCookies = true;

        var nodes = doc.DocumentNode.SelectNodes("/html/body/div/main/div[2]/div[2]/div/div/div/div");
        foreach (var node in nodes)
        {
            try
            {
                var img = node.CssSelect("div a img").First();
                var pttitle = img.GetAttributeValue("alt");
                var pturl = img.GetAttributeValue("src");
                var temp = pttitle.Split(" - ");
                var temp2 = pturl.Split("/");

                Anime anime = new()
                {
                    Title = temp[0],
                    Url = string.Concat(originUrl, "/", temp2[2], "/", temp2[3]),
                    Cover = string.Concat(originUrl, img.Attributes["src"].Value),
                    Type = AnimeType.OVA,
                    Status = "",
                    Provider = (Provider)GenProvider(),
                };
                anime.ProviderId = anime.Provider.Id;
                animeList.Add(anime);
            }
            catch (Exception)
            {
                continue;
            }
        }
        return animeList.ToArray();
    }

    public async Task<IAnime> GetAnimeDetailsAsync(string requestUrl)
    {
        var browser = new ScrapingBrowser { Encoding = Encoding.UTF8 };
        var webPage = await browser.NavigateToPageAsync(new Uri(requestUrl));
        var doc = webPage.Html.CssSelect("body").First();
        var img = doc.SelectSingleNode(".//div/main/div/div/div[1]/div[1]/div[1]/img");

        var geners = doc.CssSelect("ul.list-none > li > a")
            .Select(x => Regex.Replace(x.InnerText, @"\t|\n|\r", ""))
            .Select(x => x.Trim())
            .ToList();
        var anime = new Anime
        {
            Url = requestUrl,
            Title = Regex
                .Replace(doc.SelectSingleNode(".//div/main/div/div/div[1]/div[1]/div[2]/h1").InnerText, @"\t|\n|\r", "")
                .Trim(),
            Cover = string.Concat(originUrl, img.Attributes["src"].Value),
            Description = doc.SelectSingleNode(".//div/main/div/div/div[1]/div[1]/div[2]/p[2]").InnerText.Trim(),
            Type = AnimeType.OVA,
            //the provider does not return a status... sooo i'll put this one
            Status = "Completed",
            GenreStr = string.Join(",", geners),
            RemoteID = requestUrl.Replace("/", ""),
        };

        anime.Provider = (Provider)GenProvider();
        anime.ProviderId = anime.Provider.Id;

        var chapters = new List<Chapter>();

        var nodes = doc.SelectNodes(".//div/main/div/div/div[1]/div[2]/div/div");

        var i = 1;
        foreach (var node in nodes)
        {
            var chapter = new Chapter
            {
                Url = node.CssSelect("a").First().GetAttributeValue("href"),
                ChapterNumber = i,
                Name = node.CssSelect("a div p").First().InnerText,
                Extraval = node.CssSelect("a").First().GetAttributeValue("href"),
            };
            chapters.Add(chapter);
            i++;
        }

        anime.Chapters = chapters;

        await Task.CompletedTask;
        return anime;
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var videoSources = new List<VideoSource>();

        var response = await client.GetAsync(requestUrl);
        var responseBody = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(responseBody);

        var cookies = response.Headers.GetValues("Set-Cookie");
        var token = cookies.First(cookie => cookie.Contains("XSRF-TOKEN")).Split('=')[1];
        token = token.Replace("; expires", "");
        token = token.Replace(" SameSite", "");
        var episodeId = doc.DocumentNode.SelectSingleNode("//input[@id='e_id']").GetAttributeValue("value", "");
        var strCookie = string.Join("; ", cookies);
        var newHeaders = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{originUrl}/player/api"),
            Headers =
            {
                { "Referer", response.RequestMessage.RequestUri.ToString() },
                { "Origin", originUrl },
                { "X-Requested-With", "XMLHttpRequest" },
                { "X-XSRF-TOKEN", Uri.UnescapeDataString(token) },
                { "Cookie", strCookie },
            },
            Content = new StringContent($"{{\"episode_id\": \"{episodeId}\"}}", Encoding.UTF8, "application/json"),
        };

        var apiResponse = await client.SendAsync(newHeaders);
        var apiResponseBody = await apiResponse.Content.ReadAsStringAsync();
        var data = JObject.Parse(apiResponseBody);

        var sD = (JArray)data["stream_domains"];
        var streamDomains = sD.ToObject<List<string>>();

        var resolutions = new List<string> { "720", "1080" };

        var urlBase = streamDomains[new Random().Next(streamDomains.Count)] + "/" + (string)data["stream_url"];

        foreach (var resolution in resolutions)
        {
            var dest = string.Concat(urlBase, "/", resolution, "/manifest.mpd");
            var vsouce = new VideoSource
            {
                Server = "Juro",
                Title = "Juro",
                Code = dest,
                Url = dest,
                Subtitle = $"{urlBase}/eng.ass",
            };
            videoSources.Add(vsouce);
        }

        return videoSources.ToArray();
    }

    public static string GenerateTagString(Tag[] tags)
    {
        var result = "";
        for (var i = 0; i < tags.Length; i++)
        {
            result += $"tags[{i}]={tags[i].Value}";
            if (i < tags.Length - 1)
            {
                result += "&";
            }
        }
        return result;
    }

    public Tag[] GetTags()
    {
        return new Tag[]
        {
            new() { Name = "Yuri", Value = "yuri" },
            new() { Name = "Ahegao", Value = "ahegao" },
            new() { Name = "Bestiality", Value = "bestiality" },
            new() { Name = "Bondage", Value = "bondage" },
            new() { Name = "Creampie", Value = "creampie" },
            new() { Name = "Gore", Value = "gore" },
            new() { Name = "Harem", Value = "harem" },
            new() { Name = "Incest", Value = "incest" },
            new() { Name = "Lactation", Value = "lactation" },
            new() { Name = "Lq", Value = "lq" },
            new() { Name = "Mind Break", Value = "mind-break" },
            new() { Name = "Mind Control", Value = "mind-control" },
            new() { Name = "Scat", Value = "scat" },
            new() { Name = "Tentacle", Value = "tentacle" },
            new() { Name = "Toys", Value = "toys" },
            new() { Name = "Tsundere", Value = "tsundere" },
            new() { Name = "Virgin", Value = "virgin" },
            new() { Name = "Yuri", Value = "yuri" },
            new() { Name = "Anal", Value = "anal" },
            new() { Name = "Bdsm", Value = "bdsm" },
            new() { Name = "Facial", Value = "facial" },
            new() { Name = "Blow Job", Value = "blow-job" },
            new() { Name = "Boob Job", Value = "boob-job" },
            new() { Name = "Foot Job", Value = "foot-job" },
            new() { Name = "Hand Job", Value = "hand-job" },
            new() { Name = "Rimjob", Value = "rimjob" },
            new() { Name = "Inflation", Value = "inflation" },
            new() { Name = "Masturbation", Value = "masturbation" },
            new() { Name = "Public Sex", Value = "public-sex" },
            new() { Name = "Rape", Value = "rape" },
            new() { Name = "Reverse Rape", Value = "reverse-rape" },
            new() { Name = "Threesome", Value = "threesome" },
            new() { Name = "Orgy", Value = "orgy" },
            new() { Name = "Gangbang", Value = "gangbang" },
            new() { Name = "Loli", Value = "loli" },
            new() { Name = "Shota", Value = "shota" },
            new() { Name = "Milf", Value = "milf" },
            new() { Name = "Futanari", Value = "futanari" },
            new() { Name = "Big Boobs", Value = "big-boobs" },
            new() { Name = "Small Boobs", Value = "small-boobs" },
            new() { Name = "Dark Skin", Value = "dark-skin" },
            new() { Name = "Cosplay", Value = "cosplay" },
            new() { Name = "Elf", Value = "elf" },
            new() { Name = "Maid", Value = "maid" },
            new() { Name = "Nekomimi", Value = "nekomimi" },
            new() { Name = "Nurse", Value = "nurse" },
            new() { Name = "School Girl", Value = "school-girl" },
            new() { Name = "Succubus", Value = "succubus" },
            new() { Name = "Teacher", Value = "teacher" },
            new() { Name = "Trap", Value = "trap" },
            new() { Name = "Pregnant", Value = "pregnant" },
            new() { Name = "Glasses", Value = "glasses" },
            new() { Name = "Swim Suit", Value = "swim-suit" },
            new() { Name = "Ugly Bastard", Value = "ugly-bastard" },
            new() { Name = "Monster", Value = "monster" },
            new() { Name = "3D", Value = "3d" },
            new() { Name = "4K", Value = "4k" },
            new() { Name = "48Fps", Value = "48fps" },
            new() { Name = "4K 48Fps", Value = "4k-48fps" },
            new() { Name = "Censored", Value = "censored" },
            new() { Name = "Uncensored", Value = "uncensored" },
            new() { Name = "Comedy", Value = "comedy" },
            new() { Name = "Fantasy", Value = "fantasy" },
            new() { Name = "Horror", Value = "horror" },
            new() { Name = "Vanilla", Value = "vanilla" },
            new() { Name = "Ntr", Value = "ntr" },
            new() { Name = "Pov", Value = "pov" },
            new() { Name = "Filmed", Value = "filmed" },
            new() { Name = "X-Ray", Value = "x-ray" },
        };
    }
}
