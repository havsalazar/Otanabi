using System.Globalization;
using System.Net;
using AngleSharp;
using Newtonsoft.Json;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.Extractors;
using Otanabi.Extensions.Models.Hentaila;

namespace Otanabi.Extensions.Extractors;

public class HentailaExtractor : IExtractor
{
    internal ServerConventions _serverConventions = new();
    internal readonly int extractorId = 7;
    internal readonly string sourceName = "HentaiLA";
    internal readonly string baseUrl = "https://www5.hentaila.com";
    internal readonly bool Persistent = true;
    internal readonly string Type = "ANIME";
    internal readonly bool IsTrackeable = true;
    internal readonly bool AllowNativeSearch = true;
    internal readonly bool IsAdult = true;

    private readonly IBrowsingContext _client;

    public HentailaExtractor()
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
            IsAdult = IsAdult,
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
            if (page > 1)
            {
                return animeList.ToArray();
            }
            url = $"{baseUrl}/api/search";
        }
        else if (tags != null && tags.Length > 0)
        {
            var genre = tags.FirstOrDefault()?.Value;
            url = $"{baseUrl}/genero/{genre}?p={page}";
        }
        else
        {
            url = $"{baseUrl}/directorio?filter=recent&p={page}";
        }

        var prov = (Provider)GenProvider();
        if (url.Contains("api/search"))
        {
            var _httpClient = new HttpClient();
            var formData = new Dictionary<string, string>
            {
                { "value", searchTerm },
            };

            var content = new FormUrlEncodedContent(formData);

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            var responseSearch = await _httpClient.SendAsync(request);
            var html = await responseSearch.Content.ReadAsStringAsync();
            var elements = JsonConvert.DeserializeObject<List<HentailaDto>>(html);
            foreach (var anime in elements)
            {
                animeList.Add(new()
                {
                    Title = anime.title,
                    Cover = $"{baseUrl}/uploads/portadas/{anime.id}.jpg",
                    Url = $"{baseUrl}/hentai-{anime.slug}",
                    Provider = prov,
                    ProviderId = prov.Id,
                    Type = AnimeType.OTHER
                });
            }
        }
        else
        {
            var doc = await _client.OpenAsync(url);
            foreach (var element in doc.QuerySelectorAll("div.columns main section.section div.grid.hentais article.hentai"))
            {
                animeList.Add(new()
                {
                    Title = element.QuerySelector("header.h-header h2")?.TextContent?.Trim(),
                    Cover = element.QuerySelector("div.h-thumb figure img").GetImageUrl(),
                    Url = element.QuerySelector("a").GetAbsoluteUrl("href"),
                    Provider = prov,
                    ProviderId = prov.Id,
                    Type = AnimeType.OTHER
                });
            }
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
            Title = doc.QuerySelector("article.hentai-single header.h-header h1")?.TextContent?.Trim(),
            Description = doc.QuerySelector("article.hentai-single div.h-content p")?.TextContent?.TrimAll(),
            Status = doc.QuerySelectorAll("article.hentai-single span.status-on").Length != 0 ? "En Emisión" : "Finalizado",
            GenreStr = string.Join(",", doc.QuerySelectorAll("article.hentai-single footer.h-footer nav.genres a.btn.sm").Select(x => WebUtility.HtmlDecode(x.TextContent?.Trim())).ToList()),
            RemoteID = requestUrl.Replace("/", ""),
            Cover = doc.QuerySelector("div.h-thumb figure img").GetImageUrl(),
            Type = AnimeType.OTHER,

            Chapters = await GetChapters(requestUrl)
        };
        return anime;
    }

    private async Task<List<Chapter>> GetChapters(string requestUrl)
    {
        var _httpClient = new HttpClient();
        var doc = await _client.OpenAsync(requestUrl);
        var animeId = doc.Url.SubstringAfter("hentai-").ToLower().TrimAll();
        var chapters = new List<Chapter>();
        foreach (var chapter in doc.QuerySelectorAll("div.episodes-list article"))
        {
            var numEp = chapter.QuerySelector("a")
                                .GetAbsoluteUrl("href")
                                .SubstringAfter($"/ver/{animeId}-")
                                .Replace($"/ver/{animeId}-", "");

            var date = chapter.QuerySelector(".h-header time")?.TextContent?.Trim();
            date = DateTime.TryParseExact(date, "MMMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd/MM/yyyy") : "";

            chapters.Add(new()
            {
                ChapterNumber = numEp.ToIntOrNull() ?? 0,
                Name = $"Episodio {numEp}",
                Url = $"{baseUrl}/ver/{animeId}-{numEp}",
            });
        }
        return chapters.OrderBy(x => x.ChapterNumber).ToList();
    }

    public async Task<IVideoSource[]> GetVideoSources(string requestUrl)
    {
        var sources = new List<VideoSource>();
        var doc = await _client.OpenAsync(requestUrl);
        var scriptElement = doc.Scripts.FirstOrDefault(s => s.TextContent.Contains("var videos = ["))?.TextContent;
        var videoServers = scriptElement.Split("videos = ")[1].Split(";")[0].Replace("[[", "").Replace("]]", "");
        var videoServerList = videoServers.Split("],[");
        foreach (var it in videoServerList)
        {
            var server = it.Split(',').Select(a => a.Replace("\"", "")).ToList();
            var urlServer = server[1].Replace("\\/", "/");
            var name = server[0];
            var serverName = _serverConventions.GetServerName(name);
            sources.Add(new()
            {
                Server = serverName,
                Title = serverName,
                Url = urlServer,
            });
        }
        return sources.Where(x => !string.IsNullOrEmpty(x.Server)).OrderByDescending(s => s.Server).ToArray();
    }

    public Tag[] GetTags()
    {
        return
        [
            new() { Name = "3D", Value = "3d" },
            new() { Name = "Ahegao", Value = "ahegao" },
            new() { Name = "Anal", Value = "anal" },
            new() { Name = "Casadas", Value = "casadas" },
            new() { Name = "Chikan", Value = "chikan" },
            new() { Name = "Ecchi", Value = "ecchi" },
            new() { Name = "Enfermeras", Value = "enfermeras" },
            new() { Name = "Futanari", Value = "futanari" },
            new() { Name = "Escolares", Value = "escolares" },
            new() { Name = "Gore", Value = "gore" },
            new() { Name = "Hardcore", Value = "hardcore" },
            new() { Name = "Harem", Value = "harem" },
            new() { Name = "Incesto", Value = "incesto" },
            new() { Name = "Juegos Sexuales", Value = "juegos-sexuales" },
            new() { Name = "Milfs", Value = "milfs" },
            new() { Name = "Maids", Value = "maids" },
            new() { Name = "Netorare", Value = "netorare" },
            new() { Name = "Ninfomania", Value = "ninfomania" },
            new() { Name = "Ninjas", Value = "ninjas" },
            new() { Name = "Orgias", Value = "orgias" },
            new() { Name = "Romance", Value = "romance" },
            new() { Name = "Shota", Value = "shota" },
            new() { Name = "Softcore", Value = "softcore" },
            new() { Name = "Succubus", Value = "succubus" },
            new() { Name = "Teacher", Value = "teacher" },
            new() { Name = "Tentaculos", Value = "tentaculos" },
            new() { Name = "Tetonas", Value = "tetonas" },
            new() { Name = "Vanilla", Value = "vanilla" },
            new() { Name = "Violacion", Value = "violacion" },
            new() { Name = "Virgenes", Value = "virgenes" },
            new() { Name = "Yaoi", Value = "yaoi" },
            new() { Name = "Yuri", Value = "yuri" },
            new() { Name = "Bondage", Value = "bondage" },
            new() { Name = "Elfas", Value = "elfas" },
            new() { Name = "Petit", Value = "petit" },
            new() { Name = "Threesome", Value = "threesome" },
            new() { Name = "Paizuri", Value = "paizuri" },
            new() { Name = "Gal", Value = "gal" },
            new() { Name = "Oyakodon", Value = "oyakodon" }
        ];
    }
}