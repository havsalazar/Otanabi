using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using F23.StringSimilarity;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Models;

namespace Otanabi.Core.Services;

//this class will connect anilist with the providers
public sealed class SearchEngineService
{
    private static Levenshtein _levenshtein = new();

    //search the anime title
    //will return a list of possibile anime/s


    public static async Task<(Anime, List<Anime>)> SearchByName(MediaTitle searchTerm, Provider provider)
    {
        var animeService = new SearchAnimeService();

        var searchQuery = provider.AllowNativeSearch ? searchTerm.Native : searchTerm.Romaji;

        var data = await animeService.SearchAnimeAsync(searchQuery, 1, provider);
        var searchTerms = new[] { searchTerm.Romaji, searchTerm.English, searchTerm.Native };

        //var result = data.Where(anime => searchTerms.Any(y => Normalize(y) == Normalize(anime.Title))).ToList().FirstOrDefault();

        var result = data.Where(anime =>
                searchTerms.Any(y => _levenshtein.Distance(y.NormalizeSTR(), anime.Title.NormalizeSTR()) < 3)
            )
            .ToList()
            .FirstOrDefault();

        var fullResult = data.ToList();

        return (result, fullResult);
    }

    public static async Task<Anime> GetAnimeDetail(Anime anime)
    {
        var animeService = new SearchAnimeService();
        var data = await animeService.GetAnimeDetailsAsync(anime);

        return data;
    }
}
