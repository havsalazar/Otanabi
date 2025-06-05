using System.Globalization;
using System.Net;
using System.Web;
using AngleSharp;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;

namespace Otanabi.Extensions.Extractors;

public class MonoschinosExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 6;
    internal readonly string sourceName = "MonosChinos";
    internal readonly string baseUrl = "https://monoschinos2.net";
    internal readonly bool Persistent = true;
    internal readonly string Type = "ANIME";
    internal readonly bool IsTrackeable = true;
    internal readonly bool AllowNativeSearch = true;

    private readonly IBrowsingContext _client;

    public MonoschinosExtractor()
    {
        var config = Configuration.Default.WithDefaultLoader();
        _client = BrowsingContext.New(config);
    }

    public IProvider GenProvider() =>
        new Provider
        {
            Id = extractorId,
            Name = sourceName,
            Url = baseUrl,
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
            url = $"{baseUrl}/animes?buscar={HttpUtility.UrlEncode(searchTerm)}&pag={page}";
        }
        else if (tags != null && tags.Length > 0)
        {
            url = $"{baseUrl}/animes?genero={GenerateTagString(tags)}&pag={page}&orden=desc";
        }
        else
        {
            url = $"{baseUrl}/animes?estado=en+emision&pag={page}";
        }

        var doc = await _client.OpenAsync(url);
        var prov = (Provider)GenProvider();
        foreach (var element in doc.QuerySelectorAll(".ficha_efecto a"))
        {
            animeList.Add(new()
            {
                Title = element.QuerySelector(".title_cap")?.TextContent?.Trim(),
                Cover = element.QuerySelector("img").GetImageUrl(),
                Url = element.GetAbsoluteUrl("href"),
                Provider = prov,
                ProviderId = prov.Id,
                Type = GetAnimeTypeByStr(element.QuerySelector("span.text-muted")?.TextContent?.Trim()),
            });
        }
        return animeList.ToArray();
    }

    public async Task<IAnime> GetAnimeDetailsAsync(string requestUrl)
    {
        var doc = await _client.OpenAsync(requestUrl);
        if (doc.StatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Anime could not be found");
        }

        var prov = (Provider)GenProvider();
        Anime anime = new()
        {
            Provider = prov,
            ProviderId = prov.Id,
            Url = requestUrl,
            Title = doc.QuerySelector(".flex-column h1.text-capitalize")?.TextContent?.Trim(),
            Description = doc.QuerySelector(".h-100 .mb-3 p")?.TextContent?.TrimAll(),
            Status = doc.QuerySelector(".d-flex .ms-2 div:not([class])")?.TextContent?.Trim(),
            GenreStr = string.Join(",", doc.QuerySelectorAll(".lh-lg span").Select(x => WebUtility.HtmlDecode(x.TextContent?.Trim())).ToList()),
            RemoteID = requestUrl.Replace("/", ""),
            Cover = doc.QuerySelector(".gap-3 img").GetImageUrl(),
            Type = GetAnimeTypeByStr(doc.QuerySelector(".bg-transparent dl dd")?.TextContent?.Trim()),

            Chapters = await GetChapters(requestUrl)
        };
        return anime;
    }

    private async Task<List<Chapter>> GetChapters(string requestUrl)
    {
        var _httpClient = new HttpClient();
        var html = await _httpClient.GetStringAsync(requestUrl);
        var doc = await _client.OpenAsync(req => req.Content(html).Address(requestUrl));

        var referer = doc.Url;
        var dt = doc.QuerySelector("#dt");

        var total = int.Parse(dt.GetAttribute("data-e") ?? "0");
        var perPage = 50.0;
        var pages = (int)Math.Ceiling(total / perPage);

        var i = dt.GetAttribute("data-i");
        var u = dt.GetAttribute("data-u");
        var chapters = new List<Chapter>();
        for (var page = 1; page <= pages; page++)
        {
            var formData = new Dictionary<string, string>
            {
                { "acc", "episodes" },
                { "i", i },
                { "u", u },
                { "p", page.ToString() }
            };

            var content = new FormUrlEncodedContent(formData);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/ajax_pagination")
            {
                Content = content
            };

            request.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
            request.Headers.Add("accept-language", "es-419,es;q=0.8");
            request.Headers.Referrer = new Uri(referer);
            request.Headers.Add("origin", baseUrl);
            request.Headers.Add("x-requested-with", "XMLHttpRequest");

            var response = await _httpClient.SendAsync(request);

            var pageChapters = await GetChamperInfo(response);

            chapters.AddRange(pageChapters);
        }

        return chapters.OrderBy(x => x.ChapterNumber).ToList();
    }

    public async Task<List<Chapter>> GetChamperInfo(HttpResponseMessage document)
    {
        var html = await document.Content.ReadAsStringAsync();
        var doc = await _client.OpenAsync(req => req.Content(html));
        var champers = new List<Chapter>();
        var nodes = doc.QuerySelectorAll(".ko");
        var idx = 0;
        foreach (var node in nodes)
        {
            int chapterNumber;
            try
            {
                var titleText = node.QuerySelector("h2")?.TextContent ?? "";
                var numberPart = titleText.Split("Capítulo")[1].Trim();
                chapterNumber = int.Parse(numberPart, CultureInfo.InvariantCulture);
            }
            catch
            {
                chapterNumber = idx + 1;
            }
            var name = node.QuerySelector(".fs-6")?.TextContent ?? "";
            var url = node.GetAbsoluteUrl("href") ?? "";
            var episode = new Chapter()
            {
                ChapterNumber = chapterNumber,
                Name = name.Trim(),
                Url = url
            };
            champers.Add(episode);
            idx++;
        }

        return champers;
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var _httpClient = new HttpClient();
        var sources = new List<VideoSource>();

        var html = await _httpClient.GetStringAsync(requestUrl);
        var doc = await _client.OpenAsync(req => req.Content(html).Address(requestUrl));
        var i = doc.QuerySelector(".opt").GetAttribute("data-encrypt");
        var referer = doc.Url;

        var formData = new Dictionary<string, string>
            {
                { "acc", "opt" },
                { "i", i }
            };

        var content = new FormUrlEncodedContent(formData);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/ajax_pagination")
        {
            Content = content
        };

        request.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        request.Headers.Add("accept-language", "es-419,es;q=0.8");
        request.Headers.Referrer = new Uri(referer);
        request.Headers.Add("origin", baseUrl);
        request.Headers.Add("x-requested-with", "XMLHttpRequest");

        var response = await _httpClient.SendAsync(request);

        var videoSources = await GetVideoSourceInfo(response);

        sources.AddRange(videoSources);

        return sources.ToArray();
    }

    private async Task<List<VideoSource>> GetVideoSourceInfo(HttpResponseMessage document)
    {
        var html = await document.Content.ReadAsStringAsync();
        var doc = await _client.OpenAsync(req => req.Content(html));
        var sources = new List<VideoSource>();
        foreach (var server in doc.QuerySelectorAll("[data-player]"))
        {
            var url = server.GetAttribute("data-player").DecodeBase64();
            var name = server.TextContent.Trim();
            var serverName = _serverConventions.GetServerName(name);

            sources.Add(new()
            {
                Server = serverName,
                Title = serverName,
                Url = url,
            });
        }
        return sources.Where(x => !string.IsNullOrEmpty(x.Server)).OrderByDescending(s => s.Server).ToList();
    }

    public static string GenerateTagString(Tag[] tags)
    {
        return string.Join(",", tags.Select(t => t.Value));
    }

    public Tag[] GetTags()
    {
        return
        [
            new() { Name = "Acción", Value = "accion" },
            new() { Name = "Aenime", Value = "aenime" },
            new() { Name = "Anime Latino", Value = "anime-latino" },
            new() { Name = "Artes Marciales", Value = "artes-marciales" },
            new() { Name = "Aventura", Value = "aventura" },
            new() { Name = "Aventuras", Value = "aventuras" },
            new() { Name = "Blu-ray", Value = "blu-ray" },
            new() { Name = "Carreras", Value = "carreras" },
            new() { Name = "Castellano", Value = "castellano" },
            new() { Name = "Ciencia Ficción", Value = "ciencia-ficcion" },
            new() { Name = "Comedia", Value = "comedia" },
            new() { Name = "Cyberpunk", Value = "cyberpunk" },
            new() { Name = "Demencia", Value = "demencia" },
            new() { Name = "Dementia", Value = "dementia" },
            new() { Name = "Demonios", Value = "demonios" },
            new() { Name = "Deportes", Value = "deportes" },
            new() { Name = "Drama", Value = "drama" },
            new() { Name = "Ecchi", Value = "ecchi" },
            new() { Name = "Escolares", Value = "escolares" },
            new() { Name = "Espacial", Value = "espacial" },
            new() { Name = "Fantasía", Value = "fantasia" },
            new() { Name = "Gore", Value = "gore" },
            new() { Name = "Harem", Value = "harem" },
            new() { Name = "Historia paralela", Value = "historia-paralela" },
            new() { Name = "Historico", Value = "historico" },
            new() { Name = "Horror", Value = "horror" },
            new() { Name = "Infantil", Value = "infantil" },
            new() { Name = "Josei", Value = "josei" },
            new() { Name = "Juegos", Value = "juegos" },
            new() { Name = "Latino", Value = "latino" },
            new() { Name = "Lucha", Value = "lucha" },
            new() { Name = "Magia", Value = "magia" },
            new() { Name = "Mecha", Value = "mecha" },
            new() { Name = "Militar", Value = "militar" },
            new() { Name = "Misterio", Value = "misterio" },
            new() { Name = "Monogatari", Value = "monogatari" },
            new() { Name = "Música", Value = "musica" },
            new() { Name = "Parodia", Value = "parodia" },
            new() { Name = "Parodias", Value = "parodias" },
            new() { Name = "Policía", Value = "policia" },
            new() { Name = "Psicológico", Value = "psicologico" },
            new() { Name = "Recuentos de la vida", Value = "recuentos-de-la-vida" },
            new() { Name = "Recuerdos de la vida", Value = "recuerdos-de-la-vida" },
            new() { Name = "Romance", Value = "romance" },
            new() { Name = "Samurai", Value = "samurai" },
            new() { Name = "Seinen", Value = "seinen" },
            new() { Name = "Shojo", Value = "shojo" },
            new() { Name = "Shonen", Value = "shonen" },
            new() { Name = "Shoujo", Value = "shoujo" },
            new() { Name = "Shounen", Value = "shounen" },
            new() { Name = "Sobrenatural", Value = "sobrenatural" },
            new() { Name = "Superpoderes", Value = "superpoderes" },
            new() { Name = "Suspenso", Value = "suspenso" },
            new() { Name = "Terror", Value = "terror" },
            new() { Name = "Vampiros", Value = "vampiros" },
            new() { Name = "Yaoi", Value = "yaoi" },
            new() { Name = "Yuri", Value = "yuri" },
        ];
    }

    #region Private methods

    private static AnimeType GetAnimeTypeByStr(string strType)
    {
        return strType switch
        {
            "OVA" => AnimeType.OVA,
            "Anime" or "anime" or "doramas" or "serie" => AnimeType.TV,
            "Película" or "pelicula" => AnimeType.MOVIE,
            "Especial" => AnimeType.SPECIAL,
            _ => AnimeType.TV,
        };
    }

    #endregion
}