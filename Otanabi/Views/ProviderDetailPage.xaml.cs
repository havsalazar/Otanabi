using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Otanabi.Core.Models;
using Otanabi.Core.Services;
using Otanabi.ViewModels;

namespace Otanabi.Views;

public sealed partial class ProviderDetailPage : Page
{
    private int AnimeId;
    private List<FavoriteList> favoriteLists;
    private List<FavoriteList> selectedFList = new List<FavoriteList>();
    DatabaseService dbService = new();
    public ProviderDetailViewModel ViewModel { get; }

    public ProviderDetailPage()
    {
        ViewModel = App.GetService<ProviderDetailViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is Anime anime)
        {
            AnimeId = anime.Id;
            // await LoadAnimeFavList();
        }
        base.OnNavigatedTo(e);
    }

    private async Task LoadAnimeFavList()
    {
        var sFList1 = await dbService.GetFavoriteListByAnime(AnimeId);
        var tmpLit = new List<FavoriteList>();

        foreach (var item in sFList1)
        {
            foreach (var item1 in favListbox.Items)
            {
                if (item1 is FavoriteList ls && ls.Id == item.Id)
                {
                    favListbox.SelectedItems.Add(item1);
                }
            }
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back) { }
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem != null)
        {
            ViewModel.OpenPlayer((Chapter)e.ClickedItem);
        }
    }

    private async void FavoriteCombo_IsEnabledChanged(
        object sender,
        Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs e
    )
    {
        if (animetxtid.Tag is int aid)
        {
            AnimeId = aid;
        }
        if (sender is Button cb && cb.IsEnabled)
        {
            await LoadAnimeFavList();
        }
    }
}
