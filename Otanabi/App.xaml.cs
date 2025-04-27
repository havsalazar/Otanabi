using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Otanabi.Activation;
using Otanabi.Contracts.Services;
using Otanabi.Core.Contracts.Services;
using Otanabi.Core.Services;
using Otanabi.Models;
using Otanabi.Notifications;
using Otanabi.Services;
using Otanabi.ViewModels;
using Otanabi.Views;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Otanabi;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly LoggerService logger = new();
    private readonly AppUpdateService _appUpdateService = new();
    public static WindowEx MainWindow { get; } = new MainWindow();
    public static Dictionary<string, object> AppState = new() { { "Incognito", false }, { "Volume", 0.5 } };

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Host = Microsoft
            .Extensions.Hosting.Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(
                (context, services) =>
                {
                    // Default Activation Handler
                    services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                    // Other Activation Handlers
                    services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

                    // Services
                    services.AddSingleton<IAppNotificationService, AppNotificationService>();
                    services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                    services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                    services.AddTransient<INavigationViewService, NavigationViewService>();

                    services.AddSingleton<IActivationService, ActivationService>();
                    services.AddSingleton<IPageService, PageService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IWindowPresenterService, WindowPresenterService>();

                    // Core Services
                    services.AddSingleton<IFileService, FileService>();

                    // Views and ViewModels
                    services.AddTransient<HistoryViewModel>();
                    services.AddTransient<HistoryPage>();
                    services.AddTransient<FavoritesViewModel>();
                    services.AddTransient<FavoritesPage>();
                    services.AddTransient<VideoPlayerViewModel>();
                    services.AddTransient<VideoPlayerPage>();
                    services.AddTransient<ProviderDetailViewModel>();
                    services.AddTransient<ProviderDetailPage>();
                    services.AddTransient<ProviderSearchViewModel>();
                    services.AddTransient<ProviderSearchPage>();
                    services.AddTransient<SettingsViewModel>();
                    services.AddTransient<SettingsPage>();
                    services.AddTransient<ShellPage>();
                    services.AddTransient<ShellViewModel>();
                    services.AddTransient<SeasonalViewModel>();
                    services.AddTransient<SeasonalPage>();
                    services.AddTransient<DetailViewModel>();
                    services.AddTransient<DetailPage>();
                    services.AddTransient<SearchViewModel>();
                    services.AddTransient<SearchPage>();
                    services.AddTransient<ScheduleViewModel>();
                    services.AddTransient<SchedulePage>();
                    // Configuration
                    services.Configure<LocalSettingsOptions>(
                        context.Configuration.GetSection(nameof(LocalSettingsOptions))
                    );
                }
            )
            .Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        logger.LogFatal("App Crashed {0}", e.Message);
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);
        var isNeedUpdate = await _appUpdateService.IsNeedUpdate();

        if (isNeedUpdate)
        {
            _ = App.GetService<IAppNotificationService>().ShowByUpdate();
        }
    }
}
