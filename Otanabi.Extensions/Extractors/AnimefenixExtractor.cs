using System.Net;
using System.Web;
using HtmlAgilityPack;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;
using ScrapySharp.Extensions;

namespace Otanabi.Extensions.Extractors;

public class AnimefenixExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 5;
    internal readonly string sourceName = "AnimeFenix";
    internal readonly string originUrl = "https://animefenix2.tv";
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

        string url;
        if (!string.IsNullOrEmpty(searchTerm))
        {
            url = $"{originUrl}/directorio/anime?p={page}&q={HttpUtility.UrlEncode(searchTerm)}";
        }
        else if (tags != null && tags.Length > 0)
        {
            var genre = tags.FirstOrDefault()?.Value;
            url = $"{originUrl}/directorio/anime?p={page}&genero={genre}";
        }
        else
        {
            url = $"{originUrl}/directorio/anime?p={page}&estado=2";
        }

        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);

        foreach (var nodo in doc.DocumentNode.CssSelect(".grid-animes li article a"))
        {
            Anime anime = new();
            anime.Title = nodo.SelectNodes("//p[not(contains(@class, 'gray'))]").FirstOrDefault()?.InnerText;
            anime.Cover = nodo.CssSelect(".main-img img").FirstOrDefault()?.GetAttributeValue("src");
            anime.Url = nodo.GetAbsoluteUrl("href", originUrl);
            anime.Provider = (Provider)GenProvider();
            anime.ProviderId = anime.Provider.Id;
            var type = nodo.CssSelect(".tipo").FirstOrDefault()?.InnerText?.Trim();
            anime.Type = GetAnimeTypeByStr(type.ToLower());
            animeList.Add(anime);
        }
        return animeList.ToArray();
    }

    public async Task<IAnime> GetAnimeDetailsAsync(string requestUrl)
    {
        var url = string.Concat(requestUrl);
        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(url);

        if (oWeb.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Anime could not be found");
        }

        Anime anime = new();
        var node = doc.DocumentNode.SelectSingleNode("/html/body");
        anime.Url = requestUrl;
        anime.Title = node.CssSelect("h1.text-4xl")?.FirstOrDefault()?.InnerText?.TrimAll();
        anime.Description = node.CssSelect(".mb-6 p.text-gray-300")?.FirstOrDefault()?.InnerText?.TrimAll();
        anime.Cover = node.CssSelect("#blur_img")?.FirstOrDefault()?.GetImageUrl(originUrl);
        anime.Cover ??= node.CssSelect("#anime_image")?.FirstOrDefault()?.GetImageUrl(originUrl);
        anime.Provider = (Provider)GenProvider();
        anime.ProviderId = anime.Provider.Id;
        anime.Status = node.CssSelect(".relative .rounded")?.FirstOrDefault()?.InnerText?.Trim();
        var genres = node.CssSelect(".flex-wrap a").Select(x => WebUtility.HtmlDecode(x.InnerText?.Trim())).ToList();
        anime.GenreStr = string.Join(",", genres);
        anime.RemoteID = requestUrl.Replace("/", "");
        anime.Type = GetAnimeTypeByStr(node.CssSelect(".text-gray-300 .mb-2")?.FirstOrDefault()?.InnerText?.ToLower()?.SubstringAfter("tipo:")?.Trim());
        anime.Chapters = [];
        foreach (var item in node.CssSelect(".divide-y li > a").Reverse())
        {
            var title = item.CssSelect(".font-semibold")?.FirstOrDefault()?.InnerText?.TrimAll();
            anime.Chapters.Add(new Chapter()
            {
                Url = item.GetAbsoluteUrl("href", originUrl),
                ChapterNumber = title.SubstringAfter("Episodio").ToIntOrNull() ?? 0,
                Name = title,
            });
        }
        return anime;
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var oWeb = new HtmlWeb();
        var doc = await oWeb.LoadFromWebAsync(requestUrl);

        if (oWeb.StatusCode != HttpStatusCode.OK)
            throw new Exception("Page could not be loaded");

        var sources = new List<VideoSource>();

        var scriptNode = doc.DocumentNode
            .SelectNodes("//script")
            ?.FirstOrDefault(s => s.InnerText.Contains("var tabsArray"));

        if (scriptNode == null)
        {
            return [];
        }

        var data = scriptNode.InnerText;

        // Extraer iframes y obtener el parámetro "id" del src
        var urls = data
            .Split(["<iframe"], StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // el primero no tiene iframe
            .Select(part => part.Split(["src='"], StringSplitOptions.None).ElementAtOrDefault(1))
            .Where(src => src != null)
            .Select(src => src.Split('\'')[0]) // cortar en la primera comilla simple
            .Select(src => src.Split(["redirect.php?id="], StringSplitOptions.None).LastOrDefault()?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        foreach (var url in urls.Order())
        {
            var serverName = _serverConventions.GetServerName(GetDomainName(url));
            sources.Add(new VideoSource()
            {
                Server = serverName,
                Title = serverName,
                Url = url,
            });
        }

        return sources.Where(x => !string.IsNullOrEmpty(x.Server)).OrderByDescending(s => s.Server).ToArray();
    }

    public Tag[] GetTags()
    {
        return
        [
            new() { Name = "Acción", Value = "1" },
            new() { Name = "Escolares", Value = "2" },
            new() { Name = "Romance", Value = "3" },
            new() { Name = "Shoujo", Value = "4" },
            new() { Name = "Comedia", Value = "5" },
            new() { Name = "Drama", Value = "6" },
            new() { Name = "Seinen", Value = "7" },
            new() { Name = "Deportes", Value = "8" },
            new() { Name = "Shounen", Value = "9" },
            new() { Name = "Recuentos de la vida", Value = "10" },
            new() { Name = "Ecchi", Value = "11" },
            new() { Name = "Sobrenatural", Value = "12" },
            new() { Name = "Fantasía", Value = "13" },
            new() { Name = "Magia", Value = "14" },
            new() { Name = "Superpoderes", Value = "15" },
            new() { Name = "Demencia", Value = "16" },
            new() { Name = "Misterio", Value = "17" },
            new() { Name = "Psicológico", Value = "18" },
            new() { Name = "Suspenso", Value = "19" },
            new() { Name = "Ciencia Ficción", Value = "20" },
            new() { Name = "Mecha", Value = "21" },
            new() { Name = "Militar", Value = "22" },
            new() { Name = "Aventuras", Value = "23" },
            new() { Name = "Historico", Value = "24" },
            new() { Name = "Infantil", Value = "25" },
            new() { Name = "Artes Marciales", Value = "26" },
            new() { Name = "Terror", Value = "27" },
            new() { Name = "Harem", Value = "28" },
            new() { Name = "Josei", Value = "29" },
            new() { Name = "Parodia", Value = "30" },
            new() { Name = "Policía", Value = "31" },
            new() { Name = "Juegos", Value = "32" },
            new() { Name = "Carreras", Value = "33" },
            new() { Name = "Samurai", Value = "34" },
            new() { Name = "Espacial", Value = "35" },
            new() { Name = "Música", Value = "36" },
            new() { Name = "Yuri", Value = "37" },
            new() { Name = "Demonios", Value = "38" },
            new() { Name = "Vampiros", Value = "39" },
            new() { Name = "Yaoi", Value = "40" },
            new() { Name = "Humor Negro", Value = "41" },
            new() { Name = "Crimen", Value = "42" },
            new() { Name = "Hentai", Value = "43" },
            new() { Name = "Youtuber", Value = "44" },
            new() { Name = "MaiNess Random", Value = "45" },
            new() { Name = "Donghua", Value = "46" },
            new() { Name = "Horror", Value = "47" },
            new() { Name = "Sin Censura", Value = "48" },
            new() { Name = "Gore", Value = "49" },
            new() { Name = "Live Action", Value = "50" },
            new() { Name = "Isekai", Value = "51" },
            new() { Name = "Gourmet", Value = "52" },
            new() { Name = "spokon", Value = "53" },
            new() { Name = "Zombies", Value = "54" },
            new() { Name = "Idols", Value = "55" },
        ];
    }

    #region Private methods

    private static AnimeType GetAnimeTypeByStr(string strType)
    {
        return strType switch
        {
            "ova" => AnimeType.OVA,
            "anime" or "doramas" or "serie" or "manhwa" or "live action" or "tv" => AnimeType.TV,
            "película" or "pelicula" => AnimeType.MOVIE,
            "especial" => AnimeType.SPECIAL,
            _ => AnimeType.OTHER,
        };
    }

    public static string GetDomainName(string url)
    {
        try
        {
            var uri = new UriBuilder(url).Uri;
            var host = uri.Host;
            var parts = host.Split('.');
            if (parts.Length >= 2)
            {
                return parts[^2];
            }
            return host;
        }
        catch
        {
            return "invalid";
        }
    }

    #endregion
}