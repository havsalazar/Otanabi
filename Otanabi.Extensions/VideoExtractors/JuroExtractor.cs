using System.Net.Http.Headers;
using Otanabi.Core.Models;
using Otanabi.Extensions.Contracts.VideoExtractors;

namespace Otanabi.Extensions.VideoExtractors;

internal class JuroExtractor : IVideoExtractor
{
    public async Task<SelectedSource> GetStreamAsync(string url)
    {
        await Task.CompletedTask;
        return new SelectedSource(url);
    }
}
