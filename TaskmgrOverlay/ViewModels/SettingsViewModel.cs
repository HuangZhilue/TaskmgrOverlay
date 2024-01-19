using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using TaskmgrOverlay.Contracts.Services;
using TaskmgrOverlay.Contracts.ViewModels;
using TaskmgrOverlay.Models;

namespace TaskmgrOverlay.ViewModels;

public partial class SettingsViewModel(
    IOptions<AppConfig> appConfig,
    IThemeSelectorService themeSelectorService,
    ISystemService systemService,
    IApplicationInfoService applicationInfoService) : ObservableObject, INavigationAware
{
    private readonly AppConfig _appConfig = appConfig.Value;
    [ObservableProperty]
    private AppTheme _theme;
    [ObservableProperty]
    private string _versionDescription;

    public void OnNavigatedTo(object parameter)
    {
        VersionDescription = $"{Properties.Resources.AppDisplayName} - {applicationInfoService.GetVersion()}";
        Theme = themeSelectorService.GetCurrentTheme();
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void OnSetTheme(string themeName)
    {
        AppTheme theme = (AppTheme)Enum.Parse(typeof(AppTheme), themeName);
        themeSelectorService.SetTheme(theme);
    }

    [RelayCommand]
    private void OnGithubLink()
        => systemService.OpenInWebBrowser(_appConfig.GithubLink);
}
