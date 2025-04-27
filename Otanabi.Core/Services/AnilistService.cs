using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using F23.StringSimilarity;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Anilist;
using Otanabi.Core.Anilist.Enums;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Models;

namespace Otanabi.Core.Services;

public sealed class AnilistService
{
    private AnilistClient _client = AnilistClient.Instance;
    private Levenshtein _levenshtein = new();

    public AnilistService() { }

    public async Task<List<string>> GetTagsASync()
    {
        var query = _client.GetQuery(QueryType.GetTags);

        var response = await _client.SendQueryAsync(query);

        var data = response["data"];

        List<string> temp = new List<string>();

        foreach (var el in data["genres"])
        {
            temp.Add((string)el);
        }
        foreach (var el in data["tags"])
        {
            if ((bool)el["isAdult"] == false)
            {
                temp.Add((string)el["name"]);
            }
        }

        return temp;
    }

    public async Task<(Media[], PageInfo)> GetSeasonal(
        int page = 1,
        MediaSeason season = MediaSeason.Fall,
        int seasonYear = 2024,
        MediaStatus? status = MediaStatus.Finished,
        bool isAdult = false,
        MediaType type = MediaType.Anime,
        MediaFormat format = MediaFormat.Tv,
        MediaSort[] sortFilter = null
    )
    {
        sortFilter ??= new MediaSort[] { MediaSort.Popularity_Desc };
        var query = _client.GetQuery(QueryType.Seasonal);
        var variables = new Dictionary<string, object>
        {
            { "perPage", 100 },
            { "page", 1 },
            { "year", seasonYear },
            { "season", season.ToString().ToUpper() },
            { "type", type.ToString().ToUpper() },
            { "format", format.ToString().ToUpper() },
            //{ "sort", sortFilter.Select(x => x.ToString().ToUpper()).ToArray() },
        };
        var response = await _client.SendQueryAsync(query, variables);
        var data = response["data"]["Page"];
        var pdata = data["pageInfo"];
        var pageinfo = new PageInfo { HasNextPage = (bool)pdata["hasNextPage"], Total = (int)pdata["total"] };

        var medias = new List<Media>();
        foreach (var media in data["media"])
        {
            try
            {
                var mediaItem = new Media
                {
                    Id = (int)media["id"],
                    Title = new MediaTitle
                    {
                        English = (string)media["title"]["english"],
                        Romaji = (string)media["title"]["romaji"],
                        Native = (string)media["title"]["native"],
                    },
                    BannerImage = (string)media["bannerImage"],
                    CoverImage = new MediaCoverImage
                    {
                        Color = (string)media["coverImage"]["color"],
                        ExtraLarge = (string)media["coverImage"]["extraLarge"],
                    },
                    Description = RemoveHtmlTags((string)media["description"]),
                    Genres = media["genres"].Select(x => (string)x).ToArray(),
                    Popularity = (int?)media["popularity"] ?? 0,
                    AverageScore = (int?)media["averageScore"] ?? 0,
                    Status = ConvertToMediastatus((string)media["status"]),
                    Format = ConvertToMediaFormat((string)media["format"]),
                    StartDate = new FuzzyDate
                    {
                        Day = (int?)media["startDate"]["day"] ?? 0,
                        Month = (int?)media["startDate"]["month"] ?? 0,
                        Year = (int?)media["startDate"]["year"] ?? 0,
                    },
                    EndDate = new FuzzyDate
                    {
                        Day = (int?)media["endDate"]["day"] ?? 0,
                        Month = (int?)media["endDate"]["month"] ?? 0,
                        Year = (int?)media["endDate"]["year"] ?? 0,
                    },
                    Duration = (int?)media["duration"] ?? 0,
                    Episodes = (int?)media["episodes"] ?? 0,
                    Season = ConvertToMediaSeason((string)media["season"]),
                    SeasonYear = (int?)media["seasonYear"] ?? 0,
                };
                medias.Add(mediaItem);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return (medias.ToArray(), pageinfo);
    }

    public async Task<(Media[], PageInfo)> SearchMedia(
        int page,
        string searchTerm,
        int? year,
        MediaSeason? season,
        MediaStatus? status,
        MediaFormat[] formats = null,
        string[] genres = null,
        MediaSort[] sortFilter = null,
        bool isAdult = false,
        int perPage = 30,
        MediaType type = MediaType.Anime
    )
    {
        sortFilter ??= new MediaSort[] { MediaSort.Popularity_Desc };
        var query = _client.GetQuery(QueryType.Search);
        var variables = new Dictionary<string, object>
        {
            { "page", page },
            { "perPage", perPage },
            { "type", type.ToString().ToUpper() },
        };

        if (string.IsNullOrEmpty(searchTerm))
        {
            sortFilter ??= new[] { MediaSort.Trending_Desc };
        }
        else
        {
            variables["search"] = searchTerm;
        }

        // Add optional parameters conditionally
        AddIfNotNull(variables, "year", year);
        AddIfNotNull(variables, "season", season?.ToString().ToUpper());
        AddIfNotNull(variables, "status", status?.ToString().ToUpper());
        AddIfNotNull(variables, "format", formats?.Any() == true ? arrayToQuery(formats) : null);
        AddIfNotNull(variables, "genres", genres?.Any() == true ? genres : null);
        AddIfNotNull(variables, "sort", sortFilter?.Any() == true ? arrayToQuery(sortFilter) : null);

        var response = await _client.SendQueryAsync(query, variables);
        var data = response["data"]["Page"];
        var pdata = data["pageInfo"];
        var pageinfo = new PageInfo { HasNextPage = (bool)pdata["hasNextPage"], Total = (int)pdata["total"] };

        var medias = new List<Media>();
        foreach (var media in data["media"])
        {
            try
            {
                var mediaItem = new Media
                {
                    Id = (int)media["id"],
                    Title = new MediaTitle
                    {
                        English = (string)media["title"]["english"],
                        Romaji = (string)media["title"]["romaji"],
                        Native = (string)media["title"]["native"],
                    },
                    BannerImage = (string)media["bannerImage"],
                    CoverImage = new MediaCoverImage
                    {
                        Color = (string)media["coverImage"]["color"],
                        ExtraLarge = (string)media["coverImage"]["extraLarge"],
                    },
                    Description = RemoveHtmlTags((string)media["description"]),
                    Genres = media["genres"].Select(x => (string)x).ToArray(),
                    Popularity = (int?)media["popularity"] ?? 0,
                    AverageScore = (int?)media["averageScore"] ?? 0,
                    Status = ConvertToMediastatus((string)media["status"]),
                    Format = ConvertToMediaFormat((string)media["format"]),
                    StartDate = new FuzzyDate
                    {
                        Day = (int?)media["startDate"]["day"] ?? 0,
                        Month = (int?)media["startDate"]["month"] ?? 0,
                        Year = (int?)media["startDate"]["year"] ?? 0,
                    },
                    EndDate = new FuzzyDate
                    {
                        Day = (int?)media["endDate"]["day"] ?? 0,
                        Month = (int?)media["endDate"]["month"] ?? 0,
                        Year = (int?)media["endDate"]["year"] ?? 0,
                    },
                    Duration = (int?)media["duration"] ?? 0,
                    Episodes = (int?)media["episodes"] ?? 0,
                    Season = ConvertToMediaSeason((string)media["season"]),
                    SeasonYear = (int?)media["seasonYear"] ?? 0,
                };
                medias.Add(mediaItem);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw;
            }
        }

        return (medias.ToArray(), pageinfo);
    }

    public async Task<Media> GetMediaByIdAsync(int id)
    {
        var variables = new Dictionary<string, object> { { "id", id } };

        var query = _client.GetQuery(QueryType.ById);

        var response = await _client.SendQueryAsync(query, variables);

        var data = response["data"]["Media"];

        var anime = new Media
        {
            Id = id,
            Title = new MediaTitle
            {
                English = (string)data["title"]["english"],
                Romaji = (string)data["title"]["romaji"],
                Native = (string)data["title"]["native"],
            },
            BannerImage = (string)data["bannerImage"],
            CoverImage = new MediaCoverImage
            {
                Color = (string)data["coverImage"]["color"],
                ExtraLarge = (string)data["coverImage"]["extraLarge"],
            },
            Description = RemoveHtmlTags((string)data["description"]),
            Genres = data["genres"].Select(x => (string)x).ToArray(),
            MeanScore = (int?)data["meanScore"] ?? 0,
            Popularity = (int?)data["popularity"] ?? 0,
            AverageScore = (int?)data["averageScore"] ?? 0,
            Status = ConvertToMediastatus((string)data["status"]),
            Format = ConvertToMediaFormat((string)data["format"]),
            StartDate = new FuzzyDate
            {
                Day = (int?)data["startDate"]["day"] ?? 0,
                Month = (int?)data["startDate"]["month"] ?? 0,
                Year = (int?)data["startDate"]["year"] ?? 0,
            },
            EndDate = new FuzzyDate
            {
                Day = (int?)data["endDate"]["day"] ?? 0,
                Month = (int?)data["endDate"]["month"] ?? 0,
                Year = (int?)data["endDate"]["year"] ?? 0,
            },
            Duration = (int?)data["duration"] ?? 0,
            Episodes = (int?)data["episodes"] ?? 0,
            Season = ConvertToMediaSeason((string)data["season"]),
            SeasonYear = (int?)data["seasonYear"] ?? 0,
        };

        try
        {
            anime.NextAiringEpisode = new AiringSchedule
            {
                Episode = (int?)data["nextAiringEpisode"]["episode"] ?? 0,
                TimeUntilAiring = (int?)data["nextAiringEpisode"]["timeUntilAiring"] ?? 0,
                AiringAt = (int?)data["nextAiringEpisode"]["airingAt"] ?? 0,
            };
        }
        catch (Exception) { }

        List<MediaStreamingEpisode> episodes = [];
        List<MediaStreamingEpisode> StramingEpisodes = [];
        List<MediaStreamingEpisode> TmpEpisodes = [];
        if (anime.Status != MediaStatus.NotYetReleased)
        {
            var episodeLimiter = (int?)data["episodes"] ?? 0;

            if (anime.Status == MediaStatus.Releasing)
            {
                episodeLimiter = (int)data["nextAiringEpisode"]?["episode"] - 1;
            }
            for (int i = 0; i < episodeLimiter; i++)
            {
                var episode = new MediaStreamingEpisode
                {
                    Site = "",
                    Thumbnail = "//Assets/OtanabiSplash.png",
                    Title = $"Episode {i + 1}",
                    Number = i + 1,
                };
                TmpEpisodes.Add(episode);
            }

            foreach (var item in data["streamingEpisodes"])
            {
                var episode = new MediaStreamingEpisode
                {
                    Site = "",
                    Thumbnail = (string)item["thumbnail"],
                    Title = $"Episode {GetEpisodeNumber((string)item["title"])}",
                    Number = GetEpisodeNumber((string)item["title"]) ?? 1,
                };
                StramingEpisodes.Add(episode);
            }

            var episodesToRemove = new HashSet<int>(StramingEpisodes.Select(x => x.Number));
            TmpEpisodes = TmpEpisodes.Where(x => !episodesToRemove.Contains(x.Number)).ToList();

            episodes = TmpEpisodes.Union(StramingEpisodes).ToList();
            episodes = episodes.OrderByDescending(x => x.Number).ToList();
        }
        anime.StreamingEpisodes = episodes;

        return anime;
    }

    public async Task<Media> SearchByName(string titleName, bool isAdult, List<string> alternateTitles)
    {
        var variables = new Dictionary<string, object>
        {
            { "search", titleName.NormalizeSTR() },
            { "type", "ANIME" },
            { "page", 1 },
            { "perpage", 20 },
            { "isAdult", isAdult },
        };

        var query = _client.GetQuery(QueryType.ByName);

        var response = await _client.SendQueryAsync(query, variables);

        var medias = new List<Media>();

        var data = response["data"]["Page"];
        var pdata = data["pageInfo"];

        foreach (var media in data["media"])
        {
            try
            {
                var mediaItem = new Media()
                {
                    Id = (int)media["id"],
                    Title = new MediaTitle
                    {
                        English = (string)media["title"]["english"],
                        Romaji = (string)media["title"]["romaji"],
                        Native = (string)media["title"]["native"],
                    },
                    CoverImage = new MediaCoverImage
                    {
                        Color = (string)media["coverImage"]["color"],
                        ExtraLarge = (string)media["coverImage"]["extraLarge"],
                    },
                };

                medias.Add(mediaItem);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw;
            }
        }
        //_levenshtein.Distance(y.NormalizeSTR(), anime.Title.NormalizeSTR()) < 3)

        //var selectedMedia = medias.FirstOrDefault(x =>
        //    string.Equals(x.Title.Romaji, titleName, StringComparison.OrdinalIgnoreCase)
        //    || string.Equals(x.Title.Native, titleName, StringComparison.OrdinalIgnoreCase)
        //    || string.Equals(x.Title.English, titleName, StringComparison.OrdinalIgnoreCase)
        //);
        var selectedMedia = new Media();
        if (alternateTitles.Count > 0)
        {
            selectedMedia = medias.FirstOrDefault(x =>
                _levenshtein.Distance(x.Title.Romaji.NormalizeSTR(), titleName.NormalizeSTR()) < 3
                || _levenshtein.Distance(x.Title.Native.NormalizeSTR(), titleName.NormalizeSTR()) < 3
                || _levenshtein.Distance(x.Title.English.NormalizeSTR(), titleName.NormalizeSTR()) < 3
                || alternateTitles.Any(y => _levenshtein.Distance(x.Title.Romaji.NormalizeSTR(), y.NormalizeSTR()) < 3)
                || alternateTitles.Any(y => _levenshtein.Distance(x.Title.Native.NormalizeSTR(), y.NormalizeSTR()) < 3)
                || alternateTitles.Any(y => _levenshtein.Distance(x.Title.English.NormalizeSTR(), y.NormalizeSTR()) < 3)
            );
        }
        else
        {
            selectedMedia = medias.FirstOrDefault(x =>
                _levenshtein.Distance(x.Title.Romaji.NormalizeSTR(), titleName.NormalizeSTR()) < 3
                || _levenshtein.Distance(x.Title.Native.NormalizeSTR(), titleName.NormalizeSTR()) < 3
                || _levenshtein.Distance(x.Title.English.NormalizeSTR(), titleName.NormalizeSTR()) < 3
            );
        }

        return selectedMedia;
    }

    private static string RemoveHtmlTags(string html)
    {
        return string.IsNullOrEmpty(html) ? html : Regex.Replace(html, @"<.*?>", string.Empty);
    }

    private static int? GetEpisodeNumber(string text)
    {
        try
        {
            Match match = Regex.Match(text, @"Episode (\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static MediaStatus ConvertToMediastatus(string param)
    {
        return param switch
        {
            "FINISHED" => MediaStatus.Finished,
            "RELEASING" => MediaStatus.Releasing,
            "NOT_YET_RELEASED" => MediaStatus.NotYetReleased,
            "CANCELLED" => MediaStatus.Cancelled,
            _ => MediaStatus.Finished,
        };
    }

    private static MediaFormat ConvertToMediaFormat(string param)
    {
        return param switch
        {
            "TV" => MediaFormat.Tv,
            "TV_SHORT" => MediaFormat.TvShort,
            "MOVIE" => MediaFormat.Movie,
            "SPECIAL" => MediaFormat.Special,
            "OVA" => MediaFormat.Ova,
            "ONA" => MediaFormat.Ona,
            "MUSIC" => MediaFormat.Music,
            _ => MediaFormat.Tv,
        };
    }

    private static MediaSeason ConvertToMediaSeason(string param)
    {
        return param switch
        {
            "WINTER" => MediaSeason.Winter,
            "SPRING" => MediaSeason.Spring,
            "SUMMER" => MediaSeason.Summer,
            "FALL" => MediaSeason.Fall,
            _ => MediaSeason.Winter,
        };
    }

    private List<string> arrayToQuery(MediaFormat[] formats)
    {
        return arrayToQuery(formats.Select(x => x.ToString().ToUpper()).ToArray());
    }

    private List<string> arrayToQuery(MediaSort[] sort)
    {
        return arrayToQuery(sort.Select(x => x.ToString().ToUpper()).ToArray());
    }

    private List<string> arrayToQuery(string[] arr)
    {
        var test = arr.Select(x => x.ToString().ToUpper()).ToArray();

        return test.ToList();
    }

    private void AddIfNotNull(Dictionary<string, object> variables, string key, object value)
    {
        if (value != null)
        {
            variables[key] = value;
        }
    }
}
