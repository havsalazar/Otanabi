using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Otanabi.UserControls;

public sealed partial class VideoPlayerControl : UserControl, IDisposable
{
    public VideoPlayerControl()
    {
        this.InitializeComponent();
        this.Unloaded += VideoPlayerControl_Unloaded;
    }

    private void VideoPlayerControl_Unloaded(object sender, RoutedEventArgs e)
    {
        // Clean up resources when the page is unloaded
        CleanupMediaResources();
    }

    public bool IsLoadPlayer = false;

    //#region Dependency Properties
    public static readonly DependencyProperty VideoSourceProperty = DependencyProperty.Register(
        nameof(VideoSource),
        typeof(string),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty FullScreenCommandProperty = DependencyProperty.Register(
        nameof(FullScreenCommand),
        typeof(ICommand),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty PreviusCommandProperty = DependencyProperty.Register(
        nameof(PreviusCommand),
        typeof(ICommand),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty NextCommandProperty = DependencyProperty.Register(
        nameof(NextCommand),
        typeof(ICommand),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty OpenPanelCommandProperty = DependencyProperty.Register(
        nameof(OpenPanelCommand),
        typeof(ICommand),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty SkipIntroCommandProperty = DependencyProperty.Register(
        nameof(SkipIntroCommand),
        typeof(ICommand),
        typeof(VideoPlayerControl),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty IsNextEnabledProperty = DependencyProperty.Register(
        nameof(IsNextEnabled),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty IsPrevEnabledProperty = DependencyProperty.Register(
        nameof(IsPrevEnabled),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty IsPanelOpenProperty = DependencyProperty.Register(
        nameof(IsPanelOpen),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty IsFullScreenProperty = DependencyProperty.Register(
        nameof(IsFullScreen),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );

    public bool IsNextEnabled
    {
        get => (bool)GetValue(IsNextEnabledProperty);
        set => SetValue(IsNextEnabledProperty, value);
    }
    public bool IsPrevEnabled
    {
        get => (bool)GetValue(IsPrevEnabledProperty);
        set => SetValue(IsPrevEnabledProperty, value);
    }
    public bool IsPanelOpen
    {
        get => (bool)GetValue(IsPanelOpenProperty);
        set => SetValue(IsPanelOpenProperty, value);
    }
    public bool IsFullScreen
    {
        get => (bool)GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }
    public ICommand FullScreenCommand
    {
        get => (ICommand)GetValue(FullScreenCommandProperty);
        set => SetValue(FullScreenCommandProperty, value);
    }
    public ICommand PreviusCommand
    {
        get => (ICommand)GetValue(PreviusCommandProperty);
        set => SetValue(PreviusCommandProperty, value);
    }
    public ICommand NextCommand
    {
        get => (ICommand)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }
    public ICommand OpenPanelCommand
    {
        get => (ICommand)GetValue(OpenPanelCommandProperty);
        set => SetValue(OpenPanelCommandProperty, value);
    }
    public ICommand SkipIntroCommand
    {
        get => (ICommand)GetValue(SkipIntroCommandProperty);
        set => SetValue(SkipIntroCommandProperty, value);
    }
    public string VideoSource
    {
        get => (string)GetValue(VideoSourceProperty);
        set => SetValue(VideoSourceProperty, value);
    }

    //#endregion

    private MediaPlayer mediaPlayer;
    private MediaPlayerElement mediaPlayerElement;
    private bool isDisposed = false;

    public void CreatePlayer(string videoUrl)
    {
        mediaPlayer = new MediaPlayer();
        mediaPlayerElement = new MediaPlayerElement
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            AreTransportControlsEnabled = true, // Show playback controls
            AutoPlay = true,
        };
        mediaPlayerElement.TransportControls = new AnimeMediaTransportControls
        {
            IsNextEnabled = IsNextEnabled,
            IsPrevEnabled = IsPrevEnabled,
            IsPanelOpen = IsPanelOpen,
            IsFullScreen = IsFullScreen,
            FullScreenCommand = FullScreenCommand,
            PreviusCommand = PreviusCommand,
            NextCommand = NextCommand,
            OpenPanelCommand = OpenPanelCommand,
            SkipIntroCommand = SkipIntroCommand,
        };
        mediaPlayerElement.SetMediaPlayer(mediaPlayer);
        VideoContainer.Children.Add(mediaPlayerElement);

        MediaSource mediaSource = MediaSource.CreateFromUri(new System.Uri(videoUrl));
        mediaPlayer.Source = mediaSource;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void CleanupMediaResources()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Pause();
            mediaPlayer.Source = null;
            if (mediaPlayerElement != null)
            {
                mediaPlayerElement.SetMediaPlayer(null);
            }
            if (mediaPlayerElement != null && VideoContainer.Children.Contains(mediaPlayerElement))
            {
                VideoContainer.Children.Remove(mediaPlayerElement);
            }
            mediaPlayer.Dispose();
            mediaPlayer = null;
        }

        mediaPlayerElement = null;
    }

    private void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                // Unregister event handler
                this.Unloaded -= VideoPlayerControl_Unloaded;

                // Clean up resources
                CleanupMediaResources();
            }

            isDisposed = true;
        }
    }
}
