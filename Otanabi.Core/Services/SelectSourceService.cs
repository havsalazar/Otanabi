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

            foreach (var source in orderedSources)
            {
                var reflex = _classReflectionHelper.GetMethodFromVideoSource(source);
                var method = reflex.Item1;
                var instance = reflex.Item2;

                selectedSource = await (Task<SelectedSource>)
                    method.Invoke(instance, new object[] { source.CheckedUrl });

                selectedSource.SubtitleUrl = source.Subtitle != null ? source.Subtitle : string.Empty;

                if (!string.IsNullOrEmpty(selectedSource.StreamUrl))
                {
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
        return selectedSource;
    }

    //this method will only return the sources with valid streamingUrl
    // I will use this eventually , but I don't want to add more buttons on the VideoUI
    public async Task<List<SelectedSource>> GetAllSourcesAsync(VideoSource[] videoSources)
    {
        List<SelectedSource> selectedSources = new();

        foreach (var source in videoSources)
        {
            try
            {
                var reflex = _classReflectionHelper.GetMethodFromVideoSource(source);
                var method = reflex.Item1;
                var instance = reflex.Item2;

                var selectedSource = await (Task<SelectedSource>)
                    method.Invoke(instance, new object[] { source.CheckedUrl });

                selectedSource.SubtitleUrl = source.Subtitle != null ? source.Subtitle : string.Empty;

                if (!string.IsNullOrEmpty(selectedSource.StreamUrl))
                {
                    selectedSources.Add(selectedSource);
                }
            }
            catch (Exception e)
            {
                logger.LogFatal("Failed on load video extension {0}", e.Message);
                throw;
            }
        }

        return selectedSources;
    }
}
