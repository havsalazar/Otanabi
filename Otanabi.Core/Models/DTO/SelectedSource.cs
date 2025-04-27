using System.Net.Http.Headers;

namespace Otanabi.Core.Models;

public class SelectedSource
{
    public string StreamUrl { get; set; } = string.Empty;
    public string SubtitleUrl { get; set; } = string.Empty;
    public HttpHeaders HttpHeaders { get; set; } = new HttpClient().DefaultRequestHeaders;
}
