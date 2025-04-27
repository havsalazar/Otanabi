using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Otanabi.ViewModels;
using Windows.Media.Playback;

namespace Otanabi.Views;

public sealed partial class VideoPlayerPage : Page
{
    public VideoPlayerViewModel ViewModel { get; }

    public VideoPlayerPage()
    {
        ViewModel = App.GetService<VideoPlayerViewModel>();
        InitializeComponent();
        AMediaPlayer.Loaded += OnPlayerLoaded;
    }

    private void OnPlayerLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (AMediaPlayer != null)
        {
            ViewModel.setMediaPlayer(AMediaPlayer);
            ViewModel.InitializedCommand.Execute(null);
        }
    }
}
