using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Anilist.Enums;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Models;
using Otanabi.Core.Services;
using Otanabi.Services;
using Otanabi.UserControls;
using Windows.System;

namespace Otanabi.ViewModels;

public partial class DetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly AnilistService _anilistService = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SearchAnimeService _searchAnimeService = new();
    private readonly INavigationService _navigationService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly DatabaseService db = new();

    public ObservableCollection<Provider> Providers { get; } = new();

    [ObservableProperty]
    private bool isLoadingVideo = false;

    [ObservableProperty]
    public bool errorActive = false;

    [ObservableProperty]
    public string errorMessage = string.Empty;

    [ObservableProperty]
    private Media selectedMedia;

    [ObservableProperty]
    private Provider selectedProvider;

    [ObservableProperty]
    private BitmapImage bannerImage;

    [ObservableProperty]
    private string link;

    /*Match Anilist detail with provider*/
    private Anime _localAnime;

    /**/

    public ObservableCollection<MediaStreamingEpisode> EpisodeList { get; } =
        new ObservableCollection<MediaStreamingEpisode>();

    private EpisodeCollectionControl _episodeCollectionControl;

    [ObservableProperty]
    private bool isLoaded = false;

    [ObservableProperty]
    private bool isLoadedMerge = false;

    [ObservableProperty]
    private bool isLoadedEpisodes = false;

    [ObservableProperty]
    private bool isNotResults = false;
    public string StatusString
    {
        get
        {
            if (IsLoaded)
            {
                return SelectedMedia.Status switch
                {
                    MediaStatus.Finished => "Finished",
                    MediaStatus.Releasing => "Releasing",
                    MediaStatus.NotYetReleased => "Not Yet Released",
                    MediaStatus.Cancelled => "Cancelled",
                    _ => "Unknown",
                };
            }

            return "";
        }
    }

    public bool IsPlayeable
    {
        get
        {
            if (IsLoaded)
            {
                return SelectedMedia.Status switch
                {
                    MediaStatus.Finished => true,
                    MediaStatus.Releasing => true,
                    MediaStatus.NotYetReleased => false,
                    MediaStatus.Cancelled => false,
                    _ => false,
                };
            }
            return false;
        }
    }

    public DetailViewModel(INavigationService navigationService, ILocalSettingsService localSettingsService)
    {
        _navigationService = navigationService;
        _localSettingsService = localSettingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void OnNavigatedFrom() { }

    public async void OnNavigatedTo(object parameter)
    {
        GC.Collect();
        await GetProviders();

        if (parameter is Media media)
        {
            if (SelectedMedia == null || SelectedMedia.Id != media.Id)
            {
                await LoadMediaAsync(media.Id);
            }
        }
    }

    private async Task LoadMediaAsync(int id)
    {
        var data = await _anilistService.GetMediaByIdAsync(id);
        BannerImage =
            data.BannerImage != null
                ? new BitmapImage(new Uri(data.BannerImage))
                : new BitmapImage(new Uri(data.CoverImage.ExtraLarge));

        Link = $"https://anilist.co/anime/{data.Id}";
        EpisodeList.Clear();

        if (data.Status == MediaStatus.NotYetReleased)
        {
            IsLoadedMerge = true;
        }

        SelectedMedia = data;
        IsLoaded = true;
        EpisodesLoaded();
        OnPropertyChanged(nameof(StatusString));
        OnPropertyChanged(nameof(IsPlayeable));
        await SearchReferences();
    }

    [RelayCommand]
    private void OpenLink()
    {
        _dispatcherQueue.TryEnqueue(async () => await Launcher.LaunchUriAsync(new Uri(Link)));
    }

    private async Task GetProviders()
    {
        if (Providers.Count == 0)
        {
            Providers.Clear();
            var provs = _searchAnimeService.GetProviders().Where(prov => prov.IsTrackeable).ToList();
            foreach (var item in provs)
            {
                Providers.Add(item);
            }
            SelectedProvider = provs[0];
        }
        /* set the default provider definied in settings */
        var provdef = await _localSettingsService.ReadSettingAsync<int>("ProviderId");

        if (provdef != 0)
        {
            var tmp = Providers.FirstOrDefault(p => p.Id == provdef);
            if (tmp != null)
                SelectedProvider = tmp;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ProviderChanged()
    {
        IsLoadedMerge = false;
        IsNotResults = false;
        IsLoadedEpisodes = false;
        await SearchReferences();
    }

    public void LoadEpisodeComponent(EpisodeCollectionControl episodeCollectionControl)
    {
        _episodeCollectionControl = episodeCollectionControl;
    }

    [RelayCommand]
    private async Task LoadEpisode(MediaStreamingEpisode episode)
    {
        if (_localAnime != null)
        {
            var ep = _localAnime.Chapters.Where(x => x.ChapterNumber == episode.Number).FirstOrDefault();
            if (ep != null)
            {
                Debug.Print($"Episode selected: {ep.Url}");
                await OpenPlayer(ep);
            }
        }
    }

    private void EpisodesLoaded()
    {
        var data = SelectedMedia.NextAiringEpisode;
        _episodeCollectionControl.LoadNextAiringData();
    }

    private async Task SearchReferences()
    {
        var data = await SearchEngineService.SearchByName(SelectedMedia.Title, SelectedProvider);
        var exactMatch = data.Item1;
        var otherMatches = data.Item2;

        if (exactMatch != null)
        {
            _localAnime = await _searchAnimeService.GetAnimeDetailsAsync(exactMatch);
            EpisodeList.Clear();
            foreach (var item in _localAnime.Chapters.OrderByDescending(x => x.ChapterNumber))
            {
                var matchedEpisode = selectedMedia.StreamingEpisodes.FirstOrDefault(x =>
                    x.Number == item.ChapterNumber
                );

                var title = matchedEpisode != null ? matchedEpisode.Title : "";
                var thumbnail = matchedEpisode != null ? matchedEpisode.Thumbnail : "";
                var episode = new MediaStreamingEpisode
                {
                    Title = title,
                    Number = item.ChapterNumber,
                    Thumbnail = thumbnail,
                    IsValid = true,
                    Url = item.Url,
                };
                EpisodeList.Add(episode);
            }
            if (EpisodeList.Count == 0)
            {
                IsNotResults = true;
                IsLoadedEpisodes = false;
            }
            else
            {
                IsNotResults = false;
                EpisodesLoaded();
                IsLoadedEpisodes = true;
            }
            IsLoadedMerge = true;

            var savedAnime = await db.GetOrAddAnimeByMedia(SelectedMedia, selectedProvider, _localAnime);

            _localAnime.Id = savedAnime.Id;
        }
        else
        {
            IsNotResults = true;
            IsLoadedEpisodes = false;
            IsLoadedMerge = true;
        }
    }

    public async Task OpenPlayer(Chapter chapter)
    {
        _dispatcherQueue.TryEnqueue(() => IsLoadingVideo = true);

        try
        {
            App.AppState.TryGetValue("Incognito", out var incognito);
            dynamic data = new ExpandoObject();
            data.History = (bool)incognito
                ? null
                : await db.GetOrCreateHistoryByCap(_localAnime, chapter.ChapterNumber);
            data.IsIncognito = (bool)incognito;
            data.Chapter = chapter;
            data.AnimeTitle = _localAnime.Title;
            data.Anime = _localAnime;
            data.ChapterList = _localAnime.Chapters.ToList();
            data.Provider = _localAnime.Provider;

            _dispatcherQueue.TryEnqueue(() =>
            {
                IsLoadingVideo = false;
                _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, data);
            });
        }
        catch (Exception e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsLoadingVideo = false;
                ErrorMessage = e.Message.ToString();
                ErrorActive = true;
            });
            return;
        }
    }
}
