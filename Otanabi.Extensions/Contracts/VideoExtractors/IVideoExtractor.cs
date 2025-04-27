using System.Net.Http.Headers;
using Otanabi.Core.Models;

namespace Otanabi.Extensions.Contracts.VideoExtractors;

public interface IVideoExtractor
{
    Task<SelectedSource> GetStreamAsync(string url);
}
