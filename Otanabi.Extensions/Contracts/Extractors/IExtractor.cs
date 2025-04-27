using Otanabi.Core.Models;

namespace Otanabi.Extensions.Contracts.Extractors;

public interface IExtractor
{
    public Task<IAnime[]> MainPageAsync(int page = 1, Tag[]? tags = null);
    public Task<IAnime[]> SearchAnimeAsync(string searchTerm, int page, Tag[]? tags = null);
    public Task<IAnime> GetAnimeDetailsAsync(string requestUrl);
    public Task<IVideoSource[]> GetVideoSources(string requestUrl);
    public IProvider GenProvider();
    public Tag[] GetTags();
}
