using System.Net.Http.Headers;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;

namespace Otanabi.Core.Services;

public sealed class SelectSourceService
{
    private readonly ClassReflectionHelper _classReflectionHelper = new();
    private readonly LoggerService logger = new();

    internal static List<T> MoveToFirst<T>(List<T> list, T item)
    {
        var newList = new List<T>(list);
        if (newList.Remove(item))
        {
            newList.Insert(0, item);
        }
        return newList;
    }

    public async Task<SelectedSource> SelectSourceAsync(VideoSource[] videoSources, string byDefault = "")
    {
        var selectedSource = new SelectedSource();
        HttpHeaders headers = new HttpClient().DefaultRequestHeaders;
        try
        {
            var item = videoSources.FirstOrDefault(e => e.Server == byDefault) ?? videoSources[0];
            var orderedSources = MoveToFirst(videoSources.ToList(), item);
            selectedSource.SubtitleUrl = item.Subtitle != null ? item.Subtitle : "";

            foreach (var source in orderedSources)
            {
                (string, HttpHeaders) tempUrl;
                var reflex = _classReflectionHelper.GetMethodFromVideoSource(source);
                var method = reflex.Item1;
                var instance = reflex.Item2;
                tempUrl = await (Task<(string, HttpHeaders)>)
                    method.Invoke(instance, new object[] { source.CheckedUrl });
                if (!string.IsNullOrEmpty(tempUrl.Item1))
                {
                    selectedSource.StreamUrl = tempUrl.Item1;
                    if (tempUrl.Item2 != null)
                    {
                        selectedSource.HttpHeaders = tempUrl.Item2;
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            logger.LogFatal("Failed on load video extension {0}", e.Message);
            selectedSource.StreamUrl = string.Empty;
            throw;
        }
        //return (streamUrl, subUrl, headers);
        return selectedSource;
    }
}
