using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Otanabi.Core.Anilist.Models;
using Otanabi.Core.Services;

namespace Otanabi.UserControls;

public sealed partial class AnimeDetailCollectionControl
{
    public AnimeDetailCollectionControl()
    {
        this.InitializeComponent();
    }

    public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource",
        typeof(ObservableCollection<Media>),
        typeof(AnimeDetailCollectionControl),
        null
    );

    public ObservableCollection<Media> ItemsSource
    {
        get => (ObservableCollection<Media>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public event EventHandler<Media> MediaSelected;

    private void Card_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border bd && bd.DataContext is Media media)
        {
            MediaSelected?.Invoke(this, media);
        }
    }
}
