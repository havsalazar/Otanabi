using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Models;
using Otanabi.Core.Services;

namespace Otanabi.ViewModels;

public partial class HistoryViewModel : ObservableRecipient, INavigationAware
{
    private readonly DatabaseService dbService = new();
    private readonly INavigationService _navigationService;
    private readonly SearchAnimeService animeService = new();

    [ObservableProperty]
    public Boolean errorActive = false;

    [ObservableProperty]
    public string errorMessage = "";

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool isLoadingVideo = false;

    private bool noData = false;

    public ObservableCollection<History> Histories { get; } = new ObservableCollection<History>();

    public HistoryViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async void OnNavigatedFrom()
    {
        // Histories.Clear();
        //await LoadHistory();
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadHistory();
    }

    [RelayCommand]
    public async Task LoadHistory()
    {
        if (IsLoading && noData)
            return;

        IsLoading = true;
        var history = (await dbService.GetAllHistories()).OrderByDescending(h => h.WatchedDate).ToList();
        if (history != null)
        {
            foreach (var item in history)
            {
                Histories.Add(item);
            }
        }
        else
        {
            noData = true;
        }
        IsLoading = false;
    }

    [RelayCommand]
    public async Task PrepareVideo(object param)
    {
        if (param is History history)
        {
            if (IsLoadingVideo)
                return;

            IsLoadingVideo = true;

            var selectedHistory = history;
            var selectedAnime = selectedHistory.Anime;

            var queriedAnime = await animeService.GetAnimeDetailsAsync(selectedAnime);
            selectedAnime.Chapters = queriedAnime.Chapters.ToArray();
            var selectedChapter = selectedAnime
                .Chapters.Where(c => c.ChapterNumber == selectedHistory.ChapterNumber)
                .FirstOrDefault();

            await OpenPlayer(selectedHistory, selectedChapter, selectedAnime);
        }
    }

    public async Task OpenPlayer(History history, Chapter selectedChapter, Anime selectedAnime)
    {
        try
        {
            dynamic data = new ExpandoObject();
            data.History = history;
            data.Chapter = selectedChapter;
            data.AnimeTitle = selectedAnime.Title;
            data.Anime = selectedAnime;
            data.ChapterList = selectedAnime.Chapters.ToList();
            data.Provider = selectedAnime.Provider;
            data.IsIncognito = false;
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
    public async Task DeleteHistoryById(object senderId)
    {
        if (senderId is int id)
        {
            var hs = Histories.Where(h => h.Id == id).First();
            Histories.Remove(hs);
            await dbService.DeleteFromHistory(id);
        }
    }
}
