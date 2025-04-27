using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Anilist.Enums;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Otanabi.ViewModels;

public partial class SeasonalViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;

    private readonly DispatcherQueue _dispatcherQueue;

    private AnilistService _anilistService = new();
    public ObservableCollection<Media> AnimeList { get; } = new();

    public int[] Years { get; } = Enumerable.Range(2009, ((DateTime.Now.Year + 2) - 2009)).Reverse().ToArray();

    private int CurrPage = 1;
    private bool HasMore = true;

    [ObservableProperty]
    private int selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private MediaSeason selectedSeason;

    [ObservableProperty]
    private SelectorBarItem selectedSeasonBar;

    private SelectorBarItem[] selectorBars;

    public SeasonalViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    private async Task LoadSeasonalAnimes()
    {
        // Load seasonal animes
        await LoadData(SelectedSeason, SelectedYear);
    }

    public async void OnNavigatedTo(object parameter) { }

    public void OnNavigatedFrom() { }

    private async Task LoadData(MediaSeason season, int year)
    {
        var response = await _anilistService.GetSeasonal(season: season, seasonYear: year, page: CurrPage);

        var pageInfo = response.Item2;
        HasMore = (bool)pageInfo.HasNextPage;

        foreach (var anime in response.Item1)
        {
            AnimeList.Add(anime);
        }
    }

    [RelayCommand]
    private async Task LoadedView(SelectorBar selectorBar)
    {
        if (AnimeList.Count > 0)
        {
            return;
        }
        selectorBars = selectorBar.Items.ToArray();
        AnimeList.Clear();
        LoadCurrentSeason();
        await LoadData(SelectedSeason, SelectedYear);
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (!HasMore)
        {
            return;
        }
        CurrPage++;
        await LoadData(SelectedSeason, SelectedYear);
    }

    [RelayCommand]
    private async Task SeasonChanged(object e)
    {
        if (e is SelectorBar bar && bar.SelectedItem is SelectorBarItem item)
        {
            if (item != null)
            {
                var selector = item.Tag;
                var season = selector switch
                {
                    "SelectorSpring" => MediaSeason.Spring,
                    "SelectorSummer" => MediaSeason.Summer,
                    "SelectorFall" => MediaSeason.Fall,
                    "SelectorWinter" => MediaSeason.Winter,
                    _ => MediaSeason.Winter,
                };
                AnimeList.Clear();
                SelectedSeason = season;
                SelectedSeasonBar = item;
                OnPropertyChanged(nameof(SelectedSeason));
                CurrPage = 1;
                await LoadData(SelectedSeason, SelectedYear);
            }
        }
    }

    [RelayCommand]
    private async Task YearChanged(int year)
    {
        if (SelectedSeasonBar != null)
        {
            SelectedYear = year;
            OnPropertyChanged(nameof(SelectedYear));
            AnimeList.Clear();
            CurrPage = 1;
            await LoadData(SelectedSeason, SelectedYear);
        }
    }

    [RelayCommand]
    private void OnItemClick(Media? clickedItem)
    {
        if (clickedItem != null)
        {
            _dispatcherQueue.TryEnqueue(
                () => _navigationService.NavigateTo(typeof(DetailViewModel).FullName!, clickedItem)
            );
        }
    }

    private void LoadCurrentSeason()
    {
        var currDate = DateTime.Now.Date;
        var month = currDate.Month;
        var day = currDate.Day;
        if ((month == 3 && day >= 20) || month == 4 || month == 5 || (month == 6 && day < 21))
        {
            SelectedSeasonBar = selectorBars.FirstOrDefault(x => x.Name == "SelectorSpring");
            SelectedSeason = MediaSeason.Spring;
        }
        else if ((month == 6 && day >= 21) || month == 7 || month == 8 || (month == 9 && day < 22))
        {
            SelectedSeasonBar = selectorBars.FirstOrDefault(x => x.Name == "SelectorSummer");
            SelectedSeason = MediaSeason.Summer;
        }
        else if ((month == 9 && day >= 22) || month == 10 || month == 11 || (month == 12 && day < 21))
        {
            SelectedSeasonBar = selectorBars.FirstOrDefault(x => x.Name == "SelectorFall");
            SelectedSeason = MediaSeason.Fall;
        }
        else
        {
            SelectedSeasonBar = selectorBars.FirstOrDefault(x => x.Name == "SelectorWinter");
            SelectedSeason = MediaSeason.Winter;
        }
        OnPropertyChanged(nameof(SelectedSeasonBar));
    }
}
