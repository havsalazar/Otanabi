using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Models;
using Otanabi.Core.Services;
using Otanabi.Models.Enums;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Otanabi.ViewModels;

public partial class ProviderDetailViewModel : ObservableRecipient, INavigationAware
{
    private readonly SearchAnimeService _searchAnimeService = new();
    private readonly DatabaseService _Db = new();
    private readonly SelectSourceService _selectSourceService = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly INavigationService _navigationService;

    public ObservableCollection<Chapter> ChapterList { get; } = new ObservableCollection<Chapter>();
    public ObservableCollection<FavoriteList> FavLists { get; } = new ObservableCollection<FavoriteList>();

    [ObservableProperty]
    private bool isLoadingVideo = false;

    [ObservableProperty]
    private bool isLoadingFav = true;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool forceLoad = false;

    [ObservableProperty]
    public string favText = "";

    [ObservableProperty]
    public List<FavoriteList> favoritelistsSelected;

    [ObservableProperty]
    private Anime selectedAnime;

    [ObservableProperty]
    private Chapter[]? chapters;

    [ObservableProperty]
    private string favStatus = "\uE728";

    [ObservableProperty]
    private bool isFavorite = false;

    [ObservableProperty]
    private string orderIcon = "\uE74B";

    private bool orderedList = false;

    [ObservableProperty]
    public bool errorActive = false;

    [ObservableProperty]
    public string errorMessage = "";

    public ProviderDetailViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Anime animeParam)
        {
            if (SelectedAnime != null && animeParam.Url == SelectedAnime.Url)
            {
                return;
            }

            GC.Collect();
            IsLoadingFav = true;
            IsLoading = true;
            var favoriteLists = await _Db.GetFavoriteLists();
            FavLists.Clear();
            foreach (var fav in favoriteLists)
            {
                FavLists.Add(fav);
            }
            await GetAnimeData(animeParam);
            IsLoadingFav = false;
            IsLoading = false;
        }
    }

    private async Task GetAnimeData(Anime request)
    {
        var provAnime = await _searchAnimeService.GetAnimeDetailsAsync(request);
        ChapterList.Clear();
        SetInitialFavoriteStatus();
        if (provAnime != null)
        {
            SelectedAnime = provAnime;
            Chapters = provAnime.Chapters.OrderByDescending(x => x.ChapterNumber).ToArray();
            var tmpAnime = await _Db.GetOrCreateAnime(SelectedAnime.Provider, SelectedAnime);
            SelectedAnime.Id = tmpAnime.Id;
            await checkFavorite();
            SelectedAnime.IdAnilist = tmpAnime.IdAnilist;
            OnPropertyChanged(nameof(SelectedAnime));
            foreach (var chapter in Chapters)
            {
                ChapterList.Add(chapter);
            }
        }
        else
        {
            ErrorMessage = "Error loading anime data";
            ErrorActive = true;
        }
    }

    private void setLoader(bool value)
    {
        IsLoading = value;
    }

    public void OnNavigatedFrom() { }

    public async void OpenPlayer(Chapter chapter)
    {
        IsLoadingVideo = true;
        try
        {
            App.AppState.TryGetValue("Incognito", out var incognito); // Incognito mode
            dynamic data = new ExpandoObject();
            data.History = (bool)incognito
                ? null
                : await _Db.GetOrCreateHistoryByCap(SelectedAnime, chapter.ChapterNumber);
            data.IsIncognito = (bool)incognito;
            data.Chapter = chapter;
            data.AnimeTitle = SelectedAnime.Title;
            data.Anime = SelectedAnime;
            data.ChapterList = SelectedAnime.Chapters.ToList();
            data.Provider = SelectedAnime.Provider;
            _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, data);
            IsLoadingVideo = false;
        }
        catch (Exception e)
        {
            IsLoadingVideo = false;
            ErrorMessage = e.Message.ToString();
            ErrorActive = true;
            return;
        }
    }

    [RelayCommand]
    private void OrderChapterList()
    {
        ChapterList.Clear();
        orderedList = !orderedList;
        OrderIcon = orderedList ? "\uE74A" : "\uE74B";

        if (orderedList)
        {
            foreach (var chapter in SelectedAnime.Chapters)
            {
                ChapterList.Add(chapter);
            }
        }
        else
        {
            foreach (var chapter in SelectedAnime.Chapters.Reverse())
            {
                ChapterList.Add(chapter);
            }
        }
    }

    [RelayCommand]
    private async Task FavoriteFun()
    {
        IsLoadingFav = true;
        var action = IsFavorite ? "remove" : "add";
        var res = await _Db.AddToFavorites(SelectedAnime, action);

        IsFavorite = res == "added" ? true : false;
        FavStatus = IsFavorite ? "\uE8D9" : "\uE728";
        FavText = IsFavorite ? "Remove from Favorites" : "Add to Favorites";
        IsLoadingFav = false;
    }

    private async Task checkFavorite()
    {
        IsFavorite = await _Db.IsFavorite(SelectedAnime.Id);
        // Icon TO FAV "\uE728"
        // Icon TO UNFAV "\uE8D9"
        FavStatus = IsFavorite ? "\uE8D9" : "\uE728";
        FavText = IsFavorite ? "Remove from Favorites" : "Add to Favorites";
    }

    private void SetInitialFavoriteStatus()
    {
        IsFavorite = false;
        FavStatus = "\uE728";
        FavText = "Add to Favorites";
    }

    [RelayCommand]
    private async Task ChangeFavLists(object param)
    {
        var idList = new List<int>();
        if (param is ListBox box)
        {
            foreach (var item in box.SelectedItems)
            {
                if (item is FavoriteList lt)
                {
                    idList.Add(lt.Id);
                }
            }
        }
        if (idList.Count > 0)
        {
            await _Db.UpdateAnimeList(SelectedAnime.Id, idList);
        }
    }

    [RelayCommand]
    private void GenreClick(object param)
    {
        if (param is string genre)
        {
            var tag = new Tag() { Name = genre };
            var data = new Dictionary<string, object>
            {
                { "Tag", tag },
                { "Provider", SelectedAnime.Provider },
                { "Method", SearchMethods.SearchByTag },
            };
            _dispatcherQueue.TryEnqueue(
                () => _navigationService.NavigateTo(typeof(ProviderSearchViewModel).FullName!, data)
            );
        }
    }
}
