namespace Otanabi.Core.Models;

public interface IVideoSource
{
    int Ads { get; set; }
    bool Allow_mobile { get; set; }
    string CheckedUrl { get; }
    string Code { get; set; }
    string Server { get; set; }
    string Title { get; set; }
    string Url { get; set; }
    string Subtitle { get; set; }
}
