using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using Otanabi.Contracts.Services;
using Otanabi.Contracts.ViewModels;
using Otanabi.Core.Database;
using Otanabi.Core.Helpers;
using Otanabi.Core.Models;
using Otanabi.Core.Services;
using Otanabi.Helpers;
using Windows.ApplicationModel;

namespace Otanabi.ViewModels;

public partial class SettingsViewModel : ObservableRecipient, INavigationAware
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly SearchAnimeService _searchAnimeService = new();
    private readonly AppUpdateService _appUpdateService = new();
    private readonly DatabaseService _databaseService = new();
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    [ObservableProperty]
    private string versionMessage = "";

    [ObservableProperty]
    private string versionExtensions = "";

    [ObservableProperty]
    private string _appName = "";

    [ObservableProperty]
    private int selectedThemeIndex;

    [ObservableProperty]
    private string messageTitle = "";

    [ObservableProperty]
    private string messageSubTitle = "";

    [ObservableProperty]
    private bool isMessageVisible = false;

    private bool updateAvailable = false;

    public ICommand SwitchThemeCommand { get; }

    [ObservableProperty]
    private Provider selectedProvider;
    public ObservableCollection<Provider> Providers { get; } = new ObservableCollection<Provider>();

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
        _localSettingsService = localSettingsService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _versionDescription = GetVersionDescription();
        _appName = GetAppName();
        GetExtensionVersion();
        SwitchThemeCommand = new RelayCommand<ComboBox>(
            async (cb) =>
            {
                var selected = ElementTheme.Dark;
                selected = cb.SelectedIndex switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    2 => ElementTheme.Default,
                    _ => ElementTheme.Default,
                };
                await _themeSelectorService.SetThemeAsync(selected);
            }
        );
    }

    private void GetExtensionVersion()
    {
        var currv = _appUpdateService.GetExtensionVer().ToString();
        VersionExtensions = $"Extensions version : {currv}";
    }

    private string GetAppName()
    {
        return $"{"AppDisplayName".GetLocalized()}";
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"Version - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    public async void OnNavigatedTo(object parameter)
    {
        await GetProviders();
        var provdef = await _localSettingsService.ReadSettingAsync<int>("ProviderId");

        if (provdef != 0)
        {
            var tmp = Providers.FirstOrDefault(p => p.Id == provdef);
            SelectedProvider = tmp != null ? tmp : new();
        }
        var currentTheme = _themeSelectorService.Theme;

        switch (currentTheme)
        {
            case ElementTheme.Light:
                SelectedThemeIndex = 0;
                break;
            case ElementTheme.Dark:
                SelectedThemeIndex = 1;
                break;
            case ElementTheme.Default:
                SelectedThemeIndex = 2;
                break;
        }

        if (parameter is bool isUpdate && isUpdate)
        {
            await CheckUpdates();
        }
    }

    public void OnNavigatedFrom() { }

    private async Task GetProviders()
    {
        var provs = _searchAnimeService.GetProviders();
        foreach (var item in provs)
        {
            Providers.Add(item);
        }
        SelectedProvider = provs[0];
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ChangedProvider()
    {
        if (SelectedProvider != null)
        {
            await _localSettingsService.SaveSettingAsync<int>("ProviderId", SelectedProvider.Id);
        }
    }

    [RelayCommand]
    private async Task CheckUpdates()
    {
        try
        {
            VersionMessage = "Searching for updates";

            var result = await _appUpdateService.CheckMainUpdates();
            var verEval = result.Item1;
            var version = result.Item2;
            if (verEval > 0)
            {
                VersionMessage =
                    "The running version is higher than the main version; it is not recommended to update in debug mode.";
                OnPatchNotes(
                    this,
                    (
                        "This action will break the application, be aware \n"
                            + "The running version is higher than the main version; it is not recommended to update in debug mode.",
                        version.ToString(),
                        true
                    )
                );
            }
            else if (verEval < 0)
            {
                VersionMessage = "Update Available";
                updateAvailable = true;
                var tmpNotes = await _appUpdateService.GetReleaseNotes();
                var patchNotes = (string)JObject.Parse(tmpNotes)["body"];

                OnPatchNotes(this, (patchNotes, version.ToString(), updateAvailable));
            }
            else
            {
                VersionMessage = "This version is the latest, no update required";
                updateAvailable = false;
            }
        }
        catch (Exception e)
        {
            VersionMessage = e.ToString();
        }
    }

    public async Task UpdateApp()
    {
        OnUpdatePressed(this, true);
        await _appUpdateService.UpdateApp();
    }

    [RelayCommand]
    private async Task CheckPatchNotes()
    {
        var version = _appUpdateService.GetCurrVersion();
        var tmpNotes = await _appUpdateService.GetReleaseNotes(version);
        var patchNotes = (string)JObject.Parse(tmpNotes)["body"];
        OnPatchNotes(this, (patchNotes, version.ToString(), false));
    }

    [RelayCommand]
    private async Task DeleteDBData()
    {
        ShowMessage("Wait", "Deleting data, please wait");
        await DatabaseHandler.GetInstance().DeleteDataAndRestructure();
        ShowMessage("Task Done", "Database cleared");
    }

    [RelayCommand]
    private async Task DeleteAutoComplete()
    {
        await _databaseService.ClearAutoComplete();
        ShowMessage("Task Done", "AutoComplete cleared");
    }

    private void ShowMessage(string title, string subtitle)
    {
        MessageTitle = title;
        MessageSubTitle = subtitle;
        IsMessageVisible = true;
    }

    public void ToggleMessageVisibility(bool state)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsMessageVisible = state;
        });
    }

    public event EventHandler<(string Notes, string version, bool IsAvaible)> OnPatchNotes;
    public event EventHandler<bool> OnUpdatePressed;
}
