using Otanabi.Core.Helpers;
using Otanabi.Core.Models;

namespace Otanabi.Core.Services;

public sealed class SearchAnimeService
{
    private readonly ClassReflectionHelper _classReflectionHelper = new();

    public Provider[] GetProviders()
    {
        return _classReflectionHelper.GetProviders();
    }

    public async Task<Anime[]> MainPageAsync(Provider provider, int page = 1, Tag[] tags = null)
    {
        var reflex = _classReflectionHelper.GetMethodFromProvider("MainPageAsync", provider);
        var method = reflex.Item1;
        var instance = reflex.Item2;
        var animesTmp = (Anime[])await (Task<IAnime[]>)method.Invoke(instance, new object[] { page, tags });

        return animesTmp.ToArray();
    }

    public async Task<Anime[]> SearchAnimeAsync(string searchTerm, int page, Provider provider, Tag[] tags = null)
    {
        var reflex = _classReflectionHelper.GetMethodFromProvider("SearchAnimeAsync", provider);
        var method = reflex.Item1;
        var instance = reflex.Item2;
        var animesTmp = (Anime[])await (Task<IAnime[]>)method.Invoke(instance, new object[] { searchTerm, page, tags });

        return animesTmp.ToArray();
    }

    public async Task<Anime> GetAnimeDetailsAsync(Anime animeReq)
    {
        var reflex = _classReflectionHelper.GetMethodFromProvider("GetAnimeDetailsAsync", animeReq.Provider);
        var method = reflex.Item1;
        var instance = reflex.Item2;
        var animesDet = (Anime)await (Task<IAnime>)method.Invoke(instance, new object[] { animeReq.Url });

        return animesDet;
    }

    public async Task<VideoSource[]> GetVideoSources(string requestUrl, Provider provider)
    {
        var reflex = _classReflectionHelper.GetMethodFromProvider("GetVideoSources", provider);
        var method = reflex.Item1;
        var instance = reflex.Item2;
        var videoSources = (VideoSource[])
            await (Task<IVideoSource[]>)method.Invoke(instance, new object[] { requestUrl });

        return videoSources.ToArray();
    }

    public Tag[] GetTags(Provider provider)
    {
        var reflex = _classReflectionHelper.GetMethodFromProvider("GetTags", provider);
        var method = reflex.Item1;
        var instance = reflex.Item2;
        var tags = (Tag[])method.Invoke(instance, null);

        return tags.ToArray();
    }
}
