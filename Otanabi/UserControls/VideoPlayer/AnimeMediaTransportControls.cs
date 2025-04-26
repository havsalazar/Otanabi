using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Otanabi.UserControls;
using Otanabi.ViewModels;

namespace Otanabi.UserControls;

public sealed partial class AnimeMediaTransportControls : MediaTransportControls
{
    public AnimeMediaTransportControls()
    {
        DefaultStyleKey = typeof(AnimeMediaTransportControls);
    }

    public static readonly DependencyProperty FullScreenCommandProperty = DependencyProperty.Register(
        nameof(FullScreenCommand),
        typeof(ICommand),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty PreviusCommandProperty = DependencyProperty.Register(
        nameof(PreviusCommand),
        typeof(ICommand),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty NextCommandProperty = DependencyProperty.Register(
        nameof(NextCommand),
        typeof(ICommand),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty OpenPanelCommandProperty = DependencyProperty.Register(
        nameof(OpenPanelCommand),
        typeof(ICommand),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );
    public static readonly DependencyProperty SkipIntroCommandProperty = DependencyProperty.Register(
        nameof(SkipIntroCommand),
        typeof(ICommand),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(null)
    );

    public static readonly DependencyProperty IsNextEnabledProperty = DependencyProperty.Register(
        nameof(IsNextEnabled),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(
            default(bool),
            propertyChangedCallback: (d, e) =>
            {
                if (d is AnimeMediaTransportControls control)
                {
                    var button = control.GetTemplateChild("NextTrackButton") as AppBarButton;
                    if (button != null)
                    {
                        button.IsEnabled = (bool)e.NewValue;
                        if ((bool)e.NewValue)
                        {
                            VisualStateManager.GoToState((Control)control.GetTemplateChild("NextTrackButton"), "Normal", true);
                        }
                        else
                        {
                            VisualStateManager.GoToState((Control)control.GetTemplateChild("NextTrackButton"), "Disabled", true);
                        }
                    }
                }
            }
        )
    );
    public static readonly DependencyProperty IsPrevEnabledProperty = DependencyProperty.Register(
        nameof(IsPrevEnabled),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(
            default(bool),
            propertyChangedCallback: (d, e) =>
            {
                if (d is AnimeMediaTransportControls control)
                {
                    var button = control.GetTemplateChild("PreviousTrackButton") as AppBarButton;
                    if (button != null)
                    {
                        button.IsEnabled = (bool)e.NewValue;
                        if ((bool)e.NewValue)
                        {
                            VisualStateManager.GoToState((Control)control.GetTemplateChild("PreviousTrackButton"), "Normal", true);
                        }
                        else
                        {
                            VisualStateManager.GoToState((Control)control.GetTemplateChild("PreviousTrackButton"), "Disabled", true);
                        }
                    }
                }
            }
        )
    );
    public static readonly DependencyProperty IsPanelOpenProperty = DependencyProperty.Register(
        nameof(IsPanelOpen),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(false)
    );
    public static readonly DependencyProperty IsFullScreenProperty = DependencyProperty.Register(
        nameof(IsFullScreen),
        typeof(bool),
        typeof(AnimeMediaTransportControls),
        new PropertyMetadata(
            default(bool),
            new PropertyChangedCallback(
                (d, e) =>
                {
                    if (d is AnimeMediaTransportControls control)
                    {
                        var button = control.GetTemplateChild("FullScreenButton") as AppBarWindowPresenterStateButton;
                        if (button != null)
                        {
                            button.IsFullScreen = (bool)e.NewValue;
                        }
                    }
                }
            )
        )
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

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("FullScreenButton") is AppBarButton fullScreenButton)
        {
            fullScreenButton.Click += (s, e) => FullScreenCommand?.Execute(null);
        }
        if (GetTemplateChild("PreviousTrackButton") is AppBarButton prevButton)
        {
            prevButton.Click += (s, e) => PreviusCommand?.Execute(null);
        }
        if (GetTemplateChild("NextTrackButton") is AppBarButton nextButton)
        {
            nextButton.Click += (s, e) => NextCommand?.Execute(null);
        }
        if (GetTemplateChild("OpenPanelButton") is ToggleButton openPanelButton)
        {
            openPanelButton.Click += (s, e) => OpenPanelCommand?.Execute(null);
        }
        if (GetTemplateChild("SkipIntroButton") is Button skipIntroButton)
        {
            skipIntroButton.Click += (s, e) => SkipIntroCommand?.Execute(null);
        }
    }
}
