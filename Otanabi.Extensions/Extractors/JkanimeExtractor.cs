using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace Otanabi.Extensions.Extractors;

public class JkanimeExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 2;
    internal readonly string sourceName = "Jkanime";
    internal readonly string originUrl = "https://jkanime.net/";
    internal readonly bool Persistent = true;
    internal readonly string Type = "ANIME";

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
            Active = false,
        };

    public async Task<IAnime[]> MainPageAsync(int page = 1, Tag[]? tags = null)
    {
        var animeList = new List<Anime>();
        var mainPUrl = string.Concat(originUrl, "directorio/", page);

        var browser = new ScrapingBrowser { Encoding = Encoding.UTF8 };
        WebPage webPage = await browser.NavigateToPageAsync(new Uri(mainPUrl));

        var doc = webPage.Html.CssSelect("body").First();
        var tt = doc.InnerHtml;
        var animesdata = tt.SubstringBetween("var animes = ", "var mode");
        //var pattern = @"""studios"":\s*\{[^}]*\}\s*,";
        //var replacement = "";

        //var result = Regex.Replace(animesdata, pattern, replacement);

        try
        {
            var data = JArray.Parse(animesdata.Remove(animesdata.Length - 2));

            foreach (var nodo in data)
            {
                var anime = new Anime();
                anime.Title = (string)nodo["title"];
                anime.Url = (string)nodo["slug"];
                anime.Provider = (Provider)GenProvider();
                anime.ProviderId = anime.Provider.Id;
                anime.Status = (string)nodo["status"];
                anime.Cover = (string)nodo["image"];
                anime.Type = getAnimeTypeByStr((string)nodo["type"]);
                animeList.Add(anime);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }

        return animeList.ToArray();
    }

    public async Task<IAnime[]> SearchAnimeAsync(string searchTerm, int page, Tag[]? tags = null)
    {
        var animeList = new List<Anime>();
        var tmp = searchTerm == "" ? "" : searchTerm.Replace(" ", "_") + "/";
        var mainPUrl = string.Concat(originUrl, "buscar/", tmp, page);
        var browser = new ScrapingBrowser();
        WebPage webPage = await browser.NavigateToPageAsync(new Uri(mainPUrl));

        var doc = webPage.Html.CssSelect("body").First();

        var removeDomain = (string e) => e.Replace(originUrl, "");

        foreach (var nodo in doc.CssSelect(".anime__page__content .row .col-md-6"))
        {
            var anime = new Anime();
            anime.Title = nodo.CssSelect("h5 a").First().InnerText;
            var temp = nodo.CssSelect(".anime__item a").First();
            anime.Url = removeDomain(temp.GetAttributeValue("href"));
            anime.Cover = nodo.CssSelect(".anime__item__pic").First().GetAttributeValue("data-setbg");
            anime.Provider = (Provider)GenProvider();
            anime.ProviderId = anime.Provider.Id;
            anime.Type = getAnimeTypeByStr(nodo.CssSelect(".anime__item__text ul li.anime").First().InnerText);
            animeList.Add(anime);
        }

        await Task.CompletedTask;

        return animeList.ToArray();
    }

    public async Task<IAnime> GetAnimeDetailsAsync(string requestUrl)
    {
        var mainPUrl = string.Concat(originUrl, requestUrl);
        var browser = new ScrapingBrowser { Encoding = Encoding.UTF8 };
        WebPage webPage = await browser.NavigateToPageAsync(new Uri(mainPUrl));

        var doc = webPage.Html.CssSelect("body").First();

        var anime = new Anime();
        anime.Url = requestUrl;
        anime.Title = doc.CssSelect(".anime__details__title h3").First().InnerText;
        anime.Cover = doc.CssSelect(".anime__details__pic").First().GetAttributeValue("data-setbg");
        anime.Description = doc.CssSelect(".sinopsis").First().InnerText;
        anime.Provider = (Provider)GenProvider();
        anime.ProviderId = anime.Provider.Id;
        var tempType = doc.SelectSingleNode(
            ".//section[2]/div/div[1]/div/div[2]/div/div[2]/div/div[1]/ul/li[1]"
        ).InnerText;
        anime.Type = getAnimeTypeByStr(tempType);

        anime.Status = doc.CssSelect("span.enemision").First().InnerText;

        anime.RemoteID = requestUrl.Replace("/", "");

        //do some magic things
        var lastlink = doc.CssSelect("a.numbers").Last().InnerText;
        var ttr = lastlink.Split(" - ");
        var lastEpisode = ttr[1];

        //var lastEpisode = doc.CssSelect("a#uep").First().GetAttributeValue("href");
        var lastchap = 1;
        if (!string.IsNullOrEmpty(lastEpisode))
            lastchap = int.Parse(lastEpisode);

        var chapters = new List<Chapter>();
        for (var i = 1; i <= lastchap; i++)
        {
            var chapter = new Chapter();
            chapter.Url = string.Concat(requestUrl, "/", i, "/");
            chapter.ChapterNumber = i;
            chapter.Name = string.Concat(requestUrl.Replace("/", ""), " ", i);
            chapters.Add(chapter);
        }
        anime.Chapters = chapters.ToArray();
        await Task.CompletedTask;
        return anime;
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var videoSource = new List<VideoSource>();
        var mainPUrl = string.Concat(originUrl, requestUrl);

        var browser = new ScrapingBrowser();
        WebPage webPage = await browser.NavigateToPageAsync(new Uri(mainPUrl));
        var doc = webPage.Html.InnerHtml;

        var match = doc.SubstringBetween("var servers = ", ";");

        Debug.WriteLine(match);
        try
        {
            var prefJson = JArray.Parse(match);

            foreach (JObject vsource in prefJson.Children<JObject>())
            {
                var serverName = _serverConventions.GetServerName((string)vsource["server"]);

                if (string.IsNullOrEmpty(serverName))
                {
                    continue;
                }
                var iframe = originUrl + "/c1.php?u=" + (string)vsource["remote"] + "&s=" + serverName.ToLower();
                var tempframe = await browser.NavigateToPageAsync(new Uri(iframe));
                var iframedoc = tempframe.Html.SelectSingleNode(".//iframe").GetAttributeValue("src");
                var vSouce = new VideoSource();
                vSouce.Server = serverName;
                vSouce.Title = serverName;
                vSouce.Code = iframedoc;
                vSouce.Url = iframedoc;
                videoSource.Add(vSouce);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
        await Task.CompletedTask;
        return videoSource.ToArray();
    }

    private AnimeType getAnimeTypeByStr(string strType)
    {
        switch (strType)
        {
            case "OVA":
                return AnimeType.OVA;
            case "ONA":
                return AnimeType.OVA;
            case "TV":
                return AnimeType.TV;
            case "Movie":
                return AnimeType.MOVIE;
            case "Special":
                return AnimeType.SPECIAL;
            default:
                return AnimeType.TV;
        }
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public Tag[] GetTags()
    {
        return Array.Empty<Tag>();
    }
}
