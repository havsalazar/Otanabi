using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Otanabi.Contracts.Services;
using Otanabi.Core.Services;
using Otanabi.Views;

namespace Otanabi.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool isBackEnabled;

    [ObservableProperty]
    private object? selected;

    [ObservableProperty]
    private NavigationViewPaneDisplayMode paneDisplayMode = NavigationViewPaneDisplayMode.Auto;

    private readonly LoggerService logger = new();

    //getters and setters

    public ICommand MenuFileExitCommand { get; }

    public ICommand MenuSettingsCommand { get; }

    public ICommand MenuViewsMainCommand { get; }

    public INavigationService NavigationService { get; }

    public IWindowPresenterService _windowPresenterService { get; }

    public INavigationViewService NavigationViewService { get; }

    public bool IsNotFullScreen => !_windowPresenterService.IsFullScreen;

    // end   getters and setters
    public ShellViewModel(
        INavigationService navigationService,
        IWindowPresenterService windowPresenterService,
        INavigationViewService navigationViewService
    )
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;

        _windowPresenterService = windowPresenterService;
        _windowPresenterService.WindowPresenterChanged += OnWindowPresenterChanged;

        /* MenuFileExitCommand = new RelayCommand(OnMenuFileExit);
         MenuSettingsCommand = new RelayCommand(OnMenuSettings);
         MenuViewsMainCommand = new RelayCommand(OnMenuViewsMain);*/
    }

    private void OnWindowPresenterChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsNotFullScreen));
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (e.SourcePageType == typeof(VideoPlayerPage))
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
        }
        else
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Auto;
        }

        if (e.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}
