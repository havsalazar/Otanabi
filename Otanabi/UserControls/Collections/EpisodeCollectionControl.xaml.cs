using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Otanabi.Core.Anilist.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Otanabi.UserControls;

public sealed partial class EpisodeCollectionControl : UserControl
{
    private readonly DispatcherQueue _dispatcherQueue;

    public EpisodeCollectionControl()
    {
        this.InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemsSource",
        typeof(ObservableCollection<MediaStreamingEpisode>),
        typeof(EpisodeCollectionControl),
        null
    );

    public ObservableCollection<MediaStreamingEpisode> ItemsSource
    {
        get => (ObservableCollection<MediaStreamingEpisode>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static DependencyProperty NextAiringProperty = DependencyProperty.Register(
        "NextAiring",
        typeof(AiringSchedule),
        typeof(EpisodeCollectionControl),
        null
    );

    public ObservableCollection<MediaStreamingEpisode> EpisodeList = new ObservableCollection<MediaStreamingEpisode>();

    public AiringSchedule NextAiring
    {
        get => (AiringSchedule)GetValue(NextAiringProperty);
        set => SetValue(NextAiringProperty, value);
    }

    public event EventHandler<MediaStreamingEpisode> EpisodeSelected;

    private void Grid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.DataContext is MediaStreamingEpisode episode)
        {
            EpisodeSelected?.Invoke(this, episode);
        }
    }

    public void LoadNextAiringData()
    {
        EpisodeList.Clear();
        if (NextAiring != null)
        {
            var episodeReleaseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            episodeReleaseTime = episodeReleaseTime.AddSeconds(NextAiring.AiringAt).ToLocalTime();
            var timeRemaining = episodeReleaseTime - DateTime.Now;
            var countDownText = "";

            if (timeRemaining.TotalDays > 60) // More than 2 months
            {
                int months = (int)(timeRemaining.TotalDays / 30);
                int days = (int)(timeRemaining.TotalDays % 30);
                countDownText = $"{months} month{(months != 1 ? "s" : "")} and {days} day{(days != 1 ? "s" : "")} ";
            }
            else if (timeRemaining.TotalDays > 1) // More than 1 day
            {
                int days = (int)timeRemaining.TotalDays;
                int hours = timeRemaining.Hours;
                countDownText = $"{days} day{(days != 1 ? "s" : "")} and {hours} hour{(hours != 1 ? "s" : "")} ";
            }
            else if (timeRemaining.TotalHours > 1) // More than 1 hour
            {
                int hours = (int)timeRemaining.TotalHours;
                int minutes = timeRemaining.Minutes;
                countDownText = $"{hours} hour{(hours != 1 ? "s" : "")} and {minutes} minute{(minutes != 1 ? "s" : "")} ";
            }
            else // Less than 1 hour
            {
                int minutes = (int)timeRemaining.TotalMinutes;
                int seconds = timeRemaining.Seconds;
                countDownText = $"{minutes} minute{(minutes != 1 ? "s" : "")} and {seconds} second{(seconds != 1 ? "s" : "")} ";
            }
            NextAiringTextBlock.Text = countDownText;
            var fakeEpisode = new MediaStreamingEpisode
            {
                Title = $"Next Episode in {countDownText}",
                Number = 0,
                Thumbnail = "//Assets/OtanabiSplash.png",
                IsValid = false,
            };
            EpisodeList.Add(fakeEpisode);
        }
        foreach (var episode in ItemsSource)
        {
            EpisodeList.Add(episode);
        }
    }

    private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (sender is Grid grid)
                {
                    var bg = (Border)this.Resources["HoverBg"];
                    grid.BorderBrush = bg.Background;
                    //createAnimation(grid, 1.01);
                }
            }
            catch (Exception) { }
        });
    }

    private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (sender is Grid grid)
                {
                    var bg = (Border)this.Resources["NormalBg"];
                    grid.BorderBrush = bg.Background;
                    //createAnimation(grid, 1.0);
                }
            }
            catch (Exception) { }
        });
    }

    private void createAnimation(Grid element, double to)
    {
        // Find the ScaleTransform in the element's RenderTransform
        var scaleTransform = element.RenderTransform as ScaleTransform;
        if (scaleTransform == null)
        {
            // If not already set, create a new one
            scaleTransform = new ScaleTransform();
            element.RenderTransform = scaleTransform;
        }

        // Create animation for X scale
        var animationX = new DoubleAnimation
        {
            To = to, // Scale to 110%
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase(),
        };

        // Set target and property for X scale
        Storyboard.SetTarget(animationX, scaleTransform);
        Storyboard.SetTargetProperty(animationX, "ScaleX");

        // Create and start X storyboard
        var storyboardX = new Storyboard();
        storyboardX.Children.Add(animationX);
        storyboardX.Begin();

        // Create animation for Y scale
        var animationY = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase(),
        };

        // Set target and property for Y scale
        Storyboard.SetTarget(animationY, scaleTransform);
        Storyboard.SetTargetProperty(animationY, "ScaleY");

        // Create and start Y storyboard
        var storyboardY = new Storyboard();
        storyboardY.Children.Add(animationY);
        storyboardY.Begin();
    }
}
