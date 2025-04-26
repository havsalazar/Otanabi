using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Otanabi.Core.Models;
using Otanabi.Core.Services;

namespace Otanabi.UserControls;

public sealed partial class AnimePaneControl : UserControl
{
    private readonly DatabaseService dbService = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SearchAnimeService _searchAnimeService = new();

    public AnimePaneControl()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        this.InitializeComponent();
        MainItemRepeater.ElementPrepared += MainItemRepeater_ElementPrepared;
    }

    public event EventHandler<Anime> AnimeSelected;
    public event EventHandler BottomReached;
    public event EventHandler FavoriteAnimeChanged;

    public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource",
        typeof(ObservableCollection<Anime>),
        typeof(AnimePaneControl),
        null
    );

    public ObservableCollection<Anime> ItemsSource
    {
        get => (ObservableCollection<Anime>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static DependencyProperty FavoriteListIdProperty = DependencyProperty.Register(
        "FavoriteListId",
        typeof(int),
        typeof(AnimePaneControl),
        null
    );
    public int FavoriteListId
    {
        get => (int)GetValue(FavoriteListIdProperty);
        set => SetValue(FavoriteListIdProperty, value);
    }
    private FavoriteList[] Favorites = [];

    private void Card_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var bg = (Border)this.Resources["HoverBg"];
            border.Background = bg.Background;
        }
    }

    private void Card_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            var bg = (Border)this.Resources["NormalBg"];
            border.Background = bg.Background;
        }
    }

    private void MainItemRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        var grid = args.Element as FrameworkElement;
        if (grid != null)
        {
            var fadeAnimation = new DoubleAnimation()
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(0.7)),
            };
            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeAnimation);
            Storyboard.SetTarget(fadeAnimation, grid);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
            storyboard.Begin();
        }
    }

    private async void SubMenuOpen(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Anime anime)
        {
            if (Favorites.Length == 0)
            {
                await LoadFavorites();
            }

            var mFlyout = new MenuFlyout();
            var _maxLength = 15;
            var animeTitle = anime.Title.ToString();
            if (anime.Title.Length > _maxLength)
            {
                animeTitle = animeTitle.Substring(0, _maxLength) + "...";
            }

            mFlyout.Items.Add(new MenuFlyoutItem() { Text = animeTitle, Command = new RelayCommand(() => AnimeSelected?.Invoke(this, anime)) });
            mFlyout.Items.Add(new MenuFlyoutSeparator());

            var fListMenu = new MenuFlyoutSubItem()
            {
                Text = "Add to Favorites",
                Icon = new FontIcon() { Glyph = "\uE710" },
            };

            var selectedFavs = await dbService.GetFavoriteListByUrl(anime.Url, anime.Provider.Id);

            foreach (var fav in Favorites)
            {
                var fItem = new ToggleMenuFlyoutItem() { Text = fav.Name };
                if (selectedFavs != null)
                {
                    var selectedFav = selectedFavs.FirstOrDefault(x => x.Id == fav.Id);
                    if (selectedFav != null)
                    {
                        fItem.IsChecked = true;
                    }
                    else
                    {
                        fItem.IsChecked = false;
                    }
                }
                fItem.Command = new RelayCommand(async () => await AddToFavorites(anime, fav.Id));
                fListMenu.Items.Add(fItem);
            }

            mFlyout.Items.Add(fListMenu);
            mFlyout.ShowAt(btn);
        }
    }

    private async Task AddToFavorites(Anime anime, int favId)
    {
        var provAnime = await _searchAnimeService.GetAnimeDetailsAsync(anime);
        if (provAnime != null)
        {
            var tmpAnime = await dbService.GetOrCreateAnime(provAnime.Provider, provAnime);
            if (tmpAnime != null)
            {
                _ = await dbService.UpsertAnimeFavorite(tmpAnime, favId);
                FavoriteAnimeChanged?.Invoke(this, null);
            }
        }
    }

    private void Card_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border bd && bd.DataContext is Anime anime)
        {
            AnimeSelected?.Invoke(this, anime);
        }
    }

    public async Task LoadFavorites()
    {
        var favs = await dbService.GetFavoriteLists();
        Favorites = favs;
    }

    private static DateTime lastActionTime = DateTime.MinValue;
    private static readonly TimeSpan actionCooldown = TimeSpan.FromSeconds(1.5);

    private void MainScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                if (DateTime.Now - lastActionTime > actionCooldown)
                {
                    BottomReached?.Invoke(this, null);
                    lastActionTime = DateTime.Now;
                }
            }
        }
    }
}
