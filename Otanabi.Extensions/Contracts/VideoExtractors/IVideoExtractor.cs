using System.Net.Http.Headers;

namespace Otanabi.Extensions.Contracts.VideoExtractors;

public interface IVideoExtractor
{
    Task<(string, HttpHeaders?)> GetStreamAsync(string url);
}
