using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Anilist.Enums;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Services;
using Otanabi.Services;

namespace Otanabi.ViewModels;

public partial class SearchViewModel : ObservableRecipient, INavigationAware
{
    private readonly AnilistService _anilistService = new();
    private readonly DatabaseService databaseService = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly INavigationService _navigationService;

    #region Variables
    public MediaSeason[] Seasons { get; } = Enum.GetValues<MediaSeason>().ToArray();
    public MediaFormat[] Formats { get; } = Enum.GetValues<MediaFormat>().ToArray();
    public MediaStatus[] Statuses { get; } = Enum.GetValues<MediaStatus>().ToArray();

    public ObservableCollection<string> Genres { get; } = new();
    public ObservableCollection<string> FilteredGenres { get; } = new();
    public ObservableCollection<Media> SourceMedia { get; } = new();

    public ObservableCollection<string> AutoSugestions { get; } = new();

    public int[] Years
    {
        get
        {
            var currYear = DateTime.Now.Year;
            return Enumerable.Range(1998, ((currYear + 2) - 1998)).Reverse().ToArray();
        }
    }

    [ObservableProperty]
    private Nullable<MediaSeason> selectedSeason;

    [ObservableProperty]
    private Nullable<MediaStatus> selectedStatus;

    public ObservableCollection<MediaFormat> SelectedFormats = new();

    public string GetSelectedFormats
    {
        get
        {
            if (SelectedFormats == null || SelectedFormats.Count == 0)
            {
                return "Select Formats";
            }
            return SelectedFormats.Count >= 2
                ? $"{SelectedFormats[0]} , +{SelectedFormats.Count - 1} "
                : string.Join(", ", SelectedFormats);
        }
    }

    public ObservableCollection<string> SelectedGenres = new();

    public string GetSelectedGenres
    {
        get
        {
            if (SelectedGenres == null || SelectedGenres.Count == 0)
            {
                return "Select Genres";
            }
            return SelectedGenres.Count >= 2
                ? $"{SelectedGenres[0]} , +{SelectedGenres.Count - 1} "
                : string.Join(", ", SelectedGenres);
        }
    }

    [ObservableProperty]
    private Nullable<int> selectedYear;

    [ObservableProperty]
    private bool isLoaded = false;

    [ObservableProperty]
    private bool hasMore = true;

    [ObservableProperty]
    private int currPage = 1;

    [ObservableProperty]
    private string searchQuery;

    #endregion

    public SearchViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        SelectedGenres.CollectionChanged += (s, e) => OnPropertyChanged(nameof(GetSelectedGenres));
        SelectedFormats.CollectionChanged += (s, e) => OnPropertyChanged(nameof(GetSelectedFormats));
    }

    public void OnNavigatedFrom() { }

    public async void OnNavigatedTo(object parameter)
    {
        await GetTags();
        await LoadRecentSugestions();
    }

    private async Task GetTags()
    {
        if (Genres.Count > 0)
        {
            return;
        }
        var data = await _anilistService.GetTagsASync();
        foreach (var item in data)
        {
            Genres.Add(item);
        }
        OnPropertyChanged(nameof(Genres));
        AssignGenres();
        OnPropertyChanged(nameof(FilteredGenres));
    }

    public void UpdateCollection(IList<object> selectedItems, string type)
    {
        if (type == "Genres")
        {
            SelectedGenres.Clear();
            foreach (var item in selectedItems)
            {
                SelectedGenres.Add(item.ToString());
            }
        }
        else
        {
            SelectedFormats.Clear();
            foreach (var item in selectedItems)
            {
                SelectedFormats.Add((MediaFormat)item);
            }
        }
    }

    private void AssignGenres()
    {
        FilteredGenres.Clear();
        foreach (var item in Genres)
        {
            FilteredGenres.Add(item);
        }
    }

    [RelayCommand]
    private async void LoadMore()
    {
        if (!HasMore)
        {
            return;
        }
        CurrPage++;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ClearSelectedGenres()
    {
        SelectedGenres.Clear();
        OnPropertyChanged(nameof(SelectedGenres));
    }

    [RelayCommand]
    private void OnItemClick(Media? clickedItem)
    {
        if (clickedItem != null)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(500);
                _navigationService.NavigateTo(typeof(DetailViewModel).FullName!, clickedItem);
            });
        }
    }

    public async void OnSearch(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await Search(args.QueryText.ToString());
    }

    private async Task Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        SearchQuery = query;
        await databaseService.AddToAutocomplete(query);
        await LoadDataAsync();
    }

    public async Task FilterGenders(object sender, TextChangedEventArgs args)
    {
        if (sender is TextBox texbox)
        {
            var query = texbox.Text;
            if (string.IsNullOrEmpty(query))
            {
                AssignGenres();
                return;
            }
            var filtered = Genres
                .Where(x => x.ToLower().Contains(query, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            FilteredGenres.Clear();
            foreach (var item in filtered)
            {
                FilteredGenres.Add(item);
            }
        }
    }

    private async Task LoadDataAsync()
    {
        var data = await _anilistService.SearchMedia(
            page: CurrPage,
            season: SelectedSeason,
            status: SelectedStatus,
            searchTerm: SearchQuery,
            year: SelectedYear,
            formats: SelectedFormats.Count > 0 ? SelectedFormats.ToArray() : null,
            genres: SelectedGenres.Count > 0 ? SelectedGenres.ToArray() : null
        );
        var pageInfo = data.Item2;
        HasMore = (bool)pageInfo.HasNextPage;

        if (CurrPage == 1)
        {
            SourceMedia.Clear();
        }

        foreach (var item in data.Item1)
        {
            SourceMedia.Add(item);
        }
    }

    private async Task LoadRecentSugestions()
    {
        AutoSugestions.Clear();
        var data = await databaseService.LastListAutoComplete();

        foreach (var item in data)
        {
            AutoSugestions.Add(item);
        }
    }

    public async Task OnAutoCompleteChanges(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var query = sender.Text;
        if (query.Length > 3 && !string.IsNullOrWhiteSpace(query))
        {
            var data = await databaseService.GetListAutoComplete(query);

            _dispatcherQueue.TryEnqueue(() =>
            {
                AutoSugestions.Clear();
                foreach (var item in data)
                {
                    AutoSugestions.Add(item);
                }
            });
        }
    }

    public async Task OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            var selectedItem = args.SelectedItem.ToString();
            await Search(selectedItem);
        });
    }

    [RelayCommand]
    public void OpenSuggestions()
    {
        Debug.WriteLine("clicked");
    }
}
