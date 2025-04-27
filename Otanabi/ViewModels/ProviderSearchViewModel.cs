using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Models;
using Otanabi.Core.Services;
using Otanabi.Models.Enums;

namespace Otanabi.ViewModels;

public partial class ProviderSearchViewModel : ObservableRecipient, INavigationAware
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly INavigationService _navigationService;
    private readonly SearchAnimeService _searchAnimeService = new();
    private readonly ILocalSettingsService _localSettingsService;

    private string currQuery = string.Empty;
    private int currPage = 1;
    private readonly int maxItemsFirstLoad = 40;

    public ObservableCollection<Anime> Source { get; } = new ObservableCollection<Anime>();

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool noResults = false;

    [ObservableProperty]
    private Provider selectedProvider;

    public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider>();

    public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();

    private Tag[] OriginalTags = Array.Empty<Tag>();

    public ProviderSearchViewModel(INavigationService navigationService, ILocalSettingsService localSettingsService)
    {
        _navigationService = navigationService;
        _localSettingsService = localSettingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public async void OnNavigatedTo(object parameter)
    {
        await GetProviders();
        if (parameter != null && parameter is Dictionary<string, object> terms)
        {
            if (terms.ContainsKey("Method"))
            {
                switch ((SearchMethods)terms["Method"])
                {
                    case SearchMethods.SearchByTag:
                        var prov = (Provider)terms["Provider"];

                        SelectedProvider = Providers.First(x => x.Id == prov.Id);
                        var tag = (Tag)terms["Tag"];
                        ResetData();
                        LoadTags();
                        if (Tags.Count > 0)
                        {
                            Tags.First(t => t.Name == tag.Name).IsChecked = true;
                            OnPropertyChanged(nameof(Tags));
                            await Search("");
                        }
                        else
                        {
                            //if there are no tags(Provider does not support tagsearch), just load the main page
                            await LoadMainAnimePage();
                        }

                        break;
                }
            }
        }
        else
        {
            if (Source.Count == 0)
            {
                var provdef = await _localSettingsService.ReadSettingAsync<int>("ProviderId");

                if (provdef != 0)
                {
                    var tmp = Providers.FirstOrDefault(p => p.Id == provdef);
                    if (tmp != null)
                        SelectedProvider = tmp;
                }
                await Task.CompletedTask;
                await LoadMainAnimePage();
                LoadTags();
            }
        }
    }

    private async Task GetProviders()
    {
        if (Providers.Count == 0)
        {
            Providers.Clear();
            var provs = _searchAnimeService.GetProviders();
            foreach (var item in provs)
            {
                Providers.Add(item);
            }
            SelectedProvider = provs[0];
        }

        await Task.CompletedTask;
    }

    public async void OnAutoComplete(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        Source.Clear();
        var queryText = args.QueryText.ToString();
        currQuery = queryText;
        currPage = 1;
        if (currQuery == "")
            await LoadMainAnimePage();
        else
            await Search(queryText);
    }

    public async Task LoadMainAnimePage()
    {
        IsLoading = true;
        NoResults = false;
        var selectedTags = Tags.Where(t => t.IsChecked == true).ToArray();
        var data = await _searchAnimeService.MainPageAsync(SelectedProvider, currPage, selectedTags);
        if (data.Count() == 0)
        {
            NoResults = true;
            IsLoading = false;
            return;
        }
        foreach (var item in data)
        {
            Source.Add(item);
        }
        IsLoading = false;

        if (Source.Count < maxItemsFirstLoad)
        {
            await LoadMore();
        }
        OnPropertyChanged(nameof(Source));
    }

    public async Task Search(string query)
    {
        NoResults = false;
        IsLoading = true;
        var selectedTags = Tags.Where(t => t.IsChecked == true).ToArray();
        var data = await _searchAnimeService.SearchAnimeAsync(query, currPage, SelectedProvider, selectedTags);
        if (data.Count() == 0)
        {
            NoResults = true;
            IsLoading = false;
            return;
        }
        foreach (var item in data)
        {
            Source.Add(item);
        }
        IsLoading = false;
        if (Source.Count < maxItemsFirstLoad)
        {
            await Task.Delay(200);
            await LoadMore();
        }

        OnPropertyChanged(nameof(Source));
    }

    private void LoadTags()
    {
        Tags.Clear();
        var filters = _searchAnimeService.GetTags(SelectedProvider);
        if (filters.Length > 0)
        {
            foreach (var item in filters)
            {
                Tags.Add(item);
            }
            OriginalTags = (Tag[])filters.Clone();
        }
    }

    public void OnNavigatedFrom() { }

    private void ResetData()
    {
        Source.Clear();
        Tags.Clear();
        currQuery = string.Empty;
        currPage = 1;
    }

    [RelayCommand]
    private void OnItemClick(Anime? clickedItem)
    {
        if (clickedItem != null)
        {
            _dispatcherQueue.TryEnqueue(
                () => _navigationService.NavigateTo(typeof(ProviderDetailViewModel).FullName!, clickedItem)
            );
        }
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (IsLoading || NoResults)
        {
            return;
        }
        currPage++;
        if (currQuery == "")
        {
            await LoadMainAnimePage();
        }
        else
        {
            await Search(currQuery);
        }
    }

    [RelayCommand]
    private async Task OnProviderChanged(Provider selected)
    {
        ResetData();
        LoadTags();
        SelectedProvider = selected;
        await LoadMainAnimePage();
    }

    [RelayCommand]
    private void ResetFilterBox()
    {
        if (OriginalTags.Length > 0)
        {
            Tags.Clear();
            foreach (var tag in OriginalTags)
            {
                tag.IsChecked = false;
                Tags.Add(tag);
            }
        }
    }
}
