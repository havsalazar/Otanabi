using System.Net.Http.Headers;

namespace Otanabi.Core.Models;

public class SelectedSource
{
    public SelectedSource() { }

    public SelectedSource(string url)
    {
        StreamUrl = url;
    }

    public SelectedSource(string url, HttpHeaders headers)
    {
        StreamUrl = url;
        HttpHeaders = headers;
    }

    public SelectedSource(string url, string subtitleUrl, HttpHeaders headers)
    {
        StreamUrl = url;
        SubtitleUrl = subtitleUrl;
        HttpHeaders = headers;
    }

    public string StreamUrl { get; set; } = string.Empty;
    public string SubtitleUrl { get; set; } = string.Empty;
    public HttpHeaders? HttpHeaders { get; set; }
}
