using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;
using ScrapySharp.Extensions;

namespace Otanabi.Extensions.Extractors;

public class AnimeflvExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 1;
    internal readonly string sourceName = "AnimeFLV";
    internal readonly string originUrl = "https://www3.animeflv.net";
    internal readonly bool Persistent = true;
    internal readonly string Type = "ANIME";
    internal readonly bool IsTrackeable = true;
    internal readonly bool AllowNativeSearch = true;

    public string GetSourceName()
    {
        return sourceName;
    }

    public string GetUrl()
    {
        return originUrl;
    }

    public IProvider GenProvider() =>
        new Provider
        {
            Id = extractorId,
            Name = sourceName,
            Url = originUrl,
            Type = Type,
            Persistent = Persistent,
            IsTrackeable = IsTrackeable,
            AllowNativeSearch = AllowNativeSearch,
        };

    public async Task<IAnime[]> MainPageAsync(int page = 1, Tag[]? tags = null)
    {
        var animeList = (Anime[])await SearchAnimeAsync("", page, tags);
        return animeList.ToArray();
    }

    public async Task<IAnime[]> SearchAnimeAsync(string searchTerm, int page, Tag[]? tags = null)
    {
        var animeList = new List<Anime>();

        var url = string.Concat(originUrl, "/browse?", $"page={page}");

        if (tags != null && tags.Length > 0)
        {
            url += $"&{GenerateTagString(tags)}";
        }
        if (!string.IsNullOrEmpty(searchTerm))
        {
            url += $"&q={HttpUtility.UrlEncode(searchTerm)}";
        }

        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);

        foreach (var nodo in doc.DocumentNode.CssSelect(".Anime"))
        {
            Anime anime = new()
            {
                Title = nodo.SelectSingleNode(".//h3").InnerText,
                Url = nodo.Descendants("a").First().GetAttributeValue("href"),
                Cover = nodo.SelectSingleNode(".//div/figure/img").GetAttributeValue("src"),
                Provider = (Provider)GenProvider(),
            };
            anime.ProviderId = anime.Provider.Id;
            anime.Type = GetAnimeTypeByStr(nodo.SelectSingleNode(".//a/div/span").InnerText);

            animeList.Add(anime);
        }
        return animeList.ToArray();
    }

    public async Task<IAnime> GetAnimeDetailsAsync(string requestUrl)
    {
        Anime anime = new();

        var url = string.Concat(originUrl, requestUrl);
        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);

        if (oWeb.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Exception("Anime could not be found");
        }

        var node = doc.DocumentNode.SelectSingleNode("/html/body");
        anime.Url = requestUrl;

        anime.Title = node.CssSelect("div.Wrapper > div > div > div.Ficha.fchlt > div.Container > h1")
            .First()
            .InnerText;
        var coverTmp = node.CssSelect(
                "div.Wrapper > div > div > div.Container > div > aside > div.AnimeCover > div > figure > img"
            )
            .First()
            .GetAttributeValue("src");
        anime.Cover = string.Concat(originUrl, coverTmp);
        anime.Description = node.CssSelect("div.Wrapper > div > div > div.Container > div > main > section")
            .First()
            .CssSelect("div.Description > p")
            .First()
            .InnerText;
        anime.Provider = (Provider)GenProvider();
        anime.ProviderId = anime.Provider.Id;
        var tempType = node.CssSelect("div.Wrapper > div > div > div.Ficha.fchlt > div.Container > span")
            .First()
            .InnerText;
        anime.Type = GetAnimeTypeByStr(tempType);

        anime.Status = node.CssSelect("div.Wrapper > div > div > div.Container > div > aside > p > span")
            .First()
            .InnerText;

        var genres = node.CssSelect(".Nvgnrs a").Select(x => WebUtility.HtmlDecode(x.InnerText)).ToList();
        anime.GenreStr = string.Join(",", genres);
        var alterTitles = node.CssSelect("div.Wrapper > div > div > div.Ficha.fchlt > div.Container >div")
            .First()
            .Descendants()
            .Where(n => n.Name == "span")
            .Select(n => WebUtility.HtmlDecode(n.InnerText))
            .ToList();
        anime.AlternativeTitlesStr = string.Join("!-!", alterTitles);

        var identifier = GetUriIdentify(node.InnerHtml, anime.Status);
        anime.Chapters = GetChaptersByregex(node.InnerHtml, identifier);
        anime.RemoteID = identifier[0];
        return anime;
    }

    private string[] GetUriIdentify(string text, string aStatus)
    {
        var pattern = @"anime_info = (\[.*])";
        var identifier = "";
        var chapUri = "";
        var chapName = "";

        var match = Regex.Match(text, pattern);
        if (match.Success)
        {
            var special = match.Groups[1].Value;
            /*
            special = special.Replace("[", "").Replace("]", "").Replace(@"""", "");
            var data = special.Split(new string[] { "," }, StringSplitOptions.None);
            */
            var data = match.Groups[1].Value.Trim('[', ']').Split(',');
            var dataList = new List<string>();

            foreach (var value in data)
            {
                // Remove quotes and trim extra whitespaces
                var trimmedValue = value.Trim('"');
                dataList.Add(trimmedValue);
            }

            foreach (var item in dataList.GetRange(1, dataList.Count - 3))
            {
                chapName += item;
            }

            identifier = dataList[0];
            if (aStatus == "En emision")
            {
                chapUri = dataList.GetRange(dataList.Count - 2, 1)[0];
            }
            else
            {
                chapUri = dataList.GetRange(dataList.Count - 1, 1)[0];
            }
        }

        return new string[] { identifier, chapUri, chapName };
    }

    private Chapter[] GetChaptersByregex(string text, string[] chapIdentifier)
    {
        var pattern = @"episodes = (\[\[.*\].*])";
        var chapters = new List<Chapter>();
        var match = Regex.Match(text, pattern);
        if (match.Success)
        {
            var innerArrays = match.Groups[1].Value.Split(new string[] { "],[" }, StringSplitOptions.None);
            var chaptherOrder = 0;
            foreach (var innerArray in innerArrays)
            {
                chaptherOrder++;
                var chapter = new Chapter
                {
                    Url = string.Concat("/ver/", chapIdentifier[1], "-", chaptherOrder),
                    ChapterNumber = chaptherOrder,
                    Name = string.Concat(chapIdentifier[2], " ", chaptherOrder),
                };

                chapters.Add(chapter);
            }
        }
        return chapters.ToArray();
    }

    private VideoSource[] getSorcesRegex(string text)
    {
        var pattern = @"var videos = \{""SUB"":(.*?)\};";
        var latPattern = @"var videos = \{""LAT"":(.*?)\};";
        var sources = new List<VideoSource>();
        var match = Regex.Match(text, pattern);
        var validator = false;

        var dubbed = "SUB";
        if (!match.Success)
        {
            match = Regex.Match(text, latPattern);
            validator = match.Success;
            dubbed = "LAT";
        }
        else
        {
            validator = true;
        }

        if (validator)
        {
            var prefab = match.Groups[0].Value.Replace("var videos =", "").Replace(";", "");
            var prefJson = JObject.Parse(prefab);

            foreach (var type in prefJson)
            {
                foreach (var vsource in prefJson[type.Key])
                {
                    var serverName = _serverConventions.GetServerName((string)vsource["server"]);

                    if (string.IsNullOrEmpty(serverName))
                    {
                        continue;
                    }

                    var vSouce = new VideoSource
                    {
                        Server = serverName,
                        Code = (string)vsource["code"],
                        Url = (string)vsource["url"],
                        Ads = (int)vsource["ads"],
                        Title = (string)vsource["title"],
                        Allow_mobile = (bool)vsource["allow_mobile"],
                    };
                    sources.Add(vSouce);
                }
            }
        }
        return sources.ToArray();
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var url = string.Concat(originUrl, requestUrl);
        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);
        var node = doc.DocumentNode.SelectSingleNode("/html/body");

        var sources = getSorcesRegex(node.InnerHtml);

        return sources;
    }

    private static AnimeType GetAnimeTypeByStr(string strType)
    {
        return strType switch
        {
            "OVA" => AnimeType.OVA,
            "Anime" => AnimeType.TV,
            "Película" => AnimeType.MOVIE,
            "Especial" => AnimeType.SPECIAL,
            _ => AnimeType.TV,
        };
    }

    public static string GenerateTagString(Tag[] tags)
    {
        var result = "";
        for (var i = 0; i < tags.Length; i++)
        {
            result += $"genre%5B%5D={tags[i].Value}";
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
            new() { Name = "Todo", Value = "all" },
            new() { Name = "Acción", Value = "accion" },
            new() { Name = "Artes Marciales", Value = "artes_marciales" },
            new() { Name = "Aventuras", Value = "aventura" },
            new() { Name = "Carreras", Value = "carreras" },
            new() { Name = "Ciencia Ficción", Value = "ciencia_ficcion" },
            new() { Name = "Comedia", Value = "comedia" },
            new() { Name = "Demencia", Value = "demencia" },
            new() { Name = "Demonios", Value = "demonios" },
            new() { Name = "Deportes", Value = "deportes" },
            new() { Name = "Drama", Value = "drama" },
            new() { Name = "Ecchi", Value = "ecchi" },
            new() { Name = "Escolares", Value = "escolares" },
            new() { Name = "Espacial", Value = "espacial" },
            new() { Name = "Fantasía", Value = "fantasia" },
            new() { Name = "Harem", Value = "harem" },
            new() { Name = "Historico", Value = "historico" },
            new() { Name = "Infantil", Value = "infantil" },
            new() { Name = "Josei", Value = "josei" },
            new() { Name = "Juegos", Value = "juegos" },
            new() { Name = "Magia", Value = "magia" },
            new() { Name = "Mecha", Value = "mecha" },
            new() { Name = "Militar", Value = "militar" },
            new() { Name = "Misterio", Value = "misterio" },
            new() { Name = "Música", Value = "musica" },
            new() { Name = "Parodia", Value = "parodia" },
            new() { Name = "Policía", Value = "policia" },
            new() { Name = "Psicológico", Value = "psicologico" },
            new() { Name = "Recuentos de la vida", Value = "recuentos_de_la_vida" },
            new() { Name = "Romance", Value = "romance" },
            new() { Name = "Samurai", Value = "samurai" },
            new() { Name = "Seinen", Value = "seinen" },
            new() { Name = "Shoujo", Value = "shoujo" },
            new() { Name = "Shounen", Value = "shounen" },
            new() { Name = "Sobrenatural", Value = "sobrenatural" },
            new() { Name = "Superpoderes", Value = "superpoderes" },
            new() { Name = "Suspenso", Value = "suspenso" },
            new() { Name = "Terror", Value = "terror" },
            new() { Name = "Vampiros", Value = "vampiros" },
            new() { Name = "Yaoi", Value = "yaoi" },
            new() { Name = "Yuri", Value = "yuri" },
        };
    }
}
